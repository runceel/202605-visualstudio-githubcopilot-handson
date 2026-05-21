using EventRegistration.Events.Application.Repositories;
using EventRegistration.Events.Application.UseCases;
using EventRegistration.Events.Domain;
using EventRegistration.Registrations.Application.DTOs;
using EventRegistration.Registrations.Application.Repositories;
using EventRegistration.Registrations.Application.UseCases;
using EventRegistration.Web.Components.Pages.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace EventRegistration.Web.Tests.Components.Pages.Events;

[TestClass]
public sealed class EventListTests : BunitContext
{
    private IEventRepository _mockEventRepo = default!;
    private IRegistrationRepository _mockRegRepo = default!;

    [TestInitialize]
    public void Setup()
    {
        _mockEventRepo = Substitute.For<IEventRepository>();
        _mockRegRepo = Substitute.For<IRegistrationRepository>();
        Services.AddSingleton(_mockEventRepo);
        Services.AddSingleton(_mockRegRepo);
        Services.AddTransient<GetAllEventsUseCase>();
        Services.AddTransient<GetEventParticipationSummariesUseCase>();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void NoEvents_ShowsEmptyMessage()
    {
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
            Assert.IsTrue(cut.Markup.Contains("イベントがまだ登録されていません")));
    }

    [TestMethod]
    public void WithEvents_ShowsEventNames()
    {
        var events = new List<Event>
        {
            Event.Create("テストイベント1", "説明1", DateTimeOffset.UtcNow.AddDays(7), 10),
            Event.Create("テストイベント2", null, DateTimeOffset.UtcNow.AddDays(14), 20),
        };
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)events);
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("テストイベント1"));
            Assert.IsTrue(cut.Markup.Contains("テストイベント2"));
        });
    }

    [TestMethod]
    public void WithEvents_ShowsCapacity()
    {
        var ev = Event.Create("テスト", null, DateTimeOffset.UtcNow.AddDays(7), 50);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
            Assert.IsTrue(cut.Markup.Contains("50")));
    }

    [TestMethod]
    public void WithEvents_ShowsDescription()
    {
        var ev = Event.Create("テスト", "イベントの詳細説明テキスト", DateTimeOffset.UtcNow.AddDays(7), 10);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
            Assert.IsTrue(cut.Markup.Contains("イベントの詳細説明テキスト")));
    }

    [TestMethod]
    public void ClickEventCard_NavigatesToDetail()
    {
        var ev = Event.Create("テスト", null, DateTimeOffset.UtcNow.AddDays(7), 10);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
            Assert.IsTrue(cut.Markup.Contains("テスト")));

        cut.Find(".mud-card").Click();

        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.IsTrue(navMan.Uri.Contains($"/events/{ev.Id}"));
    }

    [TestMethod]
    public void ShowsCreateButton()
    {
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event>());

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
            Assert.IsTrue(cut.Markup.Contains("新しいイベントを作成")));
    }

    // TC-001: 登録 0 件 - 参加確定 0 名・残り枠 = 定員・満席/キャンセル待ちは非表示
    [TestMethod]
    public void ParticipationSummary_NoRegistrations_ShowsZeroConfirmedAndFullCapacity()
    {
        var ev = Event.Create("テストイベント", null, DateTimeOffset.UtcNow.AddDays(7), 30);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>
            {
                new EventParticipationSummary(ev.Id, ConfirmedCount: 0, WaitListedCount: 0)
            });

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("参加確定: 0 名 / 定員 30 名"));
            Assert.IsTrue(cut.Markup.Contains("残り 30 枠"));
            Assert.IsFalse(cut.Markup.Contains("満席"));
            Assert.IsFalse(cut.Markup.Contains("キャンセル待ちあり"));
        });
    }

    // TC-002: 残り枠あり - ConfirmedCount=N (N<Capacity)
    [TestMethod]
    public void ParticipationSummary_WithRemainingSlots_ShowsConfirmedAndRemainingSlots()
    {
        var ev = Event.Create("テストイベント", null, DateTimeOffset.UtcNow.AddDays(7), 20);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>
            {
                new EventParticipationSummary(ev.Id, ConfirmedCount: 5, WaitListedCount: 0)
            });

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("参加確定: 5 名 / 定員 20 名"));
            Assert.IsTrue(cut.Markup.Contains("残り 15 枠"));
            Assert.IsFalse(cut.Markup.Contains("満席"));
        });
    }

    // TC-003: 満席 - ConfirmedCount >= Capacity
    [TestMethod]
    public void ParticipationSummary_FullCapacity_ShowsFullBadgeAndHidesRemainingSlots()
    {
        var ev = Event.Create("テストイベント", null, DateTimeOffset.UtcNow.AddDays(7), 10);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>
            {
                new EventParticipationSummary(ev.Id, ConfirmedCount: 10, WaitListedCount: 0)
            });

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("満席"));
            Assert.IsFalse(cut.Markup.Contains("残り 0 枠"));
        });
    }

    // TC-004: キャンセル待ちあり - WaitListedCount >= 1
    [TestMethod]
    public void ParticipationSummary_WithWaitListed_ShowsWaitListedChip()
    {
        var ev = Event.Create("テストイベント", null, DateTimeOffset.UtcNow.AddDays(7), 10);
        _mockEventRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Event>)new List<Event> { ev });
        _mockRegRepo.GetParticipationSummariesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventParticipationSummary>)new List<EventParticipationSummary>
            {
                new EventParticipationSummary(ev.Id, ConfirmedCount: 10, WaitListedCount: 3)
            });

        var cut = Render<EventList>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Markup.Contains("満席"));
            Assert.IsTrue(cut.Markup.Contains("キャンセル待ちあり"));
        });
    }
}
