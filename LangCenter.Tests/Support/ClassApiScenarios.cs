using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Controllers;
using LangCenter.Application.Classes;
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
}
