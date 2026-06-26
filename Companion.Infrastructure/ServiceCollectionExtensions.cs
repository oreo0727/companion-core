using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Constants;
using Companion.Infrastructure.Persistence;
using Companion.Infrastructure.Services;
using Companion.Infrastructure.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Companion.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddDataProtection();

        services.AddDbContext<CompanionDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(CompanionDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<CompanionDbContext>();

        services.AddHttpClient<OpenAIProvider>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient<AnthropicProvider>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient<OllamaProvider>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient<GoogleCalendarReadConnector>();
        services.AddHttpClient<GoogleDriveReadConnector>();
        services.AddHttpClient<GmailReadConnector>();
        services.AddHttpClient<MicrosoftCalendarReadConnector>();
        services.AddHttpClient<OneDriveReadConnector>();
        services.AddHttpClient<OutlookMailReadConnector>();

        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISecretStore, DataProtectionSecretStore>();
        services.AddSingleton<IOAuthTokenProtector, OAuthTokenProtector>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<ICalendarReadConnector, LocalCalendarReadConnector>();
        services.AddScoped<IEmailReadConnector, LocalEmailReadConnector>();
        services.AddScoped<ICalendarReadConnector>(serviceProvider => serviceProvider.GetRequiredService<GoogleCalendarReadConnector>());
        services.AddScoped<ICalendarReadConnector>(serviceProvider => serviceProvider.GetRequiredService<MicrosoftCalendarReadConnector>());
        services.AddScoped<IEmailReadConnector>(serviceProvider => serviceProvider.GetRequiredService<GmailReadConnector>());
        services.AddScoped<IEmailReadConnector>(serviceProvider => serviceProvider.GetRequiredService<OutlookMailReadConnector>());
        services.AddScoped<IFileReadConnector>(serviceProvider => serviceProvider.GetRequiredService<GoogleDriveReadConnector>());
        services.AddScoped<IFileReadConnector>(serviceProvider => serviceProvider.GetRequiredService<OneDriveReadConnector>());
        services.AddScoped<IConnector, LocalCalendarReadConnector>();
        services.AddScoped<IConnector, LocalEmailReadConnector>();
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<GoogleCalendarReadConnector>());
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<GoogleDriveReadConnector>());
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<GmailReadConnector>());
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<MicrosoftCalendarReadConnector>());
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<OneDriveReadConnector>());
        services.AddScoped<IConnector>(serviceProvider => serviceProvider.GetRequiredService<OutlookMailReadConnector>());
        services.AddScoped<IConnectorRegistry, ConnectorRegistry>();
        services.AddScoped<IConnectorSyncService, ConnectorSyncService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IKnowledgeImporter, KnowledgeImporter>();
        services.AddScoped<IKnowledgeSearchService, KnowledgeSearchService>();
        services.AddScoped<ITool, MemorySearchTool>();
        services.AddScoped<ITool, CreateTaskTool>();
        services.AddScoped<ITool, GetBriefingTool>();
        services.AddScoped<ITool, KnowledgeSearchTool>();
        services.AddScoped<ITool, CalendarEventsTool>();
        services.AddScoped<ITool, EmailSearchTool>();
        services.AddScoped<ITool, CreateReminderTool>();
        services.AddScoped<ITool, ListNotificationsTool>();
        services.AddScoped<ITool, DesktopReadFileTool>();
        services.AddScoped<ITool, DesktopWriteFileTool>();
        services.AddScoped<ITool, DesktopLaunchApplicationTool>();
        services.AddScoped<ITool, DesktopCaptureScreenshotTool>();
        services.AddScoped<ITool, DesktopGetClipboardTool>();
        services.AddScoped<ITool, DesktopSetClipboardTool>();
        services.AddScoped<ITool, DesktopRunTerminalTool>();
        services.AddScoped<ITool, DesktopSendKeyboardTool>();
        services.AddScoped<ITool, DesktopMoveMouseTool>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IToolExecutor, ToolExecutor>();
        services.AddScoped<IContextBuilder, ContextBuilder>();
        services.AddScoped<IReasoningEngine, ChiefOfStaffReasoningEngine>();
        services.AddScoped<IMemoryExtractionService, MemoryExtractionService>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<IAIProvider>(serviceProvider => serviceProvider.GetRequiredService<OpenAIProvider>());
        services.AddScoped<IAIProvider>(serviceProvider => serviceProvider.GetRequiredService<AnthropicProvider>());
        services.AddScoped<IAIProvider>(serviceProvider => serviceProvider.GetRequiredService<OllamaProvider>());

        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IOpenLoopService, OpenLoopService>();
        services.AddScoped<IChiefOfStaffService, ChiefOfStaffService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IAgentRuntime, AgentRuntime>();
        services.AddScoped<IConnectorManager, ConnectorManager>();
        services.AddSingleton<IDesktopConnector, LocalDesktopConnector>();
        services.AddScoped<ISpeechToTextProvider, OpenAISpeechToTextProvider>();
        services.AddScoped<ISpeechToTextProvider, AzureSpeechToTextProvider>();
        services.AddScoped<ISpeechToTextProvider, LocalWhisperSpeechToTextProvider>();
        services.AddScoped<ITextToSpeechProvider, OpenAITextToSpeechProvider>();
        services.AddScoped<ITextToSpeechProvider, AzureTextToSpeechProvider>();
        services.AddScoped<ITextToSpeechProvider, LocalPiperTextToSpeechProvider>();
        services.AddScoped<IVoiceProviderRegistry, VoiceProviderRegistry>();
        services.AddScoped<IVoiceSessionService, VoiceSessionService>();

        return services;
    }
}
