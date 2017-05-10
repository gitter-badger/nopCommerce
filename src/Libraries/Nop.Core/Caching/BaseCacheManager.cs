using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Nop.Core.Caching
{
    public abstract class BaseCacheManager
    {
        #region Fields

        protected CancellationTokenSource _cancellationTokenSource;
        protected readonly ConcurrentDictionary<string, bool> _allKeys;

        #endregion

        #region Ctor

        protected BaseCacheManager()
        {
            _allKeys = new ConcurrentDictionary<string, bool>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Utilities

        protected MemoryCacheEntryOptions GetMemoryCacheEntryOptions(int cacheTime)
        {
            var options = new MemoryCacheEntryOptions()
                .AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token))
                .RegisterPostEvictionCallback(PostEviction);
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTime);

            return options;
        }

        protected string AddKey(string key)
        {
            _allKeys.TryAdd(key, true);
            return key;
        }

        protected string RemoveKey(string key)
        {
            _allKeys.TryRemove(key, out bool _);
            return key;
        }

        private void TryClearKeys()
        {
            foreach (var key in _allKeys.Where(p => !p.Value).Select(p => p.Key).ToList())
            {
                RemoveKey(key);
            }
        }

        private void PostEviction(object key, object value, EvictionReason reason, object state)
        {
            TryClearKeys();
            var currntKey = key.ToString();

            bool flag;
            _allKeys.TryRemove(currntKey, out flag);
            if (!flag)
                _allKeys.TryUpdate(currntKey, false, false);
        }

        #endregion
    }
}
