using System.Net;
using System.Net.Http.Json;
using LangCenter.API.Scheduling;
using LangCenter.Application.Scheduling;
using Xunit;

namespace LangCenter.Tests.Support;

public static class ScheduleApiScenarios
{
    public static async Task<RoomDto> CreateRoomAsync(HttpClient client, string code)
    {
        using var response = await client.PostAsJsonAsync("/api/schedule/rooms", new { code, name = "Phòng ngoại ngữ" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<RoomDto>(await response.Content.ReadFromJsonAsync<RoomDto>());
    }

    public static async Task<InstructorDto> CreateInstructorAsync(HttpClient client, string code)
    {
        using var response = await client.PostAsJsonAsync("/api/schedule/instructors", new { code, fullName = "Nguyễn An" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<InstructorDto>(await response.Content.ReadFromJsonAsync<InstructorDto>());
    }

    public static ClassSessionInput Input(Guid classId, Guid roomId, Guid instructorId, DateOnly date,
        int startHour = 18, int endHour = 20) => new()
        {
            ClassId = classId, RoomId = roomId, InstructorId = instructorId, Date = date,
            StartTime = new TimeOnly(startHour, 0), EndTime = new TimeOnly(endHour, 0)
        };

    public static async Task<ClassSessionDto> CreateSessionAsync(HttpClient client, ClassSessionInput input)
    {
        using var response = await client.PostAsJsonAsync("/api/schedule/sessions", input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ClassSessionDto>(await response.Content.ReadFromJsonAsync<ClassSessionDto>());
    }
}
