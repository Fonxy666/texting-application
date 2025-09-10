using StackExchange.Redis;

namespace UserService.Repository.RedisRepository;

public class RedisRepo : IRedisRepo
{
    private readonly IDatabase _db;
    public RedisRepo(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }
    public async Task<bool> SetValueAsync(string key, string value)
    {
        TimeSpan expiration = TimeSpan.FromMinutes(10);

        bool isSet = await _db.StringSetAsync(
            key,
            value,
            expiration
        );

        return isSet;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }
}

