using System.Collections.Generic;
using System.Web;

namespace WhaleES.Caching
{
    public interface ICache
    {
        T Put<T>(string key, T value);
        T Get<T>(string key);
    }
    public class NullCache : ICache{
        public T Put<T>(string key, T value)
        {
            return value;
        }

        public T Get<T>(string key)
        {
            return default(T);
        }
    }
    public class HttpApplicationCache : ICache
    {
        private readonly HttpApplication _application;

        public HttpApplicationCache(HttpApplication application)
        {
            _application = application;
        }

        public T Put<T>(string key, T value)
        {
            _application.Context.Cache[key] = value;
            return value;
        }

        public T Get<T>(string key)
        {
            if (_application.Context.Cache[key] != null)
                return (T) _application.Context.Cache[key];
            return default(T);
        }
    }
    public class InMemoryCache:ICache
    {
        private static Dictionary<string, object> _dictionary = new Dictionary<string, object>();

        public T Put<T>(string key, T value)
        {
            _dictionary[key] = value;
            return value;
        }

        public T Get<T>(string key)
        {
            if (_dictionary.ContainsKey(key))
                return (T) _dictionary[key];
            return default(T);
        }
    }
}