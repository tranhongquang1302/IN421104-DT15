using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Students;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class PostgresStudentApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [PostgresFact]
    public async Task Create_WithConcurrentRequestsForSameEmail_StoresOneStudent()
    {
        using var registrar = factory.CreateClient();
        var login = await AuthenticatedApiFactory.LoginAsync(registrar, AuthenticatedApiFactory.RegistrarEmail);
        registrar.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        const string email = "concurrent.student@example.test";

        var first = registrar.PostAsJsonAsync("/api/students", new CreateStudentRequest("First Student", email));
        var second = registrar.PostAsJsonAsync("/api/students", new CreateStudentRequest("Second Student", email.ToUpperInvariant()));
        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;
        var responses = new[] { firstResponse, secondResponse };

        var created = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var winner = await created.Content.ReadFromJsonAsync<StudentDto>();
        var problem = await conflict.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email học viên đã được sử dụng.", problem?.Detail);

        var students = await registrar.GetFromJsonAsync<StudentDto[]>("/api/students");
        Assert.NotNull(students);
        Assert.Equal(winner, Assert.Single(students, student => student.Email == email));
    }
}
