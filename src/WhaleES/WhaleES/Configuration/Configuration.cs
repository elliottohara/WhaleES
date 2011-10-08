using System;
using System.Collections.Generic;
using System.Configuration;
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
            StartReplay = a => { };
            EndReplay = a => { };
            PublishEvents = events => { };
        }
        private AmazonS3 _client;
        private bool _hasReplaySwith;
        public ISerializer Serializer { get; private set; }
        public string BucketName { get; private set; }
        public string Key { get; private set; }
        public string Secret { get; private set; }
        public Func<object, IEnumerable<object>> GetUncommitedEventsMethod { get; private set; }
        public Action<object,object> ApplyMethod { get; private set; }
        public Action<object> StartReplay { get; private set; }
        public Action<object> EndReplay { get; set; }
        public Action<IEnumerable<object>> PublishEvents { get; private set; }
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
            return UseFuncToGetUncommitedEvents(ar =>
                                                    {
                                                        var property = ar.GetType()
                                                            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                            .FirstOrDefault(pi => pi.Name == methodName);
                                                        if(property == null)    
                                                            throw new ConfigurationErrorsException("can not find Property with name " + methodName + "for record operation. Please check that your AggrigateRoot contains an IEnumerable<object> property with that name, or Configure WhaleES with the proper property name by calling UncommitedEventsGetMethodNameIs on ConfigureWhaleEs.With()");
                                                        var getter = property.GetGetMethod()??property.GetGetMethod(true);

                                                        return getter.Invoke(ar, null) as IEnumerable<object>;
                                                    });
            
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
                                                var applyMethod = ar.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                                                    .FirstOrDefault(mi => mi.Name == "Apply" &&
                                                                          mi.GetParameters()
                                                                              .Any(
                                                                                  pi =>
                                                                                  pi.ParameterType == @event.GetType())
                                                                                  );
                                                if (applyMethod == null) return;
                                                if (_hasReplaySwith)
                                                    applyMethod.Invoke(ar, new[] { @event });
                                                else
                                                    applyMethod.Invoke(ar, new[] {@event});
                                                    
                                                
                                            });
        }
        public Configuration CallMethodToStartReplay(string methodName)
        {
            _hasReplaySwith = false;
            StartReplay =
                ar =>
                ar.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(
                    mi => mi.Name == methodName).Invoke(ar, null);
            return this;
        }
        public Configuration CallMethodToEndReplay(string methodName)
        {
            _hasReplaySwith = true;
            EndReplay =
                ar =>
                ar.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(
                    mi => mi.Name == methodName).Invoke(ar, null);
            return this;
        }
        public Configuration PublishEventsWith(Action<IEnumerable<object>> publisher )
        {
            PublishEvents = publisher;
            return this;
        }
    }
}