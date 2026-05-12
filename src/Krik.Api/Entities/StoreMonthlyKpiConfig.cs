namespace Krik.Api.Entities;

/// <summary>Cấu hình KPI tháng (Admin); tỷ trọng lưu JSON để derive daily/weekly sau này.</summary>
public class StoreMonthlyKpiConfig
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    /// <summary>Ngày 01 của tháng.</summary>
    public DateOnly YearMonth { get; set; }

    public decimal MonthlyTargetAmount { get; set; }

    public string WeekRatiosJson { get; set; } = "[]";
    public string DayRatiosJson { get; set; } = "[]";
    public string ShiftRatiosJson { get; set; } = "{}";

    public bool IsMonthLocked { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
