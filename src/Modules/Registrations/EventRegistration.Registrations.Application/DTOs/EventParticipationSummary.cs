namespace EventRegistration.Registrations.Application.DTOs;

/// <summary>
/// イベントごとの参加状況サマリー。
/// </summary>
public sealed record EventParticipationSummary(
    Guid EventId,
    int ConfirmedCount,
    int WaitListedCount
);
