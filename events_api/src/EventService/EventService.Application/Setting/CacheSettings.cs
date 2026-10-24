namespace EventService.Application.Settings;

public class CacheSettings
{
    public const string SectionName = "Cache";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "events:";
    public int EventTtlMinutes { get; set; } = 5;
    public int TopEventsTtlMinutes { get; set; } = 10;
}