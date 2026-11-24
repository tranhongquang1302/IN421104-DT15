using LangCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace LangCenter.Tests.Support;

public sealed class PostgresFactAttribute : FactAttribute
{
    internal const string ConnectionStringVariable = "LANGCENTER_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
            Skip = $"Set {ConnectionStringVariable} to run PostgreSQL integration tests.";
    }
}

public class PostgresApiFactory : ApiFactory, IAsyncLifetime
{
    private readonly string? _connectionString = CreateConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        if (_connectionString is not null)
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Database:ApplyMigrations", "false");
    }

    public async Task InitializeAsync()
    {
        if (_connectionString is null)
            return;

        await using var scope = Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<LangCenterDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<DbSeeder>().SeedDevelopmentAccountsAsync(AuthenticatedApiFactory.Password);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            if (_connectionString is not null)
            {
                await using var scope = Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<LangCenterDbContext>().Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            await base.DisposeAsync();
        }
    }

    private static string? CreateConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = $"langcenter_tuition_{Guid.NewGuid():N}",
            Pooling = false
        }.ConnectionString;
    }
}
