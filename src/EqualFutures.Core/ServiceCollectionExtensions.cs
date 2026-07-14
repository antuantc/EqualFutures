using EqualFutures.Core.Analysis;
using EqualFutures.Core.Education;
using EqualFutures.Core.Fairness;
using EqualFutures.Core.RealEstate;
using EqualFutures.Core.Recommendations;
using EqualFutures.Core.Retirement;
using Microsoft.Extensions.DependencyInjection;

namespace EqualFutures.Core;

/// <summary>Registers the EqualFutures calculation services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEqualFuturesCore(this IServiceCollection services)
    {
        services.AddScoped<IRealEstateCalculator, RealEstateCalculator>();
        services.AddScoped<IRetirementCalculator, RetirementCalculator>();
        services.AddScoped<IPartnerEquityCalculator, PartnerEquityCalculator>();
        services.AddScoped<IEducationCalculator, EducationCalculator>();
        services.AddScoped<IFairnessEngine, FairnessEngine>();
        services.AddScoped<IRecommendationEngine, RecommendationEngine>();
        services.AddScoped<IPlanAnalysisService, PlanAnalysisService>();
        return services;
    }
}
