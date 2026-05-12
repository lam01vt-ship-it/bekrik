namespace Krik.Api.Entities;

public class StoreMonthlyKpiConfig
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public DateOnly YearMonth { get; set; }

    public decimal MonthlyTargetAmount { get; set; }

    public string WeekRatiosJson { get; set; } = "[]";
    public string DayRatiosJson { get; set; } = "[]";
    public string ShiftRatiosJson { get; set; } = "{}";

    public bool IsMonthLocked { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
