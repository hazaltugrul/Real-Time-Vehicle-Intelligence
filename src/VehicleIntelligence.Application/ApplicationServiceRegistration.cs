using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VehicleIntelligence.Application.Services;
using VehicleIntelligence.Application.Validation;

namespace VehicleIntelligence.Application;

/// <summary>
/// Extension methods for registering Application layer services into the DI container.
/// Called from both the API and Worker host builders.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IAlertRuleEngine, AlertRuleEngine>();

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssemblyContaining<TelemetryReceivedEventValidator>();

        return services;
    }
}
