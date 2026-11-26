using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LangCenter.Application.Scheduling;
using LangCenter.Tests.Support;
using Xunit;

namespace LangCenter.Tests.Integration;

public class PostgresScheduleApiTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [PostgresFact]
    public async Task Schedule_WithConcurrentRequestsForSameRoom_SavesOnlyOne()
    {
        using var admin = factory.CreateClient();
        var login = await AuthenticatedApiFactory.LoginAsync(admin, AuthenticatedApiFactory.AdminEmail);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var firstClass = await ClassApiScenarios.CreateClassAsync(admin);
        var secondClass = await ClassApiScenarios.CreateClassAsync(admin);
        var room = await ScheduleApiScenarios.CreateRoomAsync(admin, "PG-R1");
        var firstTeacher = await ScheduleApiScenarios.CreateInstructorAsync(admin, "PG-T1");
        var secondTeacher = await ScheduleApiScenarios.CreateInstructorAsync(admin, "PG-T2");
        var date = new DateOnly(2026, 12, 1);

        var first = admin.PostAsJsonAsync("/api/schedule/sessions",
            ScheduleApiScenarios.Input(firstClass.Id, room.Id, firstTeacher.Id, date));
        var second = admin.PostAsJsonAsync("/api/schedule/sessions",
            ScheduleApiScenarios.Input(secondClass.Id, room.Id, secondTeacher.Id, date));
        await Task.WhenAll(first, second);
        using var firstResponse = await first;
        using var secondResponse = await second;

        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
        var week = await admin.GetFromJsonAsync<WeekScheduleDto>($"/api/schedule?week={date:yyyy-MM-dd}&roomId={room.Id}");
        Assert.NotNull(week);
        Assert.Single(week.Sessions);
    }
}
