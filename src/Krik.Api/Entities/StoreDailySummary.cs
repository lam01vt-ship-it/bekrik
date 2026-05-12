namespace Krik.Api.Entities;

/// <summary>Tổng DT cấp cửa hàng theo ngày (hàng merge trên sheet: ca sáng/chiều/tối + chỉ số CH).</summary>
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

    /// <summary>KPI ngày giao cho CH (VND) — chia cho NV theo công thức 5.1.</summary>
    public decimal StoreDayKpiTarget { get; set; }

    /// <summary>Mock DT API (Nhanh/BQ) tổng ngày — để so sánh với tổng DT NV nhập.</summary>
    public decimal MockApiRevenueTotal { get; set; }
}
