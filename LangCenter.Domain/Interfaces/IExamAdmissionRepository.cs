using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IExamAdmissionRepository
{
    Task<bool> ExistsAsync(Guid enrollmentId, CancellationToken ct = default);
    void Add(ExamAdmission admission);
}
