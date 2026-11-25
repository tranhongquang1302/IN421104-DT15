using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
using LangCenter.Application.Enrollments;
using LangCenter.Application.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageClasses)]
public class ClassDetailsModel(GetClassByIdQueryHandler getClass, GetStudentsQueryHandler getStudents,
    IEnrollmentWorkspaceQuery enrollments, EnrollStudentCommandHandler enroll, LaunchClassCommandHandler launch) : PortalPageModel
{
    [BindProperty]
    public Guid? StudentId { get; set; }

    public TrainingClassDto Class { get; private set; } = null!;
    public IReadOnlyList<ClassEnrollmentRow> Roster { get; private set; } = [];
    public IReadOnlyList<StudentDto> AvailableStudents { get; private set; } = [];
    public bool LaunchRejected { get; private set; }

    public Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) => ShowAsync(id, ct);

    public async Task<IActionResult> OnPostEnrollAsync(Guid id, CancellationToken ct)
    {
        if (StudentId is null)
            ModelState.AddModelError(nameof(StudentId), "Hãy chọn học viên.");

        if (!await TrySaveAsync(() => enroll.HandleAsync(new EnrollStudentCommand(id, StudentId!.Value), ct)))
            return await ShowAsync(id, ct);

        TempData["StatusMessage"] = "Đã ghi danh học viên vào lớp.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostLaunchAsync(Guid id, CancellationToken ct)
    {
        TrainingClassDto? launched = null;
        if (!await TrySaveAsync(async () => launched = await launch.HandleAsync(new LaunchClassCommand(id), ct)))
        {
            LaunchRejected = true;
            return await ShowAsync(id, ct);
        }

        TempData["StatusMessage"] = $"Đã khai giảng lớp {launched!.Name}.";
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> ShowAsync(Guid id, CancellationToken ct)
    {
        try
        {
            Class = await getClass.HandleAsync(new GetClassByIdQuery(id), ct);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        Roster = await enrollments.GetRosterAsync(id, ct);
        if (Class.Status == "Open")
        {
            var existing = Roster.Select(row => row.StudentId).ToHashSet();
            AvailableStudents = (await getStudents.HandleAsync(ct)).Where(student => !existing.Contains(student.Id)).ToArray();
        }

        return Page();
    }
}
