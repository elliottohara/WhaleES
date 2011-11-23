using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using WhaleES.Caching;

namespace WhaleES
{
    public interface IStreamOfEventsFor<T> where T:new()
    {
        void Persist(string id, params object[] events);
        void Persist(string id,object @event);
        IEnumerable<object> GetEventStream(string id);
        IEnumerable<object> GetEventStream();
    }
    public class CachingEventStream<T> : StreamOfEventsFor<T> where T : new()
    {
        private readonly ICache _cache;
        private static string _key = typeof (T).FullName;
        public CachingEventStream(AmazonS3 s3Client, string bucket, ISerializer serializer,ICache cache) : base(s3Client, bucket, serializer)
        {
            _cache = cache;
        }

        public override IEnumerable<object> GetEventStream()
        {
            return _cache.Get<List<object>>(_key) ?? _cache.Put(_key, base.GetEventStream().ToList());
        }
        public override IEnumerable<object> GetEventStream(string id)
        {
            var key = String.Format("{0}/{1}", _key, id);
            return _cache.Get<List<object>>(key) ?? _cache.Put(key, base.GetEventStream(id).ToList());
        }
        public override void Persist(string id, params object[] events)
        {
            base.Persist(id, events);
            var key = String.Format("{0}/{1}", _key, id);
            var fromCache = _cache.Get<List<object>>(key) ?? _cache.Put(key, new List<object>());
            fromCache.AddRange(events);
        }
    }
    public class StreamOfEventsFor<T> : IDisposable, IStreamOfEventsFor<T> where T:new()
    {
        private readonly AmazonS3 _s3Client;
        private readonly string _bucket;
        private readonly ISerializer _serializer;
        private readonly string _keyPrefix;

// ReSharper disable StaticFieldInGenericType
        private static readonly Dictionary<string, string> _lastCommitId  = new Dictionary<string, string>();
        private static readonly Dictionary<string,List<EventEnvelope>>  _cache = new Dictionary<string, List<EventEnvelope>>();
// ReSharper restore StaticFieldInGenericType

        public StreamOfEventsFor(AmazonS3 s3Client,string bucket,ISerializer serializer)
        {
            _s3Client = s3Client;
            _bucket = bucket;
            _serializer = serializer;
            _keyPrefix = typeof (T).FullName + "/";
        }
        private string KeyFor(string id)
        {
            return _keyPrefix + id;
        }
        private IEnumerable<string> AllStreamIds()
        {
            var listObjectsRequest = new ListObjectsRequest().WithBucketName(_bucket).WithPrefix(_keyPrefix);
            foreach (var s3Object in _s3Client.ListObjects(listObjectsRequest).S3Objects)
            {
                var key = s3Object.Key;
                key = key.Substring(_keyPrefix.Length);
                var lastIndex = key.Contains("/") ? key.IndexOf("/") : key.Length;
                key = key.Substring(0, lastIndex);
                yield return key;
            }
        }
        private bool Exists(string id)
        {
            return AllStreamIds().Contains(id);
        }
        public virtual IEnumerable<object> GetEventStream()
        {
            var allEvents = new List<EventEnvelope>();
            foreach (var streamId in AllStreamIds())
            {
                allEvents.AddRange(ExistingEventsFor(streamId));
            }
            allEvents = allEvents.OrderBy(e => e.CommittedAt).ToList();
            
            return allEvents.OrderBy(e => e.CommittedAt).Select(GetPayload);
        }
        private List<EventEnvelope> GetEventFromS3(S3Object s3Object)
        {
            var getObjectRequest = new GetObjectRequest().WithBucketName(_bucket).WithKey(s3Object.Key);
            var response = _s3Client.GetObject(getObjectRequest);
            var stream = new StreamReader(response.ResponseStream);
            var data = stream.ReadToEnd();
            return (List<EventEnvelope>)_serializer.Deserialize(data, typeof(List<EventEnvelope>));      
        }
        private IEnumerable<EventEnvelope> ExistingEventsFor(string id)
        {
            if(!Exists(id))
                return new List<EventEnvelope>();
            if (!_lastCommitId.ContainsKey(id))
                _lastCommitId[id] = String.Empty;

            var allEnvelopes = _cache.ContainsKey(id) ? _cache[id] : new List<EventEnvelope>();

            var getCommitsRequest = new ListObjectsRequest().WithBucketName(_bucket)
                .WithPrefix(KeyFor(id))
                .WithMarker(_lastCommitId[id])
                ;
            var results = _s3Client.ListObjects(getCommitsRequest).S3Objects;
            foreach (var result in results.AsParallel().Select(GetEventFromS3))
            {   
                allEnvelopes.AddRange(result);
            }
            if(results.Count > 0)
                _lastCommitId[id] = results.Last().Key;
            
            return (_cache[id] = allEnvelopes.OrderBy(e => e.CommittedAt).ToList());
        }
        internal string LastCommitId(string id)
        {
            return _lastCommitId.ContainsKey(id) ? _lastCommitId[id] : String.Empty;
        }
        public virtual void Persist(string id, params object[] events)
        {
            var key = KeyFor(id + "/" + GenerateCommitId());
            var envelopes = new List<EventEnvelope>();
            foreach (var @event in events)
            {
                var serializedEvent = _serializer.Serialize(@event);
                envelopes.Add(EventEnvelope.New(serializedEvent, @event.GetType()));
            }
            PersistEvents(key,envelopes);

        }
        string GenerateCommitId()
        {
            var aGuid = Guid.NewGuid().ToString();
            return DateTime.UtcNow.Ticks + aGuid.Substring(0,aGuid.IndexOf("-"));
        }
        public void Persist(string id,object @event)
        {
            var key = KeyFor(id + "/" + GenerateCommitId());
            var serializedEvent = _serializer.Serialize(@event);
            var existing = new List<EventEnvelope>();
            existing.Add(EventEnvelope.New(serializedEvent,@event.GetType()));
            PersistEvents(key, existing);
        }

        private void PersistEvents(string key, List<EventEnvelope> events)
        {
            var serializedEventStream = _serializer.Serialize(events);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEventStream));
            var putRequest =
                new PutObjectRequest()
                    .WithBucketName(_bucket)
                    .WithKey(key)
                    .WithInputStream(stream) as PutObjectRequest;

            _s3Client.PutObject(putRequest);
        }

        public virtual IEnumerable<object> GetEventStream(string id)
        {
            var existingEvents = ExistingEventsFor(id);
            return existingEvents.Select(GetPayload);
        }

        private object GetPayload(EventEnvelope envelope)
        {
            var typeOfEvent = Type.GetType(envelope.EventType);
            return _serializer.Deserialize(envelope.Payload, typeOfEvent);
        }
        public void Dispose(){}
    }
}