namespace LangCenter.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public decimal Amount { get; private set; }
    public Guid? RequestId { get; private set; }
    public DateTime PaidAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid enrollmentId, decimal amount, Guid? requestId, DateTime paidAt)
    {
        if (enrollmentId == Guid.Empty)
            throw new ArgumentException("Phải xác định lượt ghi danh để thu học phí.", nameof(enrollmentId));
        if (amount <= 0 || amount > TrainingClass.MaxTuitionFee || decimal.Truncate(amount) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Khoản thanh toán phải là số nguyên đồng dương trong khoảng cho phép.");
        if (requestId == Guid.Empty)
            throw new ArgumentException("Mã yêu cầu thanh toán phải là UUID hợp lệ.", nameof(requestId));

        return new Payment
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            Amount = amount,
            RequestId = requestId,
            PaidAt = paidAt.AddTicks(-(paidAt.Ticks % TimeSpan.TicksPerMicrosecond))
        };
    }
}
