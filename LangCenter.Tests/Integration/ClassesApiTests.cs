using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ClassesApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Create_AsRegistrarWithValidClass_ReturnsLocationAndSavedClass()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/classes",
            new CreateClassRequest("  Tiếng Anh A2  ", 4, 1_000_000m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var trainingClass = await response.Content.ReadFromJsonAsync<TrainingClassDto>();
        Assert.NotNull(trainingClass);
        Assert.Equal("Tiếng Anh A2", trainingClass.Name);
        Assert.Equal(4, trainingClass.MinimumEnrollment);
        Assert.Equal(1_000_000m, trainingClass.TuitionFee);
        Assert.Equal("Open", trainingClass.Status);
        Assert.Null(trainingClass.StartedAt);
        Assert.Equal($"/api/classes/{trainingClass.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(trainingClass, await client.GetFromJsonAsync<TrainingClassDto>(response.Headers.Location));
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsClassesOrderedByName()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var second = await ClassApiScenarios.CreateClassAsync(client, name: "B2");
        var first = await ClassApiScenarios.CreateClassAsync(client, tuitionFee: 0, name: "A1");

        var classes = await client.GetFromJsonAsync<TrainingClassDto[]>("/api/classes");

        Assert.NotNull(classes);
        Assert.Equal(new[] { first.Id, second.Id },
            classes.Where(trainingClass => trainingClass.Id == first.Id || trainingClass.Id == second.Id)
                .Select(trainingClass => trainingClass.Id));
    }

    [Fact]
    public async Task GetAll_WithPaging_ReturnsOrderedPageAndTotal()
    {
        // Arrange
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        await ClassApiScenarios.CreateClassAsync(client, name: "A1");
        await ClassApiScenarios.CreateClassAsync(client, name: "A1");
        var classes = await client.GetFromJsonAsync<TrainingClassDto[]>("/api/classes");
        Assert.NotNull(classes);

        // Act
        var page = await client.GetFromJsonAsync<PagedResult<TrainingClassDto>>("/api/classes?page=2&pageSize=1");

        // Assert
        Assert.NotNull(page);
        Assert.Equal(classes[1], Assert.Single(page.Items));
        Assert.Equal(classes.Length, page.TotalCount);
        Assert.Equal(classes.Length, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
    }

    [Fact]
    public async Task GetAll_AsCashier_ReturnsForbidden()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var response = await client.GetAsync("/api/classes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenClassDoesNotExist_ReturnsNotFound()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.GetAsync($"/api/classes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, -1)]
    public async Task Create_WithInvalidClassSettings_ReturnsBadRequest(int minimumEnrollment, int tuitionFee)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/classes",
            new CreateClassRequest("Tiếng Pháp A1", minimumEnrollment, tuitionFee));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithFractionalFee_ReturnsProblemDetails()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/classes",
            new CreateClassRequest("Tiếng Pháp A1", 1, 1_000.50m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem?.Detail);
        Assert.Contains("số nguyên đồng", problem.Detail);
    }

    [Fact]
    public async Task Create_WithoutTuitionFee_ReturnsBadRequest()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/classes", new { name = "Tiếng Pháp A1", minimumEnrollment = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
