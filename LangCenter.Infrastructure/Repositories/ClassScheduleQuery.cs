using LangCenter.Application.Scheduling;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class ClassScheduleQuery(LangCenterDbContext db) : IClassScheduleQuery
{
    public async Task<IReadOnlyList<ClassSessionDto>> GetWeekAsync(DateTime startsAt, DateTime endsAt,
        Guid? classId, Guid? roomId, CancellationToken ct = default)
    {
        var sessions = db.ClassSessions.AsNoTracking()
            .Where(session => session.StartsAt >= startsAt && session.StartsAt < endsAt);
        if (classId is Guid selectedClass)
            sessions = sessions.Where(session => session.TrainingClassId == selectedClass);
        if (roomId is Guid selectedRoom)
            sessions = sessions.Where(session => session.RoomId == selectedRoom);

        return await (from session in sessions
                      join trainingClass in db.TrainingClasses on session.TrainingClassId equals trainingClass.Id
                      join room in db.Rooms on session.RoomId equals room.Id
                      join instructor in db.Instructors on session.InstructorId equals instructor.Id
                      orderby session.StartsAt, session.Id
                      select new ClassSessionDto(session.Id, trainingClass.Id, trainingClass.Name,
                          room.Id, room.Code, room.Name, instructor.Id, instructor.Code, instructor.FullName,
                          session.StartsAt, session.EndsAt)).ToListAsync(ct);
    }
}
