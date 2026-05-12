namespace Krik.Api.Entities;

public class StoreDailySummary
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public DateOnly WorkDate { get; set; }

    public decimal ChannelRevenueMorning { get; set; }
    public decimal ChannelRevenueAfternoon { get; set; }
    public decimal ChannelRevenueEvening { get; set; }

    public int StoreCustomers { get; set; }
    public int StoreOrders { get; set; }
    public int StoreProducts { get; set; }

    public decimal StoreDayKpiTarget { get; set; }

    public decimal TongDoanhThuHeThong { get; set; }

    public bool IsDayLocked { get; set; }
}
