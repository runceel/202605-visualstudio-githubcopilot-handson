using EventRegistration.Registrations.Application.DTOs;
using EventRegistration.Registrations.Application.Repositories;

namespace EventRegistration.Registrations.Application.UseCases;

/// <summary>
/// 複数イベントの参加状況サマリーを一括取得するユースケース。
/// </summary>
public sealed class GetEventParticipationSummariesUseCase(IRegistrationRepository registrationRepository)
{
    /// <summary>
    /// 指定したイベント ID リストの参加状況サマリーを一括取得する。
    /// </summary>
    public async Task<IReadOnlyList<EventParticipationSummary>> ExecuteAsync(
        IEnumerable<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        return await registrationRepository.GetParticipationSummariesAsync(eventIds, cancellationToken);
    }
}
