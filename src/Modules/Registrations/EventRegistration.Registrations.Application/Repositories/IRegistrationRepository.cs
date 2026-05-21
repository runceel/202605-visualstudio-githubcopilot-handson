using EventRegistration.Registrations.Application.DTOs;
using EventRegistration.Registrations.Domain;

namespace EventRegistration.Registrations.Application.Repositories;

/// <summary>
/// Registration エンティティのリポジトリ抽象。
/// </summary>
public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Registration>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> CountConfirmedByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveRegistrationAsync(Guid eventId, string normalizedEmail, CancellationToken cancellationToken = default);
    Task<Registration?> GetOldestWaitListedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(Registration registration, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 複数イベントの参加状況サマリーを一括取得する。N+1 問題を防ぐためのバッチクエリ。
    /// </summary>
    Task<IReadOnlyList<EventParticipationSummary>> GetParticipationSummariesAsync(
        IEnumerable<Guid> eventIds,
        CancellationToken cancellationToken = default);
}
