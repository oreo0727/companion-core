using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Companion.Infrastructure.Persistence;

public class CompanionDbContextFactory : IDesignTimeDbContextFactory<CompanionDbContext>
{
    public CompanionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMPANION_CORE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=companion_core;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CompanionDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(CompanionDbContext).Assembly.FullName));

        return new CompanionDbContext(optionsBuilder.Options);
    }
}
