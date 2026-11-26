using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Scheduling;

public record RoomDto(Guid Id, string Code, string Name)
{
    public static RoomDto FromRoom(Room room) => new(room.Id, room.Code, room.Name);
}

public record InstructorDto(Guid Id, string Code, string FullName)
{
    public static InstructorDto FromInstructor(Instructor instructor) =>
        new(instructor.Id, instructor.Code, instructor.FullName);
}

public record ScheduleResourcesDto(IReadOnlyList<RoomDto> Rooms, IReadOnlyList<InstructorDto> Instructors);

public class GetScheduleResourcesQueryHandler(IScheduleResourceRepository resources)
{
    public async Task<ScheduleResourcesDto> HandleAsync(CancellationToken ct = default)
    {
        var rooms = await resources.GetRoomsAsync(ct);
        var instructors = await resources.GetInstructorsAsync(ct);
        return new ScheduleResourcesDto(rooms.Select(RoomDto.FromRoom).ToArray(),
            instructors.Select(InstructorDto.FromInstructor).ToArray());
    }
}

public record CreateRoomCommand(string Code, string Name);

public class CreateRoomCommandHandler(IScheduleResourceRepository resources)
{
    public async Task<RoomDto> HandleAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        Room room;
        try
        {
            room = Room.Create(command.Code, command.Name);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        if (!await resources.TryAddAsync(room, ct))
            throw new ConflictException("Mã phòng đã được sử dụng.");
        return RoomDto.FromRoom(room);
    }
}

public record CreateInstructorCommand(string Code, string FullName);

public class CreateInstructorCommandHandler(IScheduleResourceRepository resources)
{
    public async Task<InstructorDto> HandleAsync(CreateInstructorCommand command, CancellationToken ct = default)
    {
        Instructor instructor;
        try
        {
            instructor = Instructor.Create(command.Code, command.FullName);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        if (!await resources.TryAddAsync(instructor, ct))
            throw new ConflictException("Mã giảng viên đã được sử dụng.");
        return InstructorDto.FromInstructor(instructor);
    }
}
