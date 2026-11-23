using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class TrainingClassRepository(LangCenterDbContext db) : ITrainingClassRepository
{
    public async Task AddAsync(TrainingClass trainingClass, CancellationToken ct = default)
    {
        db.TrainingClasses.Add(trainingClass);
        await db.SaveChangesAsync(ct);
    }

    public Task<TrainingClass?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.TrainingClasses.AsNoTracking().FirstOrDefaultAsync(trainingClass => trainingClass.Id == id, ct);

    public async Task<IReadOnlyList<TrainingClass>> GetAllAsync(CancellationToken ct = default) =>
        await db.TrainingClasses.AsNoTracking()
            .OrderBy(trainingClass => trainingClass.Name)
            .ThenBy(trainingClass => trainingClass.Id)
            .ToListAsync(ct);
}
