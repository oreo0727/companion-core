using Companion.Infrastructure.Persistence;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Companion.Infrastructure;

public static class ServiceProviderExtensions
{
    private const long MigrationAdvisoryLockId = 5_108_202_406_250_001;

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

                await MigrateWithCoordinationAsync(dbContext, cancellationToken);
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
        await MigrateWithCoordinationAsync(finalContext, cancellationToken);
    }

    private static async Task MigrateWithCoordinationAsync(CompanionDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!UsesPostgres(dbContext))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await ExecuteLockCommandAsync(
                dbContext.Database.GetDbConnection(),
                "SELECT pg_advisory_lock(@lockId)",
                cancellationToken);

            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            try
            {
                await ExecuteLockCommandAsync(
                    dbContext.Database.GetDbConnection(),
                    "SELECT pg_advisory_unlock(@lockId)",
                    cancellationToken);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private static bool UsesPostgres(CompanionDbContext dbContext)
    {
        return dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static async Task ExecuteLockCommandAsync(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockId";
        parameter.Value = MigrationAdvisoryLockId;
        command.Parameters.Add(parameter);

        await command.ExecuteScalarAsync(cancellationToken);
    }
}
