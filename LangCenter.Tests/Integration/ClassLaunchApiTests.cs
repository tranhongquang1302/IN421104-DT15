using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Classes;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ClassLaunchApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Launch_BelowMinimumEnrollment_ReturnsProblemDetailsAndLeavesClassOpen()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await CreateClassWithEnrollmentsAsync(client, minimumEnrollment: 2, enrollmentCount: 1);

        using var response = await LaunchAsync(client, trainingClass.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem?.Detail);
        Assert.Contains("1/2", problem.Detail);

        var stored = await client.GetFromJsonAsync<TrainingClassDto>($"/api/classes/{trainingClass.Id}");
        Assert.NotNull(stored);
        Assert.Equal("Open", stored.Status);
        Assert.Null(stored.StartedAt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Launch_AtOrAboveMinimumEnrollment_SavesStartedClass(int enrollmentCount)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await CreateClassWithEnrollmentsAsync(client, minimumEnrollment: 1, enrollmentCount);

        using var response = await LaunchAsync(client, trainingClass.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var launched = await response.Content.ReadFromJsonAsync<TrainingClassDto>();
        Assert.NotNull(launched);
        Assert.Equal("Started", launched.Status);
        Assert.NotNull(launched.StartedAt);
        Assert.Equal(DateTimeKind.Utc, launched.StartedAt.Value.Kind);

        var stored = await client.GetFromJsonAsync<TrainingClassDto>($"/api/classes/{trainingClass.Id}");
        Assert.Equal(launched, stored);
    }

    [Fact]
    public async Task Launch_WhenClassHasAlreadyStarted_ReturnsBadRequest()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var trainingClass = await CreateClassWithEnrollmentsAsync(client, minimumEnrollment: 1, enrollmentCount: 1);
        using var first = await LaunchAsync(client, trainingClass.Id);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var repeated = await LaunchAsync(client, trainingClass.Id);

        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        var problem = await repeated.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Lớp đã khai giảng.", problem?.Detail);
    }

    [Fact]
    public async Task Enroll_AfterClassHasStarted_ReturnsBadRequest()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await CreateClassWithEnrollmentsAsync(client, minimumEnrollment: 1, enrollmentCount: 1);
        using var launched = await LaunchAsync(client, trainingClass.Id);
        Assert.Equal(HttpStatusCode.OK, launched.StatusCode);
        var newStudent = await ClassApiScenarios.CreateStudentAsync(client);

        using var response = await ClassApiScenarios.EnrollAsync(client, trainingClass.Id, newStudent.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Lớp đã khai giảng và đã dừng nhận ghi danh.", problem?.Detail);
    }

    [Fact]
    public async Task Launch_WhenClassDoesNotExist_ReturnsNotFound()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await LaunchAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Launch_AsCashier_ReturnsForbidden()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var response = await LaunchAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> LaunchAsync(HttpClient client, Guid classId) =>
        client.PostAsync($"/api/classes/{classId}/launch", content: null);

    private static async Task<TrainingClassDto> CreateClassWithEnrollmentsAsync(
        HttpClient client, int minimumEnrollment, int enrollmentCount)
    {
        var trainingClass = await ClassApiScenarios.CreateClassAsync(client, minimumEnrollment);
        for (var index = 0; index < enrollmentCount; index++)
        {
            var student = await ClassApiScenarios.CreateStudentAsync(client);
            using var response = await ClassApiScenarios.EnrollAsync(client, trainingClass.Id, student.Id);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        return trainingClass;
    }
}
