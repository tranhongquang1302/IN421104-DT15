using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Reports;
using LangCenter.Application.Tuition;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageTuition)]
public class TuitionModel(IEnrollmentLookupQuery enrollmentLookup, GetClassesQueryHandler getClasses,
    IClassTuitionReportQuery reports) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [EmailAddress(ErrorMessage = "Email học viên không hợp lệ."), StringLength(Student.MaxEmailLength)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ClassId { get; set; }

    public IReadOnlyList<TrainingClassDto> Classes { get; private set; } = [];
    public IReadOnlyList<EnrollmentLookupDto> Matches { get; private set; } = [];
    public ClassTuitionReportDto? Report { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Classes = await getClasses.HandleAsync(ct);
        if (Email is not null && ModelState.IsValid)
            Matches = await enrollmentLookup.FindByStudentEmailAsync(Email, ct);

        if (ClassId is Guid classId)
        {
            Report = await reports.GetByClassIdAsync(classId, ct);
            if (Report is null)
                ModelState.AddModelError(nameof(ClassId), "Không tìm thấy lớp học.");
        }
    }
}
