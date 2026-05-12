namespace Krik.Api.Entities;

/// <summary>Một dòng nhập công + DT cá nhân theo ngày (giống 1 row NV trên sheet).</summary>
public class StaffDailyEntry
{
    public Guid Id { get; set; }
    public Guid StoreStaffId { get; set; }
    public StoreStaff StoreStaff { get; set; } = null!;

    public DateOnly WorkDate { get; set; }

    public decimal HoursMorning { get; set; }
    public decimal HoursAfternoon { get; set; }
    public decimal HoursEvening { get; set; }
    public decimal HoursExtra { get; set; }

    public decimal RevenueMorning { get; set; }
    public decimal RevenueAfternoon { get; set; }
    public decimal RevenueEvening { get; set; }

    public int Customers { get; set; }
    public int TryOns { get; set; }
    public int Orders { get; set; }
    public int Products { get; set; }

    /// <summary>Optimistic concurrency (save-on-blur / race).</summary>
    public int Version { get; set; }
}
