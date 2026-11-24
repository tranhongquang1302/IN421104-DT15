using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Certificates;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class CertificateApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Issue_WithConfirmedCompletionAndAdmission_PublishesOnlyVerificationFields()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await CertificateApiScenarios.CreateAdmittedEnrollmentAsync(registrar);
        registrar.DefaultRequestHeaders.Host = "unrelated.example.test";

        using var response = await CertificateApiScenarios.IssueAsync(registrar, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var certificate = await response.Content.ReadFromJsonAsync<CertificateResponse>();
        Assert.NotNull(certificate);
        Assert.Equal(scenario.Enrollment.Id, certificate.EnrollmentId);
        Assert.Matches("^[0-9A-F]{32}$", certificate.LookupCode);
        Assert.Equal(DateTimeKind.Utc, certificate.IssuedAt.Kind);
        Assert.Equal($"http://localhost:5080/api/certificates/verify/{certificate.LookupCode}", certificate.VerifyUrl.AbsoluteUri);
        Assert.Equal(certificate.VerifyUrl, response.Headers.Location);

        using var anonymous = factory.CreateClient();
        using var verification = await anonymous.GetAsync(certificate.VerifyUrl.AbsolutePath.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        Assert.True(verification.Headers.CacheControl?.NoStore);
        var payload = await verification.Content.ReadFromJsonAsync<JsonElement>();
        var summary = payload.Deserialize<CertificateVerificationDto>(JsonSerializerOptions.Web);
        Assert.Equal(new CertificateVerificationDto(scenario.Student.FullName, scenario.TrainingClass.Name, certificate.IssuedAt), summary);
        Assert.Equal(new[] { "className", "issuedAt", "studentName" },
            payload.EnumerateObject().Select(property => property.Name).Order());

        using var repeated = await CertificateApiScenarios.IssueAsync(registrar, scenario.Enrollment.Id);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [Fact]
    public async Task Issue_WhenCompletionIsFalseOrMissing_ReturnsBadRequest()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await CertificateApiScenarios.CreateAdmittedEnrollmentAsync(registrar);

        using var notConfirmed = await CertificateApiScenarios.IssueAsync(registrar, scenario.Enrollment.Id, false);
        using var missing = await registrar.PostAsJsonAsync($"/api/enrollments/{scenario.Enrollment.Id}/certificate", new { });

        Assert.Equal(HttpStatusCode.BadRequest, notConfirmed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var problem = await notConfirmed.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("hoàn thành khóa học", problem?.Detail);
    }

    [Fact]
    public async Task Issue_WithoutExamAdmission_ReturnsBadRequest()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(registrar);
        await TuitionApiScenarios.LaunchAsync(registrar, scenario.TrainingClass.Id);

        using var response = await CertificateApiScenarios.IssueAsync(registrar, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("đủ điều kiện dự thi", problem?.Detail);
    }

    [Fact]
    public async Task GetQr_ForIssuedCertificate_ReturnsPngAndRequiresAuthorization()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await CertificateApiScenarios.CreateAdmittedEnrollmentAsync(admin);
        using var issued = await CertificateApiScenarios.IssueAsync(admin, scenario.Enrollment.Id);
        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        var certificate = await issued.Content.ReadFromJsonAsync<CertificateResponse>();
        Assert.NotNull(certificate);
        var path = $"/api/certificates/{certificate.Id}/qr";

        using var image = await admin.GetAsync(path);
        using var anonymous = factory.CreateClient();
        using var unauthorized = await anonymous.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        var bytes = await image.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task IssueAndDownloadQr_AsCashier_ReturnsForbidden()
    {
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);

        using var issue = await CertificateApiScenarios.IssueAsync(cashier, Guid.NewGuid());
        using var qr = await cashier.GetAsync($"/api/certificates/{Guid.NewGuid()}/qr");

        Assert.Equal(HttpStatusCode.Forbidden, issue.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, qr.StatusCode);
    }

    [Fact]
    public async Task CertificateEndpoints_WhenResourcesDoNotExist_ReturnNotFound()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var anonymous = factory.CreateClient();

        using var issue = await CertificateApiScenarios.IssueAsync(registrar, Guid.NewGuid());
        using var qr = await registrar.GetAsync($"/api/certificates/{Guid.NewGuid()}/qr");
        using var unknown = await anonymous.GetAsync($"/api/certificates/verify/{Guid.NewGuid():N}");
        using var malformed = await anonymous.GetAsync("/api/certificates/verify/invalid");

        Assert.Equal(HttpStatusCode.NotFound, issue.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, qr.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
    }
}
