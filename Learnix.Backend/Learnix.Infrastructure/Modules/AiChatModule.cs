using Anthropic.SDK;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Services;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Options;
using Learnix.Infrastructure.AiChat;
using Learnix.Infrastructure.AiChat.Anthropic;
using Learnix.Infrastructure.AiChat.Gemini;
using Learnix.Infrastructure.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Modules;

/// <summary>
/// AI chat: the provider selected by <c>AiChat:Provider</c>, the tools it may call,
/// the streaming orchestrator and the availability store.
/// </summary>
public static class AiChatModule
{
    private const string AnthropicProvider = "Anthropic";
    private const string GeminiProvider = "Gemini";

    public static IServiceCollection AddAiChat(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiChatSection = configuration.GetSection(ConfigurationSectionNameConstants.AiChat);

        services.Configure<AnthropicOptions>(configuration.GetSection(ConfigurationSectionNameConstants.Anthropic));
        services.Configure<GeminiOptions>(configuration.GetSection(ConfigurationSectionNameConstants.Gemini));
        services.Configure<AiChatOptions>(aiChatSection);

        // Bound eagerly: which provider to register is a composition-time decision, so it cannot
        // wait for IOptions<T> to be resolved from the container that is still being built.
        var aiChatOptions = aiChatSection.Get<AiChatOptions>()
            ?? throw new InvalidOperationException(
                $"Missing '{ConfigurationSectionNameConstants.AiChat}' configuration section.");

        // There is no default provider: an unrecognised name has to fail here, at startup. Registering
        // nothing would leave IAiChatProvider unresolvable, and the misconfiguration would surface as a
        // 500 for the first user who opens the chat, long after the deploy that caused it.
        if (AnthropicProvider.Equals(aiChatOptions.Provider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(sp => new AnthropicClient(new APIAuthentication(
                sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey)));
            services.AddScoped<IAiChatProvider, AnthropicChatProvider>();
        }
        else if (GeminiProvider.Equals(aiChatOptions.Provider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAiChatProvider, GeminiChatProvider>();
        }
        else
        {
            throw new InvalidOperationException(
                $"'{ConfigurationSectionNameConstants.AiChat}:Provider' is '{aiChatOptions.Provider}'. " +
                $"Expected '{AnthropicProvider}' or '{GeminiProvider}'.");
        }

        services.AddScoped<IChatTool, SearchCoursesTool>();
        services.AddScoped<IChatTool, GetCategoriesTool>();
        services.AddScoped<IChatTool, GetInstructorCoursesTool>();
        services.AddScoped<IChatTool, GetMyLearningProfileTool>();
        services.AddScoped<IChatTool, GetCurrentLessonTool>();
        services.AddScoped<IChatTool, GetTestReviewTool>();
        services.AddSingleton<IChatTool, GetPlatformInfoTool>();

        services.AddScoped<ChatScopeAuthorizer>();
        services.AddScoped<ChatStreamOrchestrator>();
        services.AddScoped<IAiAvailabilityStore, RedisAiAvailabilityStore>();

        return services;
    }
}
