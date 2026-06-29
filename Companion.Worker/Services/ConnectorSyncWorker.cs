using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Worker.Services;

public class ConnectorSyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ConnectorSyncWorker> logger) : BackgroundService
{
    private readonly TimeSpan pollInterval = TimeSpan.FromSeconds(
        Math.Max(configuration.GetValue<int?>("ConnectorSyncWorker:PollIntervalSeconds") ?? 60, 10));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Connector sync worker started. Polling every {PollIntervalSeconds} seconds.",
            pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();
                var syncService = scope.ServiceProvider.GetRequiredService<IConnectorSyncService>();
                var now = DateTime.UtcNow;

                var connections = await dbContext.ConnectorConnections
                    .AsNoTracking()
                    .Include(x => x.ConnectorDefinition)
                    .Where(x =>
                        x.Status == ConnectorConnectionStatus.Connected &&
                        x.ConnectorDefinition != null &&
                        x.ConnectorDefinition.SupportsOAuth &&
                        x.ConnectorDefinition.Enabled)
                    .ToListAsync(stoppingToken);

                var processedCount = 0;
                foreach (var connection in connections.Where(x => IsDue(x.ConnectorDefinition!.Provider, x.UpdatedUtc, now)))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await syncService.SyncAsync(connection.UserProfileId, connection.Id, stoppingToken);
                    processedCount++;
                }

                if (processedCount > 0)
                {
                    logger.LogInformation("Processed {ProcessedCount} connector sync(s).", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing connector syncs.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private static bool IsDue(string provider, DateTime updatedUtc, DateTime now)
    {
        var interval = provider switch
        {
            ConnectorProviders.GoogleCalendar or ConnectorProviders.MicrosoftCalendar => TimeSpan.FromMinutes(15),
            ConnectorProviders.Gmail or ConnectorProviders.OutlookMail => TimeSpan.FromMinutes(10),
            ConnectorProviders.GoogleDrive or ConnectorProviders.OneDrive => TimeSpan.FromHours(1),
            ConnectorProviders.GooglePeople => TimeSpan.FromDays(1),
            _ => TimeSpan.FromHours(1)
        };

        return now - updatedUtc >= interval;
    }
}
