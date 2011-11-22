using System.Web;

namespace WhaleES.Caching
{
    public class CachingConfiguration 
    {
        private readonly Configuration.Configuration _config;

        public CachingConfiguration(Configuration.Configuration config)
        {
            _config = config;
        }
        public Configuration.Configuration NullCache()
        {
            _config.Cache = new NullCache();
            return _config;
        }
        public Configuration.Configuration HttpCache(HttpApplication app)
        {
            _config.Cache = new HttpApplicationCache(app);
            return _config;
        }
        public Configuration.Configuration InMemoryCache()
        {
            _config.Cache = new InMemoryCache();
            return _config;
        }

    }
}