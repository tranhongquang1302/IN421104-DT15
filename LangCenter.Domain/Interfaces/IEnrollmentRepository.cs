using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<bool> ExistsAsync(Guid classId, Guid studentId, CancellationToken ct = default);
    Task<int> CountByClassAsync(Guid classId, CancellationToken ct = default);
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Enrollment?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    void Add(Enrollment enrollment);
}
