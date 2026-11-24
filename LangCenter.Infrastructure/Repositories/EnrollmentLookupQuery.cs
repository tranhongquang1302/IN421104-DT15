using LangCenter.Application.Tuition;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class EnrollmentLookupQuery(LangCenterDbContext db) : IEnrollmentLookupQuery
{
    public async Task<IReadOnlyList<EnrollmentLookupDto>> FindByStudentEmailAsync(
        string studentEmail, CancellationToken ct = default)
    {
        var email = studentEmail.Trim().ToLowerInvariant();
        return await (from enrollment in db.Enrollments
                      join student in db.Students on enrollment.StudentId equals student.Id
                      join trainingClass in db.TrainingClasses on enrollment.TrainingClassId equals trainingClass.Id
                      where student.Email == email
                      orderby trainingClass.Name, student.FullName, enrollment.Id
                      select new EnrollmentLookupDto(enrollment.Id, trainingClass.Id, trainingClass.Name, student.FullName))
            .ToListAsync(ct);
    }
}
