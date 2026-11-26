using System.Net;
using System.Text.RegularExpressions;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LangCenter.Tests.Integration;

public class SchedulePortalTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task ManageSchedule_ThroughClassDetailsAndCalendar_PersistsChangesAndShowsConflict()
    {
        using var registrar = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var api = await factory.CreateAuthenticatedClientAsync(AuthenticatedApiFactory.RegistrarEmail);
        await PortalForms.LoginAsync(registrar, AuthenticatedApiFactory.RegistrarEmail);
        var trainingClass = await ClassApiScenarios.CreateClassAsync(api, name: "Tiếng Anh A1 lịch tuần");
        var otherClass = await ClassApiScenarios.CreateClassAsync(api, name: "Tiếng Anh A2 lịch tuần");
        var catalog = await PortalForms.GetPageAsync(registrar, "/workspace/schedule/resources");
        using var room = await PortalForms.PostAsync(registrar, "/workspace/schedule/resources?handler=Room", catalog,
            new Dictionary<string, string> { ["Room.Code"] = "WEB-R", ["Room.Name"] = "Phòng lịch tuần" });
        Assert.Equal(HttpStatusCode.Redirect, room.StatusCode);
        using var instructor = await PortalForms.PostAsync(registrar, "/workspace/schedule/resources?handler=Instructor", catalog,
            new Dictionary<string, string> { ["Instructor.Code"] = "WEB-T", ["Instructor.FullName"] = "Giảng viên kiểm thử" });
        Assert.Equal(HttpStatusCode.Redirect, instructor.StatusCode);
        catalog = await PortalForms.GetPageAsync(registrar, "/workspace/schedule/resources");
        Assert.Contains("Phòng lịch tuần", catalog);
        Assert.Contains("Giảng viên kiểm thử", catalog);

        var details = await PortalForms.GetPageAsync(registrar, $"/workspace/classes/{trainingClass.Id}");
        Assert.Contains($"/workspace/schedule?classId={trainingClass.Id}&edit=true#schedule-editor", details);
        const string page = "/workspace/schedule?week=2026-11-23";
        var editor = await PortalForms.GetPageAsync(registrar, $"{page}&classId={trainingClass.Id}&edit=true");
        var roomId = Regex.Match(editor, "value=\"([^\"]+)\">WEB-R · Phòng lịch tuần").Groups[1].Value;
        var instructorId = Regex.Match(editor, "value=\"([^\"]+)\">Giảng viên kiểm thử · WEB-T").Groups[1].Value;
        Assert.True(Guid.TryParse(roomId, out _));
        Assert.True(Guid.TryParse(instructorId, out _));
        var fields = new Dictionary<string, string>
        {
            ["Input.ClassId"] = trainingClass.Id.ToString(),
            ["Input.RoomId"] = roomId,
            ["Input.InstructorId"] = instructorId,
            ["Input.Date"] = "2026-11-23",
            ["Input.StartTime"] = "18:00",
            ["Input.EndTime"] = "19:30"
        };

        using var first = await PortalForms.PostAsync(registrar, page, editor, fields);
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        var schedule = await PortalForms.GetPageAsync(registrar, page);
        Assert.Contains("18:00–19:30", schedule);
        Assert.Contains("WEB-R · Giảng viên kiểm thử", schedule);

        fields["Input.ClassId"] = otherClass.Id.ToString();
        using var conflicting = await PortalForms.PostAsync(registrar, page, editor, fields);
        Assert.Equal(HttpStatusCode.OK, conflicting.StatusCode);
        var rejected = await PortalForms.ReadHtmlAsync(conflicting);
        Assert.Contains("Phòng WEB-R đã có buổi Tiếng Anh A1 lịch tuần", rejected);
        Assert.Contains("value=\"2026-11-23\"", rejected);

        var sessionId = Regex.Match(schedule, "sessionId=([a-f0-9-]+)#schedule-editor").Groups[1].Value;
        Assert.True(Guid.TryParse(sessionId, out _));
        var edit = await PortalForms.GetPageAsync(registrar, $"{page}&sessionId={sessionId}");
        Assert.Contains("Đổi lịch buổi học", edit);
        Assert.Contains("value=\"18:00\"", edit);
        fields["EditingSessionId"] = sessionId;
        fields["Input.ClassId"] = trainingClass.Id.ToString();
        fields["Input.StartTime"] = "19:30";
        fields["Input.EndTime"] = "21:00";
        using var updated = await PortalForms.PostAsync(registrar, page, edit, fields);
        Assert.Equal(HttpStatusCode.Redirect, updated.StatusCode);
        schedule = await PortalForms.GetPageAsync(registrar, page);
        Assert.Contains("19:30–21:00", schedule);
        Assert.DoesNotContain("18:00–19:30", schedule);
    }
}
