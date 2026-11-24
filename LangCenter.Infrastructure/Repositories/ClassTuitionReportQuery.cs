using LangCenter.Application.Reports;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class ClassTuitionReportQuery(LangCenterDbContext db) : IClassTuitionReportQuery
{
    public async Task<ClassTuitionReportDto?> GetByClassIdAsync(Guid classId, CancellationToken ct = default)
    {
        var className = await db.TrainingClasses.Where(trainingClass => trainingClass.Id == classId)
            .Select(trainingClass => trainingClass.Name).SingleOrDefaultAsync(ct);
        if (className is null)
            return null;

        var rows = await (from enrollment in db.Enrollments
                          join student in db.Students on enrollment.StudentId equals student.Id
                          where enrollment.TrainingClassId == classId
                          orderby student.FullName, enrollment.Id
                          select new ClassTuitionReportRowDto(enrollment.Id, student.FullName, enrollment.TuitionDue,
                              db.Payments.Where(payment => payment.EnrollmentId == enrollment.Id)
                                  .Sum(payment => (decimal?)payment.Amount) ?? 0m))
            .ToListAsync(ct);

        return new ClassTuitionReportDto(classId, className, rows);
    }
}
