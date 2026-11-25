using System.ComponentModel.DataAnnotations;
using LangCenter.API.Reports;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
using LangCenter.Application.Reports;
using LangCenter.Application.Tuition;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageTuition)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TuitionModel(IEnrollmentLookupQuery enrollmentLookup, GetClassesQueryHandler getClasses,
    IClassTuitionReportQuery reports, CsvClassTuitionReportStrategy csv) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [EmailAddress(ErrorMessage = "Email học viên không hợp lệ."), StringLength(Student.MaxEmailLength)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public IReadOnlyList<TrainingClassDto> Classes { get; private set; } = [];
    public IReadOnlyList<EnrollmentLookupDto> Matches { get; private set; } = [];
    public PagedClassTuitionReportDto? Report { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Classes = await getClasses.HandleAsync(ct);
        if (Email is not null && ModelState.IsValid)
            Matches = await enrollmentLookup.FindByStudentEmailAsync(Email, ct);

        if (ClassId is Guid classId)
        {
            Report = await reports.GetPageAsync(classId, PageRequest.Of(PageNumber, PageSize), ct);
            if (Report is null)
                ModelState.AddModelError(nameof(ClassId), "Không tìm thấy lớp học.");
        }
    }

    public async Task<IActionResult> OnGetDownloadAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid || ClassId is not Guid classId)
            return BadRequest();

        var report = await reports.GetByClassIdAsync(classId, ct);
        return report is null ? NotFound() : csv.Render(report);
    }
}
