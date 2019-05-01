using System;
using System.Runtime.Caching;

namespace RaceDayAPI
{
    public class InMemoryCache : ICacheService
    {
        public TValue Get<TValue>(string cacheKey, int durationInMinutes, Func<TValue> getItemCallback) where TValue : class
        {
            TValue item = MemoryCache.Default.Get(cacheKey) as TValue;
            if (item == null)
            {
                item = getItemCallback();
                if (item != null)
                    MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(durationInMinutes));
            }
            return item;
        }

        public TValue Get<TValue, TId>(string cacheKeyFormat, TId id, int durationInMinutes, Func<TId, TValue> getItemCallback) where TValue : class
        {
            string cacheKey = string.Format(cacheKeyFormat, id);
            TValue item = MemoryCache.Default.Get(cacheKey) as TValue;
            if (item == null)
            {
                item = getItemCallback(id);
                if (item != null)
                    MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(durationInMinutes));
            }
            return item;
        }
    }

    interface ICacheService
    {
        TValue Get<TValue>(string cacheKey, int durationInMinutes, Func<TValue> getItemCallback) where TValue : class;
        TValue Get<TValue, TId>(string cacheKeyFormat, TId id, int durationInMinutes, Func<TId, TValue> getItemCallback) where TValue : class;
    }
}
