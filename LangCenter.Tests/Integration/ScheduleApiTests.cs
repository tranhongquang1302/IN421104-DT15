using System.Net;
using System.Net.Http.Json;
using LangCenter.Application.Scheduling;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ScheduleApiTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task Schedule_OverlappingResources_ReturnsConflictAndAllowsAdjacentSessions()
    {
        using var registrar = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        var firstClass = await ClassApiScenarios.CreateClassAsync(registrar);
        var secondClass = await ClassApiScenarios.CreateClassAsync(registrar);
        var firstRoom = await ScheduleApiScenarios.CreateRoomAsync(registrar, "SC-101");
        var secondRoom = await ScheduleApiScenarios.CreateRoomAsync(registrar, "SC-102");
        var firstTeacher = await ScheduleApiScenarios.CreateInstructorAsync(registrar, "SC-T1");
        var secondTeacher = await ScheduleApiScenarios.CreateInstructorAsync(registrar, "SC-T2");
        var date = new DateOnly(2026, 11, 24);
        var first = ScheduleApiScenarios.Input(firstClass.Id, firstRoom.Id, firstTeacher.Id, date);
        await ScheduleApiScenarios.CreateSessionAsync(registrar, first);

        var conflicts = new[]
        {
            (Input: ScheduleApiScenarios.Input(secondClass.Id, firstRoom.Id, secondTeacher.Id, date, 19, 21), Reason: $"Phòng {firstRoom.Code}"),
            (Input: ScheduleApiScenarios.Input(secondClass.Id, secondRoom.Id, firstTeacher.Id, date), Reason: $"Giảng viên {firstTeacher.FullName}"),
            (Input: ScheduleApiScenarios.Input(firstClass.Id, secondRoom.Id, secondTeacher.Id, date), Reason: $"Lớp {firstClass.Name}")
        };
        foreach (var conflict in conflicts)
        {
            using var rejected = await registrar.PostAsJsonAsync("/api/schedule/sessions", conflict.Input);
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(await rejected.Content.ReadFromJsonAsync<ProblemDetails>());
            Assert.StartsWith(conflict.Reason, problem.Detail);
        }

        var adjacent = ScheduleApiScenarios.Input(secondClass.Id, firstRoom.Id, firstTeacher.Id, date, 20, 21);
        var saved = await ScheduleApiScenarios.CreateSessionAsync(registrar, adjacent);
        Assert.Equal(new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc), saved.StartsAt);
        var week = await registrar.GetFromJsonAsync<WeekScheduleDto>($"/api/schedule?week=2026-11-26&roomId={firstRoom.Id}");
        Assert.NotNull(week);
        Assert.Equal(new DateOnly(2026, 11, 23), week.WeekStart);
        Assert.Equal(2, week.Sessions.Count);
        Assert.Equal(saved.Id, week.Sessions[1].Id);
    }

    [Fact]
    public async Task Reschedule_ConflictingChange_PreservesOriginalAndAcceptsAvailableSlot()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        var firstClass = await ClassApiScenarios.CreateClassAsync(admin);
        var secondClass = await ClassApiScenarios.CreateClassAsync(admin);
        var room = await ScheduleApiScenarios.CreateRoomAsync(admin, "EDIT-R");
        var instructor = await ScheduleApiScenarios.CreateInstructorAsync(admin, "EDIT-T");
        var date = new DateOnly(2026, 12, 1);
        await ScheduleApiScenarios.CreateSessionAsync(admin, ScheduleApiScenarios.Input(firstClass.Id, room.Id, instructor.Id, date));
        var input = ScheduleApiScenarios.Input(secondClass.Id, room.Id, instructor.Id, date.AddDays(1));
        var session = await ScheduleApiScenarios.CreateSessionAsync(admin, input);

        input.Date = date;
        using var conflict = await admin.PutAsJsonAsync($"/api/schedule/sessions/{session.Id}", input);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var originalWeek = await admin.GetFromJsonAsync<WeekScheduleDto>($"/api/schedule?week={date:yyyy-MM-dd}&classId={secondClass.Id}");
        Assert.NotNull(originalWeek);
        Assert.Equal(session.StartsAt, Assert.Single(originalWeek.Sessions).StartsAt);

        input.StartTime = new TimeOnly(20, 0);
        input.EndTime = new TimeOnly(21, 0);
        using var updated = await admin.PutAsJsonAsync($"/api/schedule/sessions/{session.Id}", input);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var saved = Assert.IsType<ClassSessionDto>(await updated.Content.ReadFromJsonAsync<ClassSessionDto>());
        Assert.Equal(session.Id, saved.Id);
        Assert.Equal(new DateTime(2026, 12, 1, 13, 0, 0, DateTimeKind.Utc), saved.StartsAt);

        using var repeated = await admin.PutAsJsonAsync($"/api/schedule/sessions/{session.Id}", input);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
    }

    [Fact]
    public async Task ManageSchedule_WithRestrictedRoleInvalidTimeOrDuplicateCode_ReturnsExpectedErrors()
    {
        using var cashier = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.CashierEmail);
        using var admin = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.AdminEmail);
        using var forbidden = await cashier.GetAsync("/api/schedule");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var room = await ScheduleApiScenarios.CreateRoomAsync(admin, "  sd-101  ");
        Assert.Equal("SD-101", room.Code);
        using var duplicate = await admin.PostAsJsonAsync("/api/schedule/rooms", new { code = "sd-101", name = "Phòng khác" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var input = ScheduleApiScenarios.Input(Guid.NewGuid(), room.Id, Guid.NewGuid(), new DateOnly(2026, 12, 1), 19, 18);
        using var invalid = await admin.PostAsJsonAsync("/api/schedule/sessions", input);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        input.EndTime = new TimeOnly(20, 0);
        using var missingClass = await admin.PostAsJsonAsync("/api/schedule/sessions", input);
        Assert.Equal(HttpStatusCode.NotFound, missingClass.StatusCode);
    }
}
