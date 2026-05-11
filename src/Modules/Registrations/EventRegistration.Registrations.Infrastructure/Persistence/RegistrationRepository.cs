using EventRegistration.Registrations.Application.DTOs;
using EventRegistration.Registrations.Application.Repositories;
using EventRegistration.Registrations.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventRegistration.Registrations.Infrastructure.Persistence;

/// <summary>
/// IRegistrationRepository の EF Core 実装。
/// </summary>
public sealed class RegistrationRepository(RegistrationsDbContext dbContext) : IRegistrationRepository
{
    public async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Registrations.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Registration>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Registrations
            .Where(r => r.EventId == eventId && r.Status != RegistrationStatus.Cancelled)
            .OrderBy(r => r.Status)
            .ThenBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountConfirmedByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed, cancellationToken);
    }

    public async Task<bool> HasActiveRegistrationAsync(Guid eventId, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await dbContext.Registrations
            .AnyAsync(r => r.EventId == eventId
                        && r.Email == normalizedEmail
                        && r.Status != RegistrationStatus.Cancelled,
                cancellationToken);
    }

    public async Task<Registration?> GetOldestWaitListedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Registrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.WaitListed)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        await dbContext.Registrations.AddAsync(registration, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventParticipationSummary>> GetParticipationSummariesAsync(
        IEnumerable<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        var eventIdList = eventIds.ToList();
        if (eventIdList.Count == 0)
        {
            return [];
        }

        var summaries = await dbContext.Registrations
            .Where(r => eventIdList.Contains(r.EventId) && r.Status != RegistrationStatus.Cancelled)
            .GroupBy(r => r.EventId)
            .Select(g => new EventParticipationSummary(
                g.Key,
                g.Count(r => r.Status == RegistrationStatus.Confirmed),
                g.Count(r => r.Status == RegistrationStatus.WaitListed)))
            .ToListAsync(cancellationToken);

        // クエリ結果にない EventId は 0/0 で欠損補完
        var summaryDict = summaries.ToDictionary(s => s.EventId);
        return eventIdList
            .Select(id => summaryDict.TryGetValue(id, out var s)
                ? s
                : new EventParticipationSummary(id, 0, 0))
            .ToList();
    }
}
