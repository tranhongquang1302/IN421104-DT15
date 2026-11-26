using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class ClassSessionRepository(LangCenterDbContext db) : IClassSessionRepository
{
    public Task<ClassSession?> GetByIdAsync(Guid classId, Guid id, CancellationToken ct = default) =>
        db.ClassSessions.SingleOrDefaultAsync(session => session.TrainingClassId == classId && session.Id == id, ct);

    public Task<ClassSession?> FindConflictAsync(ClassSession candidate, Guid? excludedId, CancellationToken ct = default) =>
        db.ClassSessions.AsNoTracking()
            .Where(session => session.Id != excludedId
                && session.StartsAt < candidate.EndsAt && candidate.StartsAt < session.EndsAt
                && (session.RoomId == candidate.RoomId
                    || session.InstructorId == candidate.InstructorId
                    || session.TrainingClassId == candidate.TrainingClassId))
            .OrderBy(session => session.StartsAt).ThenBy(session => session.Id).FirstOrDefaultAsync(ct);

    public void Add(ClassSession session) => db.ClassSessions.Add(session);
}
