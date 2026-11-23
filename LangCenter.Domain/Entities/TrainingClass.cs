namespace LangCenter.Domain.Entities;

public enum ClassStatus
{
    Open,
    Started
}

public class TrainingClass
{
    public const int MaxNameLength = 150;
    public const decimal MaxTuitionFee = 99_999_999_999_999m;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int MinimumEnrollment { get; private set; }
    public decimal TuitionFee { get; private set; }
    public ClassStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }

    private TrainingClass() { }

    public static TrainingClass Create(string name, int minimumEnrollment, decimal tuitionFee)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            throw new ArgumentException($"Tên lớp phải có từ 1 đến {MaxNameLength} ký tự.", nameof(name));
        if (minimumEnrollment <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumEnrollment), "Sĩ số tối thiểu phải lớn hơn 0.");
        if (tuitionFee < 0 || tuitionFee > MaxTuitionFee || decimal.Truncate(tuitionFee) != tuitionFee)
            throw new ArgumentOutOfRangeException(nameof(tuitionFee), "Học phí phải là số nguyên đồng trong khoảng cho phép.");

        return new TrainingClass
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            MinimumEnrollment = minimumEnrollment,
            TuitionFee = tuitionFee,
            Status = ClassStatus.Open
        };
    }
}
