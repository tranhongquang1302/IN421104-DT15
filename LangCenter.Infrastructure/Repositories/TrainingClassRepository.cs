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


    public async Task<TrainingClass?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A transaction is required to lock the training class.");

        if (!db.Database.IsNpgsql())
            return await db.TrainingClasses.AsTracking()
                .FirstOrDefaultAsync(trainingClass => trainingClass.Id == id, ct);

        // Ghi danh và khai giảng cùng khóa bản ghi lớp trong PostgreSQL.
        var rows = await db.TrainingClasses
            .FromSql($"""SELECT * FROM training_classes WHERE "Id" = {id} FOR UPDATE""")
            .AsTracking()
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    public async Task<IReadOnlyList<TrainingClass>> GetAllAsync(CancellationToken ct = default) =>
        await db.TrainingClasses.AsNoTracking()
            .OrderBy(trainingClass => trainingClass.Name)
            .ThenBy(trainingClass => trainingClass.Id)
            .ToListAsync(ct);
}
