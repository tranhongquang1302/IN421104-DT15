using LangCenter.Domain.Entities;

namespace LangCenter.Application.Classes;

public record TrainingClassDto(Guid Id, string Name, int MinimumEnrollment, decimal TuitionFee,
    string Status, DateTime? StartedAt)
{
    public static TrainingClassDto FromTrainingClass(TrainingClass trainingClass) =>
        new(trainingClass.Id, trainingClass.Name, trainingClass.MinimumEnrollment, trainingClass.TuitionFee,
            trainingClass.Status.ToString(), trainingClass.StartedAt);
}
