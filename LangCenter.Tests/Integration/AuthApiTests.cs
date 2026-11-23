using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LangCenter.API.Controllers;
using LangCenter.API.Security;
using LangCenter.Domain.Entities;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LangCenter.Tests.Integration;

public class AuthApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsBearerTokenAndAccount()
    {
        using var client = factory.CreateClient();
        var login = await AuthenticatedApiFactory.LoginAsync(client, AuthenticatedApiFactory.AdminEmail);

        Assert.Equal("Bearer", login.TokenType);
        Assert.Equal(1800, login.ExpiresIn);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<CurrentAccountResponse>();
        Assert.NotNull(account);
        Assert.True(Guid.TryParse(account.Id, out _));
        Assert.Equal(AuthenticatedApiFactory.AdminEmail, account.Email);
        Assert.Contains(Roles.Admin, account.Roles);
    }

    [Theory]
    [InlineData(AuthenticatedApiFactory.AdminEmail)]
    [InlineData("missing@langcenter.local")]
    public async Task Login_WithInvalidCredentials_ReturnsGenericUnauthorized(string email)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "incorrect" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email hoặc mật khẩu không đúng.", problem?.Detail);
    }

    [Fact]
    public async Task GetCurrentAccount_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentAccount_WithInvalidSignature_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        var session = await AuthenticatedApiFactory.LoginAsync(client, AuthenticatedApiFactory.RegistrarEmail);
        var parts = session.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'A' ? 'B' : 'A') + parts[2][1..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parts));

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthenticatedApiFactory.AdminEmail, HttpStatusCode.NoContent)]
    [InlineData(AuthenticatedApiFactory.RegistrarEmail, HttpStatusCode.NoContent)]
    [InlineData(AuthenticatedApiFactory.CashierEmail, HttpStatusCode.Forbidden)]
    public async Task ManageStudents_WithStaffToken_EnforcesRolePolicy(string email, HttpStatusCode expected)
    {
        using var testApp = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(PolicyProbeController).Assembly)));
        using var client = testApp.CreateClient();
        var session = await AuthenticatedApiFactory.LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        using var response = await client.GetAsync("/test-policies/manage-students");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApi_InDevelopment_ListsBearerForProtectedOperations()
    {
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = document.GetProperty("paths");

        var login = paths.GetProperty("/api/auth/login").GetProperty("post");
        Assert.True(!login.TryGetProperty("security", out var loginSecurity) || loginSecurity.GetArrayLength() == 0);
        var accountSecurity = paths.GetProperty("/api/auth/me").GetProperty("get").GetProperty("security");
        Assert.True(accountSecurity[0].TryGetProperty("Bearer", out _));
    }
}

[ApiController]
[Route("test-policies")]
public class PolicyProbeController : ControllerBase
{
    [HttpGet("manage-students")]
    [Authorize(Policy = Policies.ManageStudents)]
    public IActionResult ManageStudents() => NoContent();
}
