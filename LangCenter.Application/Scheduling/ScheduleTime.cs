using LangCenter.Application.Common;

namespace LangCenter.Application.Scheduling;

public static class ScheduleTime
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        if (date == DateOnly.MinValue && time.ToTimeSpan() < VietnamOffset)
            throw new BusinessRuleException("Ngày xếp lịch nằm ngoài phạm vi hỗ trợ.");
        return new DateTimeOffset(date.ToDateTime(time), VietnamOffset).UtcDateTime;
    }

    public static DateTime ToLocal(DateTime utc) => utc.Add(VietnamOffset);

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysSinceMonday);
        if (monday == DateOnly.MinValue || monday > DateOnly.MaxValue.AddDays(-7))
            throw new BusinessRuleException("Tuần xếp lịch nằm ngoài phạm vi hỗ trợ.");
        return monday;
    }
}
