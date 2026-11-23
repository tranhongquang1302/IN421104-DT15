using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface ITrainingClassRepository
{
    Task AddAsync(TrainingClass trainingClass, CancellationToken ct = default);
    Task<TrainingClass?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
