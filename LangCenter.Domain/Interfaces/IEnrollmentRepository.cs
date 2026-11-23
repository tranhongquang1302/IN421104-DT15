using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<bool> ExistsAsync(Guid classId, Guid studentId, CancellationToken ct = default);
    void Add(Enrollment enrollment);
}
