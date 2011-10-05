using System.Configuration;

namespace WhaleES.Configuration
{
    public static class ConfigureWhaleEs
    {
        public static Configuration CurrentConfig { get; private set; }
        public static Configuration With()
        {
            CurrentConfig = new Configuration();
            return CurrentConfig;
        }
        internal static void AssertConfigurationIsValid()
        {
            if(CurrentConfig == null) throw new ConfigurationErrorsException("No configuration exists, please call ConfigureWhaleEs.With() during bootstrapping.");
            if (CurrentConfig.Serializer == null) throw new ConfigurationErrorsException("No Serializer set please call ConfigureWhaleEs.WithSerializer to set serializer.");
            if (CurrentConfig.BucketName == null) throw new ConfigurationErrorsException("S3 bucket name not set. Please call WithBucket on ConfigureWhaleEs.With().WithBucket()");
            if (string.IsNullOrEmpty(CurrentConfig.Key)) throw new ConfigurationErrorsException("Key is not set");
            if (string.IsNullOrEmpty(CurrentConfig.Secret)) throw new ConfigurationErrorsException("Secret is not set");

        }
    }
}