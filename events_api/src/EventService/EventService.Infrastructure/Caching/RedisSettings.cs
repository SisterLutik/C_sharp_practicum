namespace EventService.Infrastructure.Caching;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "events:";
    public int DefaultExpirationMinutes { get; set; } = 5;
}