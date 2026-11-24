using LangCenter.Application.Classes;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages.Workspace;

public class DashboardModel(GetClassesQueryHandler getClasses, IEnrollmentRepository enrollments) : PageModel
{
    public int ClassCount { get; private set; }
    public int OpenCount { get; private set; }
    public int StartedCount { get; private set; }
    public IReadOnlyList<DashboardClassRow> Classes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var classes = await getClasses.HandleAsync(ct);
        ClassCount = classes.Count;
        OpenCount = classes.Count(row => row.Status == nameof(ClassStatus.Open));
        StartedCount = classes.Count(row => row.Status == nameof(ClassStatus.Started));
        var rows = new List<DashboardClassRow>();
        foreach (var trainingClass in classes.OrderBy(row => row.Status != nameof(ClassStatus.Open)).Take(5))
        {
            var enrollmentCount = await enrollments.CountByClassAsync(trainingClass.Id, ct);
            rows.Add(new DashboardClassRow(trainingClass, enrollmentCount));
        }

        Classes = rows;
    }

    public record DashboardClassRow(TrainingClassDto TrainingClass, int EnrollmentCount);
}
