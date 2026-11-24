using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using Xunit;

namespace LangCenter.Tests.Support;

public static class CertificateApiScenarios
{
    public static async Task<TuitionScenario> CreateAdmittedEnrollmentAsync(HttpClient client)
    {
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(client, tuitionFee: 0m);
        await TuitionApiScenarios.LaunchAsync(client, scenario.TrainingClass.Id);
        using var admission = await TuitionApiScenarios.ConfirmExamAdmissionAsync(client, scenario.Enrollment.Id);
        Assert.Equal(HttpStatusCode.Created, admission.StatusCode);
        return scenario;
    }

    public static Task<HttpResponseMessage> IssueAsync(HttpClient client, Guid enrollmentId, bool? completionConfirmed = true) =>
        client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/certificate", new IssueCertificateRequest(completionConfirmed));
}
