using Microsoft.Extensions.DependencyInjection;

namespace PowerOfficeGoV2Onboarding.Extensions;

/// <summary>
/// Extension methods for DI.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IPowerOfficeGoOnboardingService"/> for onboarding clients to your PowerOfficeGo integration.
    /// </summary>
    /// <param name="services"></param>
    public static void AddPowerOfficeGoOnboarding(this IServiceCollection services)
    {
        services.AddSingleton<PowerOfficeGoOnboardingService>();
        services.AddScoped<IPowerOfficeGoOnboardingService>(s => s.GetRequiredService<PowerOfficeGoOnboardingService>());
        services.AddScoped<IPowerofficeGoOnboardingFinalizingService>(s => s.GetRequiredService<PowerOfficeGoOnboardingService>());
    }
}