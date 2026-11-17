using System.Collections.Concurrent;

namespace StudentApi.Security;

public sealed record AuditEntry(
    DateTime TimestampUtc,
    string? Actor,
    string Action,
    string Resource,
    string Details);

public sealed class AuditService
{
    private readonly ConcurrentQueue<AuditEntry> _entries = new();
    private const int MaximumEntries = 500;

    public void Add(string? actor, string action, string resource, string details)
    {
        _entries.Enqueue(new AuditEntry(
            DateTime.UtcNow,
            actor,
            action,
            resource,
            details));

        while (_entries.Count > MaximumEntries && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<AuditEntry> GetRecent(int count = 100)
    {
        return _entries
            .Reverse()
            .Take(Math.Clamp(count, 1, MaximumEntries))
            .ToList();
    }
}
