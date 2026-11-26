using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Scheduling;
using Entities = LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ManageClasses)]
public class ScheduleResourcesModel(GetScheduleResourcesQueryHandler getResources, CreateRoomCommandHandler createRoom,
    CreateInstructorCommandHandler createInstructor) : PortalPageModel
{
    public RoomInput Room { get; private set; } = new();
    public InstructorInput Instructor { get; private set; } = new();
    public ScheduleResourcesDto Resources { get; private set; } = new([], []);
    public bool RoomRejected { get; private set; }
    public bool InstructorRejected { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => Resources = await getResources.HandleAsync(ct);

    public async Task<IActionResult> OnPostRoomAsync(RoomInput room, CancellationToken ct)
    {
        Room = room;
        if (!await TrySaveAsync(() => createRoom.HandleAsync(new CreateRoomCommand(room.Code, room.Name), ct)))
        {
            RoomRejected = true;
            await OnGetAsync(ct);
            return Page();
        }

        TempData["StatusMessage"] = "Đã thêm phòng học.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostInstructorAsync(InstructorInput instructor, CancellationToken ct)
    {
        Instructor = instructor;
        if (!await TrySaveAsync(() =>
                createInstructor.HandleAsync(new CreateInstructorCommand(instructor.Code, instructor.FullName), ct)))
        {
            InstructorRejected = true;
            await OnGetAsync(ct);
            return Page();
        }

        TempData["StatusMessage"] = "Đã thêm giảng viên.";
        return RedirectToPage();
    }

    public class RoomInput
    {
        [Required, StringLength(Entities.Room.MaxCodeLength)]
        public string Code { get; set; } = string.Empty;
        [Required, StringLength(Entities.Room.MaxNameLength)]
        public string Name { get; set; } = string.Empty;
    }

    public class InstructorInput
    {
        [Required, StringLength(Entities.Instructor.MaxCodeLength)]
        public string Code { get; set; } = string.Empty;
        [Required, StringLength(Entities.Instructor.MaxFullNameLength)]
        public string FullName { get; set; } = string.Empty;
    }
}
