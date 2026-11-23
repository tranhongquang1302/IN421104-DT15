using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Classes;
using LangCenter.Application.Students;
using Xunit;

namespace LangCenter.Tests.Support;

public static class ClassApiScenarios
{
    public static async Task<TrainingClassDto> CreateClassAsync(HttpClient client, int minimumEnrollment = 1,
        decimal tuitionFee = 1_000_000m, string name = "Tiếng Anh A1")
    {
        using var response = await client.PostAsJsonAsync("/api/classes",
            new CreateClassRequest(name, minimumEnrollment, tuitionFee));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var trainingClass = await response.Content.ReadFromJsonAsync<TrainingClassDto>();
        Assert.NotNull(trainingClass);
        return trainingClass;
    }

    public static async Task<StudentDto> CreateStudentAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/students",
            new CreateStudentRequest("Nguyễn Minh An", $"student.{Guid.NewGuid():N}@example.test"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        Assert.NotNull(student);
        return student;
    }

    public static Task<HttpResponseMessage> EnrollAsync(HttpClient client, Guid classId, Guid studentId) =>
        client.PostAsJsonAsync($"/api/classes/{classId}/enrollments", new EnrollStudentRequest(studentId));
}
