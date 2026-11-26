using LangCenter.API.Scheduling;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
using LangCenter.Application.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageClasses)]
public class ScheduleModel(GetWeekScheduleQueryHandler getWeek, GetScheduleResourcesQueryHandler getResources,
    GetClassesQueryHandler getClasses, SaveClassSessionCommandHandler saveSession, TimeProvider clock) : PortalPageModel
{
    private static readonly string[] DayNames = ["Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy", "Chủ nhật"];

    [BindProperty(SupportsGet = true)]
    public DateOnly? Week { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ClassId { get; set; }

    [BindProperty]
    public ClassSessionInput Input { get; set; } = new();

    [BindProperty]
    public Guid? EditingSessionId { get; set; }

    public IReadOnlyList<TrainingClassDto> Classes { get; private set; } = [];
    public ScheduleResourcesDto Resources { get; private set; } = new([], []);
    public WeekScheduleDto Calendar { get; private set; } = null!;
    public IReadOnlyList<ScheduleDay> Days { get; private set; } = [];
    public bool ShowEditor { get; private set; }
    public bool CanSchedule => Classes.Count > 0 && Resources.Rooms.Count > 0 && Resources.Instructors.Count > 0;
    public DateOnly Today => DateOnly.FromDateTime(ScheduleTime.ToLocal(clock.GetUtcNow().UtcDateTime));

    public async Task<IActionResult> OnGetAsync(bool edit, Guid? sessionId, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (sessionId is Guid id)
        {
            var session = Calendar.Sessions.SingleOrDefault(row => row.Id == id);
            if (session is null)
                return NotFound();

            var start = ScheduleTime.ToLocal(session.StartsAt);
            var end = ScheduleTime.ToLocal(session.EndsAt);
            EditingSessionId = id;
            Input = new ClassSessionInput
            {
                ClassId = session.ClassId, RoomId = session.RoomId, InstructorId = session.InstructorId,
                Date = DateOnly.FromDateTime(start), StartTime = TimeOnly.FromDateTime(start), EndTime = TimeOnly.FromDateTime(end)
            };
        }
        else if (edit)
        {
            var weekStart = Calendar.WeekStart;
            Input = new ClassSessionInput
            {
                ClassId = ClassId, RoomId = RoomId,
                Date = Today >= weekStart && Today < weekStart.AddDays(7) ? Today : weekStart
            };
        }

        ShowEditor = edit || EditingSessionId.HasValue;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!await TrySaveAsync(() => saveSession.HandleAsync(Input.ToCommand(EditingSessionId), ct)))
        {
            ShowEditor = true;
            await LoadAsync(ct);
            return Page();
        }

        TempData["StatusMessage"] = EditingSessionId.HasValue ? "Đã đổi lịch buổi học." : "Đã xếp buổi học.";
        return RedirectToPage(new
        {
            week = ScheduleTime.StartOfWeek(Input.Date!.Value).ToString("yyyy-MM-dd"),
            classId = ClassId.HasValue ? Input.ClassId : null,
            roomId = RoomId.HasValue ? Input.RoomId : null
        });
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Classes = await getClasses.HandleAsync(ct);
        Resources = await getResources.HandleAsync(ct);
        try
        {
            Calendar = await getWeek.HandleAsync(new GetWeekScheduleQuery(Week, ClassId, RoomId), ct);
        }
        catch (BusinessRuleException error)
        {
            ModelState.AddModelError(nameof(Week), error.Message);
            Calendar = await getWeek.HandleAsync(new GetWeekScheduleQuery(null, ClassId, RoomId), ct);
        }

        var byDay = Calendar.Sessions.ToLookup(session => DateOnly.FromDateTime(ScheduleTime.ToLocal(session.StartsAt)));
        Days = DayNames.Select((name, index) =>
        {
            var date = Calendar.WeekStart.AddDays(index);
            return new ScheduleDay(date, name, byDay[date].ToArray());
        }).ToArray();
    }

    public record ScheduleDay(DateOnly Date, string Name, IReadOnlyList<ClassSessionDto> Sessions);
}
