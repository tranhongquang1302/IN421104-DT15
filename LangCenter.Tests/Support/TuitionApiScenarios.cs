using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Classes;
using LangCenter.Application.Enrollments;
using LangCenter.Application.Students;
using LangCenter.Application.Tuition;
using Xunit;

namespace LangCenter.Tests.Support;

public record TuitionScenario(TrainingClassDto TrainingClass, StudentDto Student, EnrollmentDto Enrollment);

public static class TuitionApiScenarios
{
    public static async Task<TuitionScenario> CreateEnrollmentAsync(HttpClient client, decimal tuitionFee = 1_000m)
    {
        var trainingClass = await ClassApiScenarios.CreateClassAsync(client, tuitionFee: tuitionFee);
        var student = await ClassApiScenarios.CreateStudentAsync(client);
        return new TuitionScenario(trainingClass, student, await EnrollAsync(client, trainingClass.Id, student.Id));
    }

    public static async Task<EnrollmentDto> EnrollAsync(HttpClient client, Guid classId, Guid studentId)
    {
        using var response = await ClassApiScenarios.EnrollAsync(client, classId, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollmentDto>();
        Assert.NotNull(enrollment);
        return enrollment;
    }

    public static Task<HttpResponseMessage> PayAsync(HttpClient client, Guid enrollmentId, decimal amount,
        Guid? requestId = null) =>
        client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/payments", new RecordPaymentRequest(amount, requestId));

    public static async Task<TuitionBalanceDto> GetBalanceAsync(HttpClient client, Guid enrollmentId)
    {
        var balance = await client.GetFromJsonAsync<TuitionBalanceDto>($"/api/enrollments/{enrollmentId}/balance");
        Assert.NotNull(balance);
        return balance;
    }
}
