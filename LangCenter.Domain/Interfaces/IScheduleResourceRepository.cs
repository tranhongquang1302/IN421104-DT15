using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IScheduleResourceRepository
{
    Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct = default);
    Task<Room?> GetRoomForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<Instructor?> GetInstructorForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<bool> TryAddAsync(Room room, CancellationToken ct = default);
    Task<bool> TryAddAsync(Instructor instructor, CancellationToken ct = default);
}
