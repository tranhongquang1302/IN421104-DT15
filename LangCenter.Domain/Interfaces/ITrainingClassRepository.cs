using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface ITrainingClassRepository
{
    Task AddAsync(TrainingClass trainingClass, CancellationToken ct = default);
    Task<TrainingClass?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TrainingClass?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingClass>> GetAllAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<TrainingClass> Items, int TotalCount)> GetPageAsync(int skip, int take,
        CancellationToken ct = default);
}
