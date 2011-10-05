using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;

namespace WhaleES
{
    public class StreamOfEventsFor<T> : IDisposable
    {
        private readonly AmazonS3 _s3Client;
        private readonly string _bucket;
        private readonly ISerializer _serializer;
        private readonly string _keyPrefix;

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
        private bool Exists(string id)
        {
            var listObjectsRequest = new ListObjectsRequest().WithBucketName(_bucket).WithPrefix(_keyPrefix);
            return _s3Client.ListObjects(listObjectsRequest).S3Objects.Any(s => s.Key == KeyFor(id));
        }
        private List<EventEnvelope>  ExistingEventsFor(string id)
        {
            if(!Exists(id))
                return new List<EventEnvelope>();
            var getObjectRequest = new GetObjectRequest().WithBucketName(_bucket).WithKey(KeyFor(id));
            var response = _s3Client.GetObject(getObjectRequest);
            var stream = new StreamReader(response.ResponseStream);
            var data = stream.ReadToEnd();
            var envelopes = (List<EventEnvelope>) _serializer.Deserialize(data, typeof (List<EventEnvelope>));
            return envelopes;
        }
        public void Persist(string id, params object[] events)
        {
            var existingEvents = ExistingEventsFor(id);
            foreach (var @event in events)
            {
                var serializedEvent = _serializer.Serialize(@event);
                existingEvents.Add(EventEnvelope.New(serializedEvent, @event.GetType()));
            }
            PersistEvents(id,existingEvents);

        }
        public void Persist(string id,object @event)
        {
            var serializedEvent = _serializer.Serialize(@event);
            var existing = ExistingEventsFor(id);
            existing.Add(EventEnvelope.New(serializedEvent,@event.GetType()));
            PersistEvents(id, existing);
        }

        private void PersistEvents(string id, List<EventEnvelope> events)
        {
            var serializedEventStream = _serializer.Serialize(events);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedEventStream));
            var putRequest =
                new PutObjectRequest()
                    .WithBucketName(_bucket)
                    .WithKey(KeyFor(id))
                    .WithInputStream(stream) as PutObjectRequest;

            _s3Client.PutObject(putRequest);
        }

        public IEnumerable<object> GetEventStream(string id)
        {
            foreach (var eventEnvelope in ExistingEventsFor(id))
            {
                var typeOfEvent = Type.GetType(eventEnvelope.EventType);
                yield return _serializer.Deserialize(eventEnvelope.Payload, typeOfEvent);
            }
        }

        public void Dispose(){}
    }
}