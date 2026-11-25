using System.Data;
using LangCenter.Application.Common;
using LangCenter.Application.Reports;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class ClassTuitionReportQuery(LangCenterDbContext db) : IClassTuitionReportQuery
{
    public async Task<ClassTuitionReportDto?> GetByClassIdAsync(Guid classId, CancellationToken ct = default)
    {
        var className = await GetClassNameAsync(classId, ct);
        if (className is null)
            return null;

        var rows = await QueryRows(classId).ToListAsync(ct);
        return new ClassTuitionReportDto(classId, className, rows);
    }

    public async Task<PagedClassTuitionReportDto?> GetPageAsync(Guid classId, PageRequest paging,
        CancellationToken ct = default)
    {
        // Đọc dòng trên trang và tổng công nợ từ cùng một ảnh chụp dữ liệu.
        await using var transaction = db.Database.IsNpgsql()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct)
            : await db.Database.BeginTransactionAsync(ct);

        var className = await GetClassNameAsync(classId, ct);
        if (className is null)
            return null;

        var enrollments = db.Enrollments.Where(enrollment => enrollment.TrainingClassId == classId);
        var totals = await enrollments.GroupBy(enrollment => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                TuitionDue = group.Sum(enrollment => enrollment.TuitionDue)
            })
            .SingleOrDefaultAsync(ct);
        var paidAmount = await (from payment in db.Payments
                                join enrollment in enrollments on payment.EnrollmentId equals enrollment.Id
                                select payment.Amount).SumAsync(ct);
        var rows = await QueryRows(classId).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        await transaction.CommitAsync(ct);

        var page = new PagedResult<ClassTuitionReportRowDto>(rows, totals?.TotalCount ?? 0, paging.Page, paging.PageSize);
        return new PagedClassTuitionReportDto(classId, className, page, totals?.TuitionDue ?? 0m, paidAmount);
    }

    private Task<string?> GetClassNameAsync(Guid classId, CancellationToken ct) =>
        db.TrainingClasses.Where(trainingClass => trainingClass.Id == classId)
            .Select(trainingClass => trainingClass.Name).SingleOrDefaultAsync(ct);

    private IQueryable<ClassTuitionReportRowDto> QueryRows(Guid classId) =>
        from enrollment in db.Enrollments
        join student in db.Students on enrollment.StudentId equals student.Id
        where enrollment.TrainingClassId == classId
        orderby student.FullName, enrollment.Id
        select new ClassTuitionReportRowDto(enrollment.Id, student.FullName, enrollment.TuitionDue,
            db.Payments.Where(payment => payment.EnrollmentId == enrollment.Id)
                .Sum(payment => (decimal?)payment.Amount) ?? 0m);
}
