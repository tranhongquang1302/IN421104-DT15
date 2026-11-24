using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class ExamAdmissionRepository(LangCenterDbContext db) : IExamAdmissionRepository
{
    public Task<bool> ExistsAsync(Guid enrollmentId, CancellationToken ct = default) =>
        db.ExamAdmissions.AnyAsync(admission => admission.EnrollmentId == enrollmentId, ct);

    public void Add(ExamAdmission admission) => db.ExamAdmissions.Add(admission);
}
