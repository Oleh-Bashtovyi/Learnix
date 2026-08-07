using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Learnix.API.Swagger;

/// <summary>
/// Registers one Swagger document per API version discovered by <see cref="IApiVersionDescriptionProvider"/>,
/// so adding a new <c>[ApiVersion]</c> to a controller automatically gets its own OpenAPI document
/// without touching <c>Program.cs</c>.
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Learnix API",
                Version = description.ApiVersion.ToString(),
                Description = "Learning Management System — REST API"
                    + (description.IsDeprecated ? " (deprecated)" : string.Empty)
            });
        }
    }
}
