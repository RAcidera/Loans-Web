using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.Interest;
using LoanManagementSystem.Application.Diary;
using LoanManagementSystem.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace LoanManagementSystem.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR against this assembly, which picks up every
    /// IRequestHandler (queries/commands) and INotificationHandler (domain
    /// event handlers) defined here via assembly scanning — nothing needs
    /// to be registered by hand as new use cases are added.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Plain application services — no repository/infra dependency, so
        // (unlike ILoanRepository etc.) these live and get registered here
        // rather than in Infrastructure's DependencyInjection.
        services.AddScoped<IInterestCalculationService, InterestCalculationService>();
        services.AddScoped<InterestEarnedReportDataProvider>();
        services.AddScoped<IFinancialSnapshotService, FinancialSnapshotService>();

        // Business Time Zone strategy: TimeProvider.System is the real
        // clock, swappable in tests; BusinessTimeZoneCache is a singleton
        // so its one in-memory value is shared and refreshed instantly on
        // update across every request (see its own doc comment).
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessTimeZoneCache, BusinessTimeZoneCache>();
        services.AddScoped<IAppDateTimeService, AppDateTimeService>();

        return services;
    }
}
