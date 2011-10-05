using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.S3;
using WhaleES.Serialization;

namespace WhaleES.Configuration
{
    public class Configuration
    {
        public Configuration()
        {
            UncommitedEventsGetMethodNameIs("UncommittedEvents");
            ApplyMethodNameIs("Apply");
        }
        private AmazonS3 _client;
        public ISerializer Serializer { get; private set; }
        public string BucketName { get; private set; }
        public string Key { get; private set; }
        public string Secret { get; private set; }
        public Func<object, IEnumerable<object>> GetUncommitedEventsMethod { get; private set; }
        public Action<object,object> ApplyMethod { get; private set; } 
        public AmazonS3 AmazonClient
        {
            get { return _client ?? (_client = Amazon.AWSClientFactory.CreateAmazonS3Client(Key, Secret)); }
        }
        public Configuration WithSerializer(ISerializer serializer)
        {
            Serializer = serializer;
            return this;
        }
        public Configuration WithProtocolBufferSerialization()
        {
            return WithSerializer(new ProtoBufSerializer());
        }
        public Configuration WithJsonSerialization()
        {
            return WithSerializer(new JsonSerializer());
        }
        public Configuration WithBucket(string bucketName)
        {
            BucketName = bucketName;
            return this;
        }
        public Configuration WithKey(string key)
        {
            Key = key;
            return this;
        }
        public Configuration WithSecret(string secret)
        {
            Secret = secret;
            return this;
        }
        public Configuration UncommitedEventsGetMethodNameIs(string methodName)
        {
            return UseFuncToGetUncommitedEvents(ar => ar.GetType().GetProperties().FirstOrDefault(pi => pi.Name == methodName).GetGetMethod().Invoke(ar,null) as IEnumerable<object>);
            
        }
        public Configuration UseFuncToGetUncommitedEvents(Func<object,IEnumerable<object>> uncommittedEventsMethodGetter)
        {
            GetUncommitedEventsMethod = uncommittedEventsMethodGetter;
            return this;
        }
      /// <summary>
      /// first param is the event, second is the event
      /// </summary>
      /// <param name="applyAction"></param>
      /// <returns></returns>
        public Configuration UseActionToCallApply(Action<object,object> applyAction)
        {
            ApplyMethod = applyAction;
            return this;
        }
        public Configuration ApplyMethodNameIs(string methodName)
        {

            return UseActionToCallApply((@event, ar) =>
                                            {
                                                var applyMethod = ar.GetType().GetMethods()
                                                    .FirstOrDefault(mi => mi.Name == "Apply" &&
                                                                          mi.GetParameters()
                                                                              .Any(
                                                                                  pi =>
                                                                                  pi.ParameterType == @event.GetType())
                                                                                  );
                                                if (applyMethod == null) return;
                                                applyMethod.Invoke(ar, new[]{@event,true});
                                            });
        }
    }
}