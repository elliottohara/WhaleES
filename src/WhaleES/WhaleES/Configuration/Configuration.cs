using System;
using System.Collections.Generic;
using Amazon.S3;

namespace WhaleES.Configuration
{
    //TODO: support the "I WANT XML folks"
    public class Configuration
    {
        public Configuration()
        {
            UseReflection().UncommitedEventsGetMethodNameIs("UncommittedEvents");
            UseReflection().ApplyMethodNameIs("Apply");
            StartReplay = a => { };
            EndReplay = a => { };
            PublishEvents = events => { };
        }
        private AmazonS3 _client;
        internal bool HasReplaySwith;
        public ISerializer Serializer { get; internal set; }
        internal string BucketName { get; set; }
        internal string Key { get; set; }
        internal string Secret { get; set; }
        internal Func<object, IEnumerable<object>> GetUncommitedEventsMethod { get; set; }
        internal Action<object, object> ApplyMethod { get; set; }
        internal Action<object> StartReplay { get; set; }
        internal Action<object> EndReplay { get; set; }
        internal Action<IEnumerable<object>> PublishEvents { get; private set; }

        internal AmazonS3 AmazonClient
        {
            get { return _client ?? (_client = Amazon.AWSClientFactory.CreateAmazonS3Client(Key, Secret)); }
        }
        public SerilizationConfiguration ToSerialize()
        {
            return new SerilizationConfiguration(this);
        }
        public AmazonConfiguration ForAmazon()
        {
            return new AmazonConfiguration(this);
        }
        public Configuration PublishEventsWith(Action<IEnumerable<object>> publisher )
        {
            PublishEvents = publisher;
            return this;
        }
        public ExampleArConfigurationBuilder<ExampleAggrigateRoot> UseExampleAggrigateRoot<ExampleAggrigateRoot>()
        {
            return new ExampleArConfigurationBuilder<ExampleAggrigateRoot>(this);
        } 
        public ReflectionAggrigateRootConfiguration UseReflection()
        {
            return new ReflectionAggrigateRootConfiguration(this);
        }
    }
}