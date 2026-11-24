using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Tuition;

public record GetTuitionBalanceQuery(Guid EnrollmentId);

public record TuitionBalanceDto(Guid EnrollmentId, decimal TuitionDue, decimal PaidAmount)
{
    public decimal OutstandingAmount => TuitionDue - PaidAmount;
}

public class GetTuitionBalanceQueryHandler(IEnrollmentRepository enrollments, IPaymentRepository payments)
{
    public async Task<TuitionBalanceDto> HandleAsync(GetTuitionBalanceQuery query, CancellationToken ct = default)
    {
        var enrollment = await enrollments.GetByIdAsync(query.EnrollmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lượt ghi danh.");
        var paidAmount = await payments.SumByEnrollmentAsync(enrollment.Id, ct);
        return new TuitionBalanceDto(enrollment.Id, enrollment.TuitionDue, paidAmount);
    }
}
