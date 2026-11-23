using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class EnrollmentRepository(LangCenterDbContext db) : IEnrollmentRepository
{
    public Task<bool> ExistsAsync(Guid classId, Guid studentId, CancellationToken ct = default) =>
        db.Enrollments.AnyAsync(enrollment =>
            enrollment.TrainingClassId == classId && enrollment.StudentId == studentId, ct);

    public Task<int> CountByClassAsync(Guid classId, CancellationToken ct = default) =>
        db.Enrollments.CountAsync(enrollment => enrollment.TrainingClassId == classId, ct);

    public void Add(Enrollment enrollment) => db.Enrollments.Add(enrollment);
}
