using System.Text.Json;
using StackExchange.Redis;

namespace Presentation;

public class RedisCacheService
{
    private readonly IDatabase db;

    public RedisCacheService(string host, int port)
    {
        var conn = ConnectionMultiplexer.Connect($"{host}:{port}");
        db = conn.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, ttl);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await db.StringGetAsync(key);
        if(value.IsNullOrEmpty)
            return default;
        return JsonSerializer.Deserialize<T>(value!);
    }
}
