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

        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISecretStore, DataProtectionSecretStore>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<ICalendarReadConnector, LocalCalendarReadConnector>();
        services.AddScoped<IConnector, LocalCalendarReadConnector>();
        services.AddScoped<IConnectorRegistry, ConnectorRegistry>();
        services.AddScoped<IConnectorSyncService, ConnectorSyncService>();
        services.AddScoped<IKnowledgeImporter, KnowledgeImporter>();
        services.AddScoped<IKnowledgeSearchService, KnowledgeSearchService>();
        services.AddScoped<ITool, MemorySearchTool>();
        services.AddScoped<ITool, CreateTaskTool>();
        services.AddScoped<ITool, GetBriefingTool>();
        services.AddScoped<ITool, KnowledgeSearchTool>();
        services.AddScoped<ITool, CalendarEventsTool>();
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

        return services;
    }
}
