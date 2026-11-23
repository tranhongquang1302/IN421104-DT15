using System.Net.Http.Headers;
using System.Net.Http.Json;
using LangCenter.Application.Auth;
using LangCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LangCenter.Tests.Support;

public class AuthenticatedApiFactory : ApiFactory, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string AdminEmail = "admin@langcenter.local";
    public const string RegistrarEmail = "registrar@langcenter.local";
    public const string CashierEmail = "cashier@langcenter.local";
    public static string Password { get; } = $"Aa1!{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<LangCenterDbContext>>();
            services.RemoveAll<DbContextOptions<LangCenterDbContext>>();
            services.AddDbContext<LangCenterDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LangCenterDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedDevelopmentAccountsAsync(Password);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public static async Task<LoginResult> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResult>())!;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateClient();
        try
        {
            var session = await LoginAsync(client, email);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}
