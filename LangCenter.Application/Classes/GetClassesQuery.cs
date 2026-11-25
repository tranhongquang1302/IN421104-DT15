using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Classes;

public class GetClassesQueryHandler(ITrainingClassRepository classes)
{
    public async Task<IReadOnlyList<TrainingClassDto>> HandleAsync(CancellationToken ct = default)
    {
        var results = await classes.GetAllAsync(ct);
        return results.Select(TrainingClassDto.FromTrainingClass).ToArray();
    }

    public async Task<PagedResult<TrainingClassDto>> HandleAsync(PageRequest paging, CancellationToken ct = default)
    {
        var (items, totalCount) = await classes.GetPageAsync(paging.Skip, paging.PageSize, ct);
        return new PagedResult<TrainingClassDto>(
            items.Select(TrainingClassDto.FromTrainingClass).ToArray(), totalCount, paging.Page, paging.PageSize);
    }
}
