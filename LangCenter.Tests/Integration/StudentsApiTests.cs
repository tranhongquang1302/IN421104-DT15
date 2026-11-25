using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Common;
using LangCenter.Application.Students;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class StudentsApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Create_AsRegistrarWithValidStudent_ReturnsCreatedAndPersistsStudent()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("  Nguyễn Mai Anh  ", "Mai.Anh@example.test"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        Assert.NotNull(student);
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal("Nguyễn Mai Anh", student.FullName);
        Assert.Equal("mai.anh@example.test", student.Email);
        Assert.Equal($"/api/students/{student.Id}", response.Headers.Location?.AbsolutePath);

        var stored = await client.GetFromJsonAsync<StudentDto>(response.Headers.Location);
        Assert.Equal(student, stored);
    }

    [Fact]
    public async Task Create_WithAnEmailAlreadyInUse_ReturnsConflictAndKeepsOriginalStudent()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        const string email = "same.student@example.test";
        using var original = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("Nguyễn Mai Anh", $"  {email.ToUpperInvariant()}  "));
        Assert.Equal(HttpStatusCode.Created, original.StatusCode);
        var expected = await original.Content.ReadFromJsonAsync<StudentDto>();

        using var duplicate = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("Hồ Minh Tâm", email));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email học viên đã được sử dụng.", problem?.Detail);
        var students = await client.GetFromJsonAsync<StudentDto[]>("/api/students");
        Assert.NotNull(students);
        Assert.Equal(expected, Assert.Single(students, student => student.Email == email));
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsSavedStudents()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        using var created = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("Lê Minh An", "minh.an@example.test"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var expected = await created.Content.ReadFromJsonAsync<StudentDto>();
        Assert.NotNull(expected);

        var students = await client.GetFromJsonAsync<StudentDto[]>("/api/students");

        Assert.NotNull(students);
        Assert.Contains(expected, students);
    }

    [Fact]
    public async Task GetAll_WithOutOfRangePaging_NormalizesLimitsAndKeepsTotal()
    {
        // Arrange
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        await ClassApiScenarios.CreateStudentAsync(client);
        var students = await client.GetFromJsonAsync<StudentDto[]>("/api/students");
        Assert.NotNull(students);

        // Act
        var first = await client.GetFromJsonAsync<PagedResult<StudentDto>>("/api/students?page=0&pageSize=0");
        var beyond = await client.GetFromJsonAsync<PagedResult<StudentDto>>(
            $"/api/students?page={int.MaxValue}&pageSize=500");

        // Assert
        Assert.NotNull(first);
        Assert.Equal(1, first.Page);
        Assert.Equal(1, first.PageSize);
        Assert.Equal(students[0], Assert.Single(first.Items));
        Assert.NotNull(beyond);
        Assert.Empty(beyond.Items);
        Assert.Equal(PageRequest.MaxPageSize, beyond.PageSize);
        Assert.Equal(students.Length, beyond.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/students");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsCashier_ReturnsForbidden()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var response = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("Hồ Minh Tâm", "minh.tam@example.test"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(" ", "student@example.test", "FullName")]
    [InlineData("Trần An", "invalid-address", "Email")]
    public async Task Create_WithInvalidStudent_ReturnsValidationErrors(string fullName, string email, string field)
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.PostAsJsonAsync("/api/students", new CreateStudentRequest(fullName, email));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, key => string.Equals(key, field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetById_WhenStudentDoesNotExist_ReturnsNotFound()
    {
        using var client = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        using var response = await client.GetAsync($"/api/students/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
