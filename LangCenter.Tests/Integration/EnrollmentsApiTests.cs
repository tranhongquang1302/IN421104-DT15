using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Enrollments;
using LangCenter.Infrastructure.Persistence;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LangCenter.Tests.Integration;

public class EnrollmentsApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Create_WhenClassIsOpen_SavesEnrollmentWithTuitionSnapshot()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await ClassApiScenarios.CreateClassAsync(client, minimumEnrollment: 3, tuitionFee: 1_200_000m);
        var student = await ClassApiScenarios.CreateStudentAsync(client);

        using var response = await ClassApiScenarios.EnrollAsync(client, trainingClass.Id, student.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollmentDto>();
        Assert.NotNull(enrollment);
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(trainingClass.Id, enrollment.TrainingClassId);
        Assert.Equal(trainingClass.TuitionFee, enrollment.TuitionDue);
        Assert.Equal(DateTimeKind.Utc, enrollment.EnrolledAt.Kind);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LangCenterDbContext>();
        var stored = await db.Enrollments.SingleAsync(item => item.Id == enrollment.Id);
        Assert.Equal(enrollment.TuitionDue, stored.TuitionDue);
    }

    [Fact]
    public async Task Create_WhenStudentIsAlreadyEnrolled_ReturnsConflictAndKeepsOneRecord()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var trainingClass = await ClassApiScenarios.CreateClassAsync(client);
        var student = await ClassApiScenarios.CreateStudentAsync(client);
        using var first = await ClassApiScenarios.EnrollAsync(client, trainingClass.Id, student.Id);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var repeated = await ClassApiScenarios.EnrollAsync(client, trainingClass.Id, student.Id);

        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        var problem = await repeated.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Học viên đã ghi danh vào lớp này.", problem?.Detail);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LangCenterDbContext>();
        Assert.Equal(1, await db.Enrollments.CountAsync(item =>
            item.TrainingClassId == trainingClass.Id && item.StudentId == student.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WhenAReferencedRecordDoesNotExist_ReturnsNotFound(bool classExists)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var classId = classExists ? (await ClassApiScenarios.CreateClassAsync(client)).Id : Guid.NewGuid();
        var studentId = classExists ? Guid.NewGuid() : (await ClassApiScenarios.CreateStudentAsync(client)).Id;

        using var response = await ClassApiScenarios.EnrollAsync(client, classId, studentId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyStudentId_ReturnsBadRequest()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await ClassApiScenarios.EnrollAsync(client, Guid.NewGuid(), Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsCashier_ReturnsForbidden()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var response = await ClassApiScenarios.EnrollAsync(client, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
