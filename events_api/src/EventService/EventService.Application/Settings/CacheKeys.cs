namespace EventService.Application.Caching;

public static class CacheKeys
{
    public static string Event(Guid id) => $"event:{id}";

    public static string TopEvents(int count) => $"events:top{count}";
}