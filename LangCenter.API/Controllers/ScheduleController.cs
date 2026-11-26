using System.ComponentModel.DataAnnotations;
using LangCenter.API.Scheduling;
using LangCenter.API.Security;
using LangCenter.Application.Scheduling;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/schedule")]
[Authorize(Policy = Policies.ManageClasses)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ScheduleController(GetWeekScheduleQueryHandler getWeek, GetScheduleResourcesQueryHandler getResources,
    CreateRoomCommandHandler createRoom, CreateInstructorCommandHandler createInstructor,
    SaveClassSessionCommandHandler saveSession) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<WeekScheduleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WeekScheduleDto>> Get(DateOnly? week, Guid? classId, Guid? roomId, CancellationToken ct) =>
        Ok(await getWeek.HandleAsync(new GetWeekScheduleQuery(week, classId, roomId), ct));

    [HttpGet("resources")]
    [ProducesResponseType<ScheduleResourcesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduleResourcesDto>> GetResources(CancellationToken ct) =>
        Ok(await getResources.HandleAsync(ct));

    [HttpPost("rooms")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomRequest request, CancellationToken ct)
    {
        var room = await createRoom.HandleAsync(new CreateRoomCommand(request.Code, request.Name), ct);
        return StatusCode(StatusCodes.Status201Created, room);
    }

    [HttpPost("instructors")]
    [ProducesResponseType<InstructorDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstructorDto>> CreateInstructor(CreateInstructorRequest request, CancellationToken ct)
    {
        var instructor = await createInstructor.HandleAsync(new CreateInstructorCommand(request.Code, request.FullName), ct);
        return StatusCode(StatusCodes.Status201Created, instructor);
    }

    [HttpPost("sessions")]
    [ProducesResponseType<ClassSessionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassSessionDto>> CreateSession(ClassSessionInput input, CancellationToken ct)
    {
        var session = await saveSession.HandleAsync(input.ToCommand(), ct);
        return StatusCode(StatusCodes.Status201Created, session);
    }

    [HttpPut("sessions/{id:guid}")]
    [ProducesResponseType<ClassSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassSessionDto>> UpdateSession(Guid id, ClassSessionInput input, CancellationToken ct) =>
        Ok(await saveSession.HandleAsync(input.ToCommand(id), ct));
}

public record CreateRoomRequest(
    [Required, StringLength(Room.MaxCodeLength)] string Code,
    [Required, StringLength(Room.MaxNameLength)] string Name);

public record CreateInstructorRequest(
    [Required, StringLength(Instructor.MaxCodeLength)] string Code,
    [Required, StringLength(Instructor.MaxFullNameLength)] string FullName);
