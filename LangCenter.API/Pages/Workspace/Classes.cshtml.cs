using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageClasses)]
public class ClassesModel(GetClassesQueryHandler getClasses, CreateClassCommandHandler createClass) : PortalPageModel
{
    [BindProperty]
    public ClassInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public PagedResult<TrainingClassDto> Classes { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken ct) =>
        Classes = await getClasses.HandleAsync(PageRequest.Of(PageNumber, PageSize), ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        TrainingClassDto? created = null;
        if (await TrySaveAsync(async () => created = await createClass.HandleAsync(
                new CreateClassCommand(Input.Name, Input.MinimumEnrollment, Input.TuitionFee), ct)))
        {
            TempData["StatusMessage"] = "Đã tạo lớp học.";
            return RedirectToPage("/Workspace/ClassDetails", new { id = created!.Id });
        }

        await OnGetAsync(ct);
        return Page();
    }

    public class ClassInput
    {
        [Required(ErrorMessage = "Hãy nhập tên lớp."), StringLength(TrainingClass.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Sĩ số tối thiểu phải lớn hơn 0.")]
        public int MinimumEnrollment { get; set; } = 1;

        [Range(typeof(decimal), "0", "99999999999999", ErrorMessage = "Học phí vượt khoảng cho phép.")]
        public decimal TuitionFee { get; set; }
    }
}
