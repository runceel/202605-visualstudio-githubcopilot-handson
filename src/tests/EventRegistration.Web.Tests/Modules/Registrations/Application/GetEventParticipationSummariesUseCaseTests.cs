using EventRegistration.Registrations.Application.DTOs;
using EventRegistration.Registrations.Application.Repositories;
using EventRegistration.Registrations.Application.UseCases;

namespace EventRegistration.Web.Tests.Modules.Registrations.Application;

/// <summary>
/// GetEventParticipationSummariesUseCase のユニットテスト。
/// </summary>
[TestClass]
public sealed class GetEventParticipationSummariesUseCaseTests
{
    [TestMethod]
    public async Task ExecuteAsync_複数EventIdを渡すとGetParticipationSummariesAsyncが1回呼ばれる()
    {
        // Arrange
        var repository = Substitute.For<IRegistrationRepository>();
        var eventIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        repository.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(eventIds.Select(id => new EventParticipationSummary(id, 0, 0)).ToList());

        var useCase = new GetEventParticipationSummariesUseCase(repository);

        // Act
        await useCase.ExecuteAsync(eventIds);

        // Assert
        await repository.Received(1).GetParticipationSummariesAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<CancellationToken>());
    }
}
