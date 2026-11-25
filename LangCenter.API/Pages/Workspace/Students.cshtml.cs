using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Common;
using LangCenter.Application.Students;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageStudents)]
public class StudentsModel(GetStudentsQueryHandler getStudents, CreateStudentCommandHandler createStudent) : PortalPageModel
{
    [BindProperty]
    public StudentInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public PagedResult<StudentDto> Students { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct) =>
        Students = await getStudents.HandleAsync(PageRequest.Of(PageNumber, PageSize), ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (await TrySaveAsync(() => createStudent.HandleAsync(new CreateStudentCommand(Input.FullName, Input.Email), ct)))
        {
            TempData["StatusMessage"] = "Đã thêm hồ sơ học viên.";
            return RedirectToPage();
        }

        await OnGetAsync(ct);
        return Page();
    }

    public class StudentInput
    {
        [Required(ErrorMessage = "Hãy nhập họ tên học viên."), StringLength(Student.MaxFullNameLength)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hãy nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(Student.MaxEmailLength)]
        public string Email { get; set; } = string.Empty;
    }
}
