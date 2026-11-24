using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Certificates;
using LangCenter.Tests.Support;
using Xunit;

namespace LangCenter.Tests.Integration;

public class PostgresCertificateApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [PostgresFact]
    public async Task Issue_WhenRequestsArriveConcurrently_CreatesOnePubliclyVerifiableCertificate()
    {
        using var registrar = factory.CreateClient();
        var login = await AuthenticatedApiFactory.LoginAsync(registrar, AuthenticatedApiFactory.RegistrarEmail);
        registrar.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var scenario = await CertificateApiScenarios.CreateAdmittedEnrollmentAsync(registrar);
        var enrollmentId = scenario.Enrollment.Id;

        var first = CertificateApiScenarios.IssueAsync(registrar, enrollmentId);
        var second = CertificateApiScenarios.IssueAsync(registrar, enrollmentId);
        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;
        var responses = new[] { firstResponse, secondResponse };

        var created = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var certificate = await created.Content.ReadFromJsonAsync<CertificateResponse>();
        Assert.NotNull(certificate);
        using var anonymous = factory.CreateClient();
        var verified = await anonymous.GetFromJsonAsync<CertificateVerificationDto>(certificate.VerifyUrl.AbsolutePath);
        Assert.Equal(new CertificateVerificationDto(scenario.Student.FullName, scenario.TrainingClass.Name, certificate.IssuedAt), verified);
    }
}
