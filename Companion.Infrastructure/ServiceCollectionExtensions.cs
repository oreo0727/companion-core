using Companion.Core.Abstractions;
using Companion.Infrastructure.Persistence;
using Companion.Infrastructure.Services;
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

        services.AddDbContext<CompanionDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(CompanionDbContext).Assembly.FullName)));

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
