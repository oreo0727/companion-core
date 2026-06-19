using Companion.Core.Abstractions;

namespace Companion.Worker.Services;

public class AgentRunWorker(IServiceScopeFactory scopeFactory, ILogger<AgentRunWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Agent run worker started. Polling every {PollIntervalSeconds} seconds.",
            PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var agentRuntime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
                var processedCount = await agentRuntime.ProcessPendingRunsAsync(stoppingToken);

                if (processedCount > 0)
                {
                    logger.LogInformation("Processed {ProcessedCount} agent run(s).", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing agent runs.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
