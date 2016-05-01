using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hub.Utilities
{
    public class CacheService<T>
    {
        private Dictionary<string, T> _internalCache;
        private Object _syncLock = new object();

        public CacheService()
        {
            _internalCache = new Dictionary<string, T>();
        }

        public T GetValue(string key)
        {
            lock (_syncLock)
            {
                if (_internalCache.ContainsKey(key)) return _internalCache[key];
                return default(T);
            }
        }

        public void SetValue(string key, T value)
        {
            lock (_syncLock)
            {
                if (_internalCache.ContainsKey(key)) _internalCache[key] = value;
                else _internalCache.Add(key, value);
            }
        }
    }
}
