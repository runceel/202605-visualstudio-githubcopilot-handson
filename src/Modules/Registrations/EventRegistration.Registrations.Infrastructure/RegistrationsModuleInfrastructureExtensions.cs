using EventRegistration.Registrations.Application.Repositories;
using EventRegistration.Registrations.Application.UseCases;
using EventRegistration.Registrations.Infrastructure.Persistence;
using EventRegistration.SharedKernel.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventRegistration.Registrations.Infrastructure;

/// <summary>
/// Registrations モジュールの Infrastructure サービスを DI コンテナへ登録する拡張メソッド。
/// </summary>
public static class RegistrationsModuleInfrastructureExtensions
{
    /// <summary>
    /// Registrations モジュールの DbContext、リポジトリ、ユースケースを登録する。
    /// </summary>
    public static IServiceCollection AddRegistrationsModuleInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<RegistrationsDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: "Registrations"));

        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<RegisterParticipantUseCase>();
        services.AddScoped<CancelRegistrationUseCase>();
        services.AddScoped<GetRegistrationsByEventUseCase>();
        services.AddScoped<GetEventParticipationSummariesUseCase>();

        // ユースケースは IDomainEventDispatcher を要求するため、
        // 購読モジュール (Notifications 等) が未登録でも解決できるよう既定実装を確保する (AC-04)。
        services.AddSharedKernelDomainEvents();

        return services;
    }
}
