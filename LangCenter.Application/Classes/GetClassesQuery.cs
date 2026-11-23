using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Classes;

public class GetClassesQueryHandler(ITrainingClassRepository classes)
{
    public async Task<IReadOnlyList<TrainingClassDto>> HandleAsync(CancellationToken ct = default)
    {
        var results = await classes.GetAllAsync(ct);
        return results.Select(TrainingClassDto.FromTrainingClass).ToArray();
    }
}
