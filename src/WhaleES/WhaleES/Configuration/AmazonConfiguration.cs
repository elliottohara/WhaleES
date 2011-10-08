namespace WhaleES.Configuration
{
    public class AmazonConfiguration
    {
        private readonly Configuration _configuration;

        public AmazonConfiguration(Configuration configuration)
        {
            _configuration = configuration;
        }
        public AmazonConfiguration KeyIs(string key)
        {
            _configuration.Key = key;
            return this;
        }
        public AmazonConfiguration SecretIs(string secret)
        {
            _configuration.Secret = secret;
            return this;
        }
        public AmazonConfiguration UseS3BucketName(string bucketName)
        {
            _configuration.BucketName = bucketName;
            return this;
        }
        public Configuration AndConfigure()
        {
            return _configuration;
        }
        public SerilizationConfiguration ToSerialize()
        {
            return _configuration.ToSerialize();
        }

    }
}