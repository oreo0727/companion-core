using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Companion.Infrastructure;

public static class ServiceProviderExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Companion.Infrastructure.Database");

        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CompanionDbContext>();

                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database is ready after {Attempt} attempt(s).", attempt);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Database initialization attempt {Attempt} failed. Retrying in 3 seconds.",
                    attempt);

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        await using var finalScope = services.CreateAsyncScope();
        var finalContext = finalScope.ServiceProvider.GetRequiredService<CompanionDbContext>();
        await finalContext.Database.MigrateAsync(cancellationToken);
    }
}
