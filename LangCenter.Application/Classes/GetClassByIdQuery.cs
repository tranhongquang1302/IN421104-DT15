using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Classes;

public record GetClassByIdQuery(Guid Id);

public class GetClassByIdQueryHandler(ITrainingClassRepository classes)
{
    public async Task<TrainingClassDto> HandleAsync(GetClassByIdQuery query, CancellationToken ct = default)
    {
        var trainingClass = await classes.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy lớp học.");
        return TrainingClassDto.FromTrainingClass(trainingClass);
    }
}
