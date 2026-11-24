using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Tuition;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class TuitionApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task RecordPayment_InInstallments_UpdatesBalanceUntilPaidInFull()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(client);
        var enrollmentId = scenario.Enrollment.Id;
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 0m),
            await TuitionApiScenarios.GetBalanceAsync(client, enrollmentId));

        using var first = await TuitionApiScenarios.PayAsync(client, enrollmentId, 250m);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 250m),
            await TuitionApiScenarios.GetBalanceAsync(client, enrollmentId));

        using var last = await TuitionApiScenarios.PayAsync(client, enrollmentId, 750m);
        Assert.Equal(HttpStatusCode.Created, last.StatusCode);
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 1000m),
            await TuitionApiScenarios.GetBalanceAsync(client, enrollmentId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.5)]
    public async Task RecordPayment_WhenAmountIsInvalid_ReturnsBadRequest(double amount)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(client);

        using var response = await TuitionApiScenarios.PayAsync(client, scenario.Enrollment.Id, (decimal)amount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem?.Detail);
    }

    [Fact]
    public async Task RecordPayment_WhenAmountExceedsBalance_LeavesBalanceUnchanged()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(client);
        var enrollmentId = scenario.Enrollment.Id;
        using var first = await TuitionApiScenarios.PayAsync(client, enrollmentId, 750m);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var response = await TuitionApiScenarios.PayAsync(client, enrollmentId, 251m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 750m),
            await TuitionApiScenarios.GetBalanceAsync(client, enrollmentId));
    }

    [Fact]
    public async Task RecordPayment_WhenRequestIdIsReused_ReturnsOriginalPaymentOrConflict()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(client);
        var enrollmentId = scenario.Enrollment.Id;
        var requestId = Guid.NewGuid();

        using var first = await TuitionApiScenarios.PayAsync(client, enrollmentId, 1000m, requestId);
        using var repeated = await TuitionApiScenarios.PayAsync(client, enrollmentId, 1000m, requestId);
        using var conflicting = await TuitionApiScenarios.PayAsync(client, enrollmentId, 500m, requestId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, repeated.StatusCode);
        var original = await first.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.NotNull(original);
        Assert.Equal(original, await repeated.Content.ReadFromJsonAsync<PaymentDto>());
        Assert.Equal(HttpStatusCode.Conflict, conflicting.StatusCode);
        Assert.Equal(new TuitionBalanceDto(enrollmentId, 1000m, 1000m),
            await TuitionApiScenarios.GetBalanceAsync(client, enrollmentId));
    }

    [Fact]
    public async Task RecordPayment_AsRegistrar_ReturnsForbiddenButAllowsBalance()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);

        using var response = await TuitionApiScenarios.PayAsync(registrar, scenario.Enrollment.Id, 100m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(new TuitionBalanceDto(scenario.Enrollment.Id, 1000m, 0m),
            await TuitionApiScenarios.GetBalanceAsync(registrar, scenario.Enrollment.Id));
    }

    [Fact]
    public async Task FindEnrollments_AsCashier_FindsStudentByEmailAndCanRecordPayment()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        var email = Uri.EscapeDataString(scenario.Student.Email.ToUpperInvariant());

        var enrollments = await cashier.GetFromJsonAsync<EnrollmentLookupDto[]>($"/api/enrollments?studentEmail={email}");

        Assert.Equal(scenario.Enrollment.Id, Assert.Single(enrollments!).EnrollmentId);
        using var response = await TuitionApiScenarios.PayAsync(cashier, scenario.Enrollment.Id, 100m);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var missing = await TuitionApiScenarios.PayAsync(cashier, Guid.NewGuid(), 100m);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
