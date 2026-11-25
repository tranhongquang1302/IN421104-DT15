using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Reports;
using LangCenter.Tests.Support;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ClassTuitionReportApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task GetClassTuition_WithMixedBalances_ReturnsCorrectRowsAndTotals()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var scenario = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        var classId = scenario.TrainingClass.Id;
        var fullyPaidStudent = await ClassApiScenarios.CreateStudentAsync(admin);
        var fullyPaid = await TuitionApiScenarios.EnrollAsync(admin, classId, fullyPaidStudent.Id);
        var unpaidStudent = await ClassApiScenarios.CreateStudentAsync(admin);
        var unpaid = await TuitionApiScenarios.EnrollAsync(admin, classId, unpaidStudent.Id);
        var otherClass = await TuitionApiScenarios.CreateEnrollmentAsync(admin);
        using var firstInstallment = await TuitionApiScenarios.PayAsync(admin, scenario.Enrollment.Id, 200m);
        using var secondInstallment = await TuitionApiScenarios.PayAsync(admin, scenario.Enrollment.Id, 100m);
        using var fullPayment = await TuitionApiScenarios.PayAsync(admin, fullyPaid.Id, 1000m);
        using var unrelatedPayment = await TuitionApiScenarios.PayAsync(admin, otherClass.Enrollment.Id, 400m);
        Assert.Equal(HttpStatusCode.Created, firstInstallment.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondInstallment.StatusCode);
        Assert.Equal(HttpStatusCode.Created, fullPayment.StatusCode);
        Assert.Equal(HttpStatusCode.Created, unrelatedPayment.StatusCode);
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);

        var report = await cashier.GetFromJsonAsync<ClassTuitionReportDto>($"/api/reports/classes/{classId}/tuition");

        Assert.NotNull(report);
        Assert.Equal(classId, report.TrainingClassId);
        Assert.Equal(scenario.TrainingClass.Name, report.ClassName);
        Assert.Equal(3000m, report.TuitionDue);
        Assert.Equal(1300m, report.PaidAmount);
        Assert.Equal(1700m, report.OutstandingAmount);
        Assert.Equal(3, report.Enrollments.Count);
        Assert.Contains(new ClassTuitionReportRowDto(scenario.Enrollment.Id, scenario.Student.FullName, 1000m, 300m),
            report.Enrollments);
        Assert.Contains(new ClassTuitionReportRowDto(fullyPaid.Id, fullyPaidStudent.FullName, 1000m, 1000m), report.Enrollments);
        Assert.Contains(new ClassTuitionReportRowDto(unpaid.Id, unpaidStudent.FullName, 1000m, 0m), report.Enrollments);

        var pagedReport = await cashier.GetFromJsonAsync<PagedClassTuitionReportDto>(
            $"/api/reports/classes/{classId}/tuition?page=2&pageSize=1");
        Assert.NotNull(pagedReport);
        Assert.Equal(report.Enrollments[1], Assert.Single(pagedReport.Enrollments.Items));
        Assert.Equal(2, pagedReport.Enrollments.Page);
        Assert.Equal(1, pagedReport.Enrollments.PageSize);
        Assert.Equal(3, pagedReport.Enrollments.TotalCount);
        Assert.Equal(3, pagedReport.Enrollments.TotalPages);
        Assert.Equal(report.TuitionDue, pagedReport.TuitionDue);
        Assert.Equal(report.PaidAmount, pagedReport.PaidAmount);
        Assert.Equal(report.OutstandingAmount, pagedReport.OutstandingAmount);

        using var download = await cashier.GetAsync($"/api/reports/classes/{classId}/tuition.csv");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/csv", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        var csv = await download.Content.ReadAsStringAsync();
        Assert.Contains($"\"{scenario.Student.FullName}\",1000,300,700", csv);
        Assert.EndsWith("\"Tổng cộng\",3000,1300,1700\r\n", csv);
        Assert.Equal(5, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public async Task GetClassTuition_WithoutEnrollments_ReturnsEmptyReport()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var trainingClass = await ClassApiScenarios.CreateClassAsync(admin);

        var report = await admin.GetFromJsonAsync<ClassTuitionReportDto>($"/api/reports/classes/{trainingClass.Id}/tuition");

        Assert.NotNull(report);
        Assert.Empty(report.Enrollments);
        Assert.Equal(0m, report.TuitionDue);
        Assert.Equal(0m, report.PaidAmount);
        Assert.Equal(0m, report.OutstandingAmount);

        var pagedReport = await admin.GetFromJsonAsync<PagedClassTuitionReportDto>(
            $"/api/reports/classes/{trainingClass.Id}/tuition?page=1");
        Assert.NotNull(pagedReport);
        Assert.Empty(pagedReport.Enrollments.Items);
        Assert.Equal(0, pagedReport.Enrollments.TotalPages);
        Assert.Equal(0m, pagedReport.OutstandingAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".csv")]
    public async Task GetClassTuition_WhenClassDoesNotExist_ReturnsNotFound(string extension)
    {
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);

        using var response = await cashier.GetAsync($"/api/reports/classes/{Guid.NewGuid()}/tuition{extension}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".csv")]
    public async Task GetClassTuition_AsRegistrar_ReturnsForbidden(string extension)
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await ClassApiScenarios.CreateClassAsync(registrar);

        using var response = await registrar.GetAsync($"/api/reports/classes/{trainingClass.Id}/tuition{extension}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
