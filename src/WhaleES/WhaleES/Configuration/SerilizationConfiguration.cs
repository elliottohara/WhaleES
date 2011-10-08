using ProtoBuf;
using WhaleES.Serialization;

namespace WhaleES.Configuration
{
    public class SerilizationConfiguration
    {
        private readonly Configuration _config;

        public SerilizationConfiguration(Configuration config)
        {
            _config = config;
        }
        public Configuration UseThisSerializer(ISerializer serializer)
        {
            _config.Serializer = serializer;
            return _config;
        }
        public Configuration UseProtocolBuffers()
        {
            return UseThisSerializer(new ProtoBufSerializer());
        }
        public Configuration UseMicrosoftJavscriptSerializer()
        {
            return UseThisSerializer(new JsonSerializer());
        }
    }
}