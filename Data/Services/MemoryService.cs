using Microsoft.Extensions.Caching.Memory;
using my_books.Data.Model;

namespace my_books.Data.Services
{
    public class MemoryService
    {
        private readonly IMemoryCache _cache;

        public MemoryService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T GetData<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out T data))
            {
                // Return the cached data if it exists
                return data;
            }

            // Return null or handle the absence of data as needed
            return null;
        }

        public void SetData<T>(string key, T data, TimeSpan expiration) where T : class
        {
            if (data != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(expiration);

                // Save data in cache
                _cache.Set(key, data, cacheEntryOptions);
                // Check if the data is set successfully
                if (_cache.TryGetValue(key, out T cachedData))
                {
                    var d = cachedData;
                    Console.WriteLine($"Data successfully cached for key: {key}");
                }
                else
                {
                    var d = cachedData;
                    Console.WriteLine($"Failed to cache data for key: {key}");
                }
            }
        }

    }
}
