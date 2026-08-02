using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Infrastructure.Services.Achievements;
using Learnix.Infrastructure.Services.Catalog;
using Learnix.Infrastructure.Services.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Modules;

/// <summary>Course catalog read services and the achievement evaluator.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddScoped<IPublicCourseCatalogSearchService, PublicCourseCatalogSearchService>();
        services.AddScoped<IAiCourseSearchService, AiCourseSearchService>();
        services.AddScoped<IFeaturedCoursesService, FeaturedCoursesService>();
        services.AddScoped<IAchievementEvaluator, AchievementEvaluator>();

        return services;
    }
}
