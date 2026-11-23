using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Classes;

public record CreateClassCommand(string Name, int MinimumEnrollment, decimal TuitionFee);

public class CreateClassCommandHandler(ITrainingClassRepository classes)
{
    public async Task<TrainingClassDto> HandleAsync(CreateClassCommand command, CancellationToken ct = default)
    {
        TrainingClass trainingClass;
        try
        {
            trainingClass = TrainingClass.Create(command.Name, command.MinimumEnrollment, command.TuitionFee);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        await classes.AddAsync(trainingClass, ct);
        return TrainingClassDto.FromTrainingClass(trainingClass);
    }
}
