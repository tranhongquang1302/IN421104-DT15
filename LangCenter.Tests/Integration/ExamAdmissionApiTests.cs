using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Exams;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ExamAdmissionApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public async Task Confirm_WhenTuitionIsOutstanding_ReturnsBadRequest(int paidAmount)
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(registrar);
        await TuitionApiScenarios.LaunchAsync(registrar, scenario.TrainingClass.Id);
        if (paidAmount > 0)
        {
            using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
            using var payment = await TuitionApiScenarios.PayAsync(cashier, scenario.Enrollment.Id, paidAmount);
            Assert.Equal(HttpStatusCode.Created, payment.StatusCode);
        }

        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(registrar, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Học viên còn nợ học phí nên chưa đủ điều kiện dự thi.",
            (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Detail);
    }

    [Fact]
    public async Task Confirm_WhenPaidButClassIsOpen_ReturnsBadRequest()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        using var payment = await TuitionApiScenarios.PayAsync(admin, scenario.Enrollment.Id, 1000m);
        Assert.Equal(HttpStatusCode.Created, payment.StatusCode);

        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(admin, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Lớp chưa khai giảng.", (await response.Content.ReadFromJsonAsync<ProblemDetails>())?.Detail);
    }

    [Fact]
    public async Task Confirm_WhenPaidAndClassStarted_SavesOneAdmission()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        await TuitionApiScenarios.LaunchAsync(admin, scenario.TrainingClass.Id);
        using var payment = await TuitionApiScenarios.PayAsync(admin, scenario.Enrollment.Id, 1000m);
        Assert.Equal(HttpStatusCode.Created, payment.StatusCode);
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);

        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(registrar, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var admission = await response.Content.ReadFromJsonAsync<ExamAdmissionDto>();
        Assert.NotNull(admission);
        Assert.NotEqual(Guid.Empty, admission.Id);
        Assert.Equal(scenario.Enrollment.Id, admission.EnrollmentId);
        Assert.Equal(DateTimeKind.Utc, admission.AdmittedAt.Kind);
        using var repeated = await TuitionApiScenarios.ConfirmExamAdmissionAsync(registrar, scenario.Enrollment.Id);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenTuitionIsZeroAndClassStarted_ReturnsCreated()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(registrar, tuitionFee: 0m);
        await TuitionApiScenarios.LaunchAsync(registrar, scenario.TrainingClass.Id);

        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(registrar, scenario.Enrollment.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenEnrollmentIsMissing_ReturnsNotFound()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(registrar, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_AsCashier_ReturnsForbidden()
    {
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var response = await TuitionApiScenarios.ConfirmExamAdmissionAsync(cashier, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
