namespace Krik.Api.Entities;

public class Store
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<StoreStaff> StaffMembers { get; set; } = new List<StoreStaff>();
    public ICollection<StoreDailySummary> DailySummaries { get; set; } = new List<StoreDailySummary>();
    public ICollection<StoreMonthlyKpiConfig> MonthlyKpiConfigs { get; set; } = new List<StoreMonthlyKpiConfig>();
}
