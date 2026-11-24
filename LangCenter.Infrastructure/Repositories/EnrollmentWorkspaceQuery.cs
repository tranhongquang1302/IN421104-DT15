using LangCenter.Application.Certificates;
using LangCenter.Application.Enrollments;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class EnrollmentWorkspaceQuery(LangCenterDbContext db) : IEnrollmentWorkspaceQuery
{
    public async Task<IReadOnlyList<ClassEnrollmentRow>> GetRosterAsync(Guid classId, CancellationToken ct = default) =>
        await (from enrollment in db.Enrollments
               join student in db.Students on enrollment.StudentId equals student.Id
               where enrollment.TrainingClassId == classId
               orderby student.FullName, student.Email
               select new ClassEnrollmentRow(enrollment.Id, student.Id, student.FullName, student.Email)).ToListAsync(ct);

    public Task<EnrollmentWorkspaceDto?> GetDetailsAsync(Guid id, CancellationToken ct = default) =>
        (from enrollment in db.Enrollments
         join student in db.Students on enrollment.StudentId equals student.Id
         join trainingClass in db.TrainingClasses on enrollment.TrainingClassId equals trainingClass.Id
         join certificate in db.Certificates on enrollment.Id equals certificate.EnrollmentId into certificates
         from certificate in certificates.DefaultIfEmpty()
         where enrollment.Id == id
         select new EnrollmentWorkspaceDto(enrollment.Id, trainingClass.Id, trainingClass.Name,
             trainingClass.Status.ToString(), student.FullName,
             db.ExamAdmissions.Any(admission => admission.EnrollmentId == enrollment.Id),
             certificate == null ? null : new CertificateDto(certificate.Id, enrollment.Id, certificate.LookupCode,
                 certificate.IssuedAt))).SingleOrDefaultAsync(ct);
}
