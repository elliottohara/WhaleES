using Amazon.S3;
using WhaleES.Serialization;

namespace WhaleES.Configuration
{
    public class Configuration
    {
        public Configuration()
        {
            UncommittedEventsMethodName = "UncommittedEvents";
        }
        private AmazonS3 _client;
        public ISerializer Serializer { get; private set; }
        public string BucketName { get; private set; }
        public string Key { get; private set; }
        public string Secret { get; private set; }
        public string UncommittedEventsMethodName { get; private set; }
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
            UncommittedEventsMethodName = methodName;
            return this;
        }
    }
}