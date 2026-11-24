using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class CertificateRepository(LangCenterDbContext db) : ICertificateRepository
{
    public Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Certificates.AsNoTracking().SingleOrDefaultAsync(certificate => certificate.Id == id, ct);

    public Task<bool> ExistsAsync(Guid enrollmentId, CancellationToken ct = default) =>
        db.Certificates.AnyAsync(certificate => certificate.EnrollmentId == enrollmentId, ct);

    public void Add(Certificate certificate) => db.Certificates.Add(certificate);
}
