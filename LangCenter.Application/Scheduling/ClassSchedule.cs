namespace LangCenter.Application.Scheduling;

public record ClassSessionDto(Guid Id, Guid ClassId, string ClassName, Guid RoomId, string RoomCode, string RoomName,
    Guid InstructorId, string InstructorCode, string InstructorName, DateTime StartsAt, DateTime EndsAt);

public record WeekScheduleDto(DateOnly WeekStart, IReadOnlyList<ClassSessionDto> Sessions);

public interface IClassScheduleQuery
{
    Task<IReadOnlyList<ClassSessionDto>> GetWeekAsync(DateTime startsAt, DateTime endsAt,
        Guid? classId, Guid? roomId, CancellationToken ct = default);
}

public record GetWeekScheduleQuery(DateOnly? Week, Guid? ClassId = null, Guid? RoomId = null);

public class GetWeekScheduleQueryHandler(IClassScheduleQuery schedule, TimeProvider clock)
{
    public async Task<WeekScheduleDto> HandleAsync(GetWeekScheduleQuery query, CancellationToken ct = default)
    {
        var localToday = DateOnly.FromDateTime(ScheduleTime.ToLocal(clock.GetUtcNow().UtcDateTime));
        var weekStart = ScheduleTime.StartOfWeek(query.Week ?? localToday);
        var startsAt = ScheduleTime.ToUtc(weekStart, TimeOnly.MinValue);
        var endsAt = ScheduleTime.ToUtc(weekStart.AddDays(7), TimeOnly.MinValue);
        var sessions = await schedule.GetWeekAsync(startsAt, endsAt, query.ClassId, query.RoomId, ct);
        return new WeekScheduleDto(weekStart, sessions);
    }
}
