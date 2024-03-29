using StackExchange.Redis.Extensions.Core.Abstractions;

namespace NotesApi.Services
{
    public interface ICacheHelperService
    {
        Task FlushGameConfig(string pattern);

        Task<T> TryToGetObjectAsync<T>(string key);

        Task<T> TryToGetObjectAsync<T>(string key, Func<Task<T>> callback);

        Task<T> TryToGetObjectAsync<T>(string key, Func<T> callback);

        Task<T> TryToGetObjectAsync<T>(string key, Func<Task<T>> callback, TimeSpan expiredIn, HashSet<string> tags = null, bool forceRefresh = false);

        Task<List<T>> TryToGetListAsync<T>(string key, Func<Task<List<T>>> callback);

        Task<List<T>> TryToGetListAsync<T>(string key, Func<List<T>> callback);


        Task<List<string>> TryToGetKeysByPattern(string key);

        Task RemoveAllKeyAsync(string[] keys);

        Task RemoveAllTagsAsync(HashSet<string> tags);

        Task RemoveTagAsync(string tag);

        Task UpdateExpiryAsync(string key, TimeSpan timeSpan);

        Task AddExpiryAsync<T>(string key, T value, TimeSpan timeSpan);

        Task AddAlwayAsync<T>(string key, T value, TimeSpan timeSpan);

        Task AddAlwayAsync<T>(string key, T value);
    }


    public class RedisCacheService : ICacheHelperService
    {
        private readonly IRedisDatabase _redisCacheClient;

        public RedisCacheService(IRedisDatabase redisCacheClient)
        {
            _redisCacheClient = redisCacheClient;
        }
        public async Task FlushGameConfig(string pattern)
        {
            var keys = await TryToGetKeysByPattern(pattern);
            await RemoveAllKeyAsync(keys.ToArray());
        }

        public async Task<List<string>> TryToGetKeysByPattern(string key)
        {
            return (await _redisCacheClient.SearchKeysAsync(key)).ToList();
        }

        public async Task RemoveAllKeyAsync(string[] keys)
        {
            await _redisCacheClient.RemoveAllAsync(keys, StackExchange.Redis.CommandFlags.FireAndForget);

        }

        public async Task<List<T>> TryToGetListAsync<T>(string key, Func<Task<List<T>>> callback)
        {
            List<T> v;
            try
            {
                v = await _redisCacheClient.GetAsync<List<T>>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                if (v == null || v.Count == 0)
                {
                    v = await callback();
                    await _redisCacheClient.AddAsync(key, v, TimeSpan.FromMinutes(15));
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key);
                v = await callback();
            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);
                v = await callback();
            }
            return v;
        }

        public async Task<List<T>> TryToGetListAsync<T>(string key, Func<List<T>> callback)
        {
            List<T> v;
            try
            {
                v = await _redisCacheClient.GetAsync<List<T>>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                if (v == null || v.Count == 0)
                {
                    v = callback();
                    await _redisCacheClient.AddAsync(key, v, TimeSpan.FromMinutes(15));
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key);
                v = callback();
            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);
                v = callback();
            }
            return v;
        }

        public async Task<T> TryToGetObjectAsync<T>(string key, Func<Task<T>> callback)
        {
            T v;
            try
            {
                v = await _redisCacheClient.GetAsync<T>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                if (v == null)
                {
                    v = await callback();
                    await _redisCacheClient.AddAsync(key, v, TimeSpan.FromMinutes(15));
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key);
                v = await callback();
            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);
                v = await callback();
            }
            return v;
        }

        public async Task<T> TryToGetObjectAsync<T>(string key, Func<T> callback)
        {
            T v;
            try
            {
                v = await _redisCacheClient.GetAsync<T>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                if (v == null)
                {
                    v = callback();
                    await _redisCacheClient.AddAsync(key, v, TimeSpan.FromMinutes(15));
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key);
                v = callback();
            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);
                v = callback();
            }
            return v;
        }

        public async Task<T> TryToGetObjectAsync<T>(string key, Func<Task<T>> callback, TimeSpan expiredIn, HashSet<string> tags = null, bool forceRefresh = false)
        {
            T v;
            try
            {
                if (!forceRefresh)
                {
                    v = await _redisCacheClient.GetAsync<T>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                    if (v == null)
                    {
                        v = await callback();
                        if (tags == null)
                        {
                            await _redisCacheClient.AddAsync(key, v, expiredIn);
                        }
                        else
                        {
                            await _redisCacheClient.AddAsync(key, v, expiredIn, tags: tags);
                        }

                    }
                }
                else
                {
                    v = await callback();
                    if (tags == null)
                    {
                        await _redisCacheClient.AddAsync(key, v, expiredIn);
                    }
                    else
                    {
                        await _redisCacheClient.AddAsync(key, v, expiredIn, tags: tags);
                    }
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key, expiredIn);
                v = await callback();
            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);
                v = await callback();
            }
            return v;
        }

        public Task UpdateExpiryAsync(string key, TimeSpan timeSpan)
        {
            return _redisCacheClient.UpdateExpiryAsync(key, timeSpan);
        }

        public Task AddExpiryAsync<T>(string key, T value, TimeSpan timeSpan)
        {
            return _redisCacheClient.AddAsync(key, value, timeSpan, StackExchange.Redis.When.NotExists);
        }

        public Task AddAlwayAsync<T>(string key, T value, TimeSpan timeSpan)
        {
            return _redisCacheClient.AddAsync(key, value, timeSpan, StackExchange.Redis.When.Always);
        }

        public Task AddAlwayAsync<T>(string key, T value)
        {
            return _redisCacheClient.AddAsync(key, value, StackExchange.Redis.When.Always);
        }


        public async Task<T> TryToGetObjectAsync<T>(string key)
        {
            T v = default(T);
            try
            {
                v = await _redisCacheClient.GetAsync<T>(key, StackExchange.Redis.CommandFlags.PreferReplica);
                if (v == null)
                {

                    await _redisCacheClient.AddAsync(key, v, TimeSpan.FromMinutes(15));
                }
            }
            catch (TimeoutException)
            {
                Serilog.Log.Error("TimeoutException on try to get {key} from redis", key);

            }
            catch (Exception e)
            {
                Serilog.Log.Error("{exception} on try to get {key} from redis", e, key);

            }
            return v;
        }

        public async Task RemoveAllTagsAsync(HashSet<string> tags)
        {
            foreach (var tag in tags)
            {
                await _redisCacheClient.RemoveByTagAsync(tag);
            }

        }

        public async Task RemoveTagAsync(string tag)
        {
            await _redisCacheClient.RemoveByTagAsync(tag);
        }
    }
}
