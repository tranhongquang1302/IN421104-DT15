using System.Net;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LangCenter.Tests.Integration;

public class WorkspacePortalTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task OpenWorkspace_WithoutSession_RedirectsToLoginAndLoadsCss()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var root = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
        Assert.Contains("/login", root.Headers.Location?.OriginalString);
        var login = await PortalForms.GetPageAsync(client, "/login");
        Assert.Contains("Đăng nhập", login);
        Assert.Contains("Tra cứu chứng chỉ", login);
        using var css = await client.GetAsync("/css/langcenter.css");
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_AsAdmin_ProvidesBothWorkspacesAndKeepsApiJwtOnly()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await PortalForms.LoginAsync(client, AuthenticatedApiFactory.AdminEmail, "https://unrelated.example.test/");
        var home = await PortalForms.GetPageAsync(client, "/workspace");
        Assert.Contains("Quản trị viên", home);
        Assert.Contains("href=\"/workspace/students\"", home);
        Assert.Contains("href=\"/workspace/classes\"", home);
        Assert.Contains("href=\"/workspace/tuition\"", home);
        Assert.Contains("href=\"/workspace/students#new-student\"", home);
        Assert.Contains("href=\"/workspace/classes#new-class\"", home);
        Assert.Contains("Thu học phí", home);
        Assert.Contains("href=\"/certificates/verify\"", home);
        await PortalForms.GetPageAsync(client, "/workspace/students");
        await PortalForms.GetPageAsync(client, "/workspace/classes");
        await PortalForms.GetPageAsync(client, "/workspace/tuition");
        using var api = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);

        using var logout = await PortalForms.PostAsync(client, "/logout", home);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        using var afterLogout = await client.GetAsync("/workspace");
        Assert.Contains("/login", afterLogout.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryOrWithInvalidPassword_DoesNotIssueSession()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var fields = new Dictionary<string, string>
        {
            ["Input.Email"] = AuthenticatedApiFactory.AdminEmail,
            ["Input.Password"] = "invalid-demo-password"
        };
        using var missingToken = await client.PostAsync("/login", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        var login = await PortalForms.GetPageAsync(client, "/login");
        using var invalid = await PortalForms.PostAsync(client, "/login", login, fields);
        var rejected = await PortalForms.ReadHtmlAsync(invalid);
        Assert.Contains("Email hoặc mật khẩu không đúng.", rejected);
        Assert.DoesNotContain("invalid-demo-password", rejected);
        using var workspace = await client.GetAsync("/workspace");
        Assert.Contains("/login", workspace.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CompleteCourse_ThroughRolePages_ProducesPublicCertificate()
    {
        using var registrar = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var cashier = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var visitor = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await PortalForms.LoginAsync(registrar, AuthenticatedApiFactory.RegistrarEmail);
        await PortalForms.LoginAsync(cashier, AuthenticatedApiFactory.CashierEmail);
        await PortalWorkflow.ExerciseAsync(registrar, cashier, visitor);
    }
}
