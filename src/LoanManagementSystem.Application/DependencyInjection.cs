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
        return services;
    }
}
