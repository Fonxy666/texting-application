namespace UserService.Repository.RedisRepository;

public interface IRedisRepo
{
    Task<bool> SetValueAsync(string key, string value);

    Task<string?> GetValueAsync(string key);
}

