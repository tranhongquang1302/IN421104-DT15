using LangCenter.Application.Certificates;
using LangCenter.Domain.Entities;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class CertificateVerificationQuery(LangCenterDbContext db) : ICertificateVerificationQuery
{
    public Task<CertificateVerificationDto?> FindByLookupCodeAsync(string lookupCode, CancellationToken ct = default)
    {
        if (!Certificate.IsValidLookupCode(lookupCode))
            return Task.FromResult<CertificateVerificationDto?>(null);

        var code = lookupCode.ToUpperInvariant();
        return (from certificate in db.Certificates
                join enrollment in db.Enrollments on certificate.EnrollmentId equals enrollment.Id
                join student in db.Students on enrollment.StudentId equals student.Id
                join trainingClass in db.TrainingClasses on enrollment.TrainingClassId equals trainingClass.Id
                where certificate.LookupCode == code
                select new CertificateVerificationDto(student.FullName, trainingClass.Name, certificate.IssuedAt))
            .SingleOrDefaultAsync(ct);
    }
}
