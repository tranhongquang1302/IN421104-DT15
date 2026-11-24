using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LangCenter.Application.Reports;
using LangCenter.Application.Tuition;
using LangCenter.Tests.Support;
using Xunit;

namespace LangCenter.Tests.Integration;

public class PostgresTuitionApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [PostgresFact]
    public async Task RecordPayment_WhenConcurrentPaymentsWouldOverpay_RecordsOnlyOne()
    {
        using var admin = await CreateAdminClientAsync();
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        var enrollmentId = scenario.Enrollment.Id;

        var first = TuitionApiScenarios.PayAsync(admin, enrollmentId, 750m);
        var second = TuitionApiScenarios.PayAsync(admin, enrollmentId, 750m);
        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;

        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.BadRequest, statuses);
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 750m),
            await TuitionApiScenarios.GetBalanceAsync(admin, enrollmentId));
        var report = await admin.GetFromJsonAsync<ClassTuitionReportDto>(
            $"/api/reports/classes/{scenario.TrainingClass.Id}/tuition");
        Assert.NotNull(report);
        Assert.Equal(750m, report.PaidAmount);
        Assert.Equal(250m, Assert.Single(report.Enrollments).OutstandingAmount);
    }

    [PostgresFact]
    public async Task RecordPayment_WhenSameRequestArrivesConcurrently_ReturnsOnePayment()
    {
        using var admin = await CreateAdminClientAsync();
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        var enrollmentId = scenario.Enrollment.Id;
        var requestId = Guid.NewGuid();

        var first = TuitionApiScenarios.PayAsync(admin, enrollmentId, 400m, requestId);
        var second = TuitionApiScenarios.PayAsync(admin, enrollmentId, 400m, requestId);
        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var payment = await firstResponse.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.NotNull(payment);
        Assert.Equal(payment, await secondResponse.Content.ReadFromJsonAsync<PaymentDto>());
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 400m),
            await TuitionApiScenarios.GetBalanceAsync(admin, enrollmentId));
    }

    [PostgresFact]
    public async Task Confirm_WhenFinalPaymentIsConcurrent_FollowsCommittedBalance()
    {
        using var admin = await CreateAdminClientAsync();
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        var enrollmentId = scenario.Enrollment.Id;
        await TuitionApiScenarios.LaunchAsync(admin, scenario.TrainingClass.Id);

        var payment = TuitionApiScenarios.PayAsync(admin, enrollmentId, 1000m);
        var admission = TuitionApiScenarios.ConfirmExamAdmissionAsync(admin, enrollmentId);
        await Task.WhenAll(payment, admission);
        using var paymentResponse = await payment;
        using var admissionResponse = await admission;

        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        Assert.Contains(admissionResponse.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.BadRequest });
        using var repeated = await TuitionApiScenarios.ConfirmExamAdmissionAsync(admin, enrollmentId);
        Assert.Equal(admissionResponse.StatusCode == HttpStatusCode.Created ? HttpStatusCode.Conflict : HttpStatusCode.Created,
            repeated.StatusCode);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = factory.CreateClient();
        try
        {
            var login = await AuthenticatedApiFactory.LoginAsync(client, AuthenticatedApiFactory.AdminEmail);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}
