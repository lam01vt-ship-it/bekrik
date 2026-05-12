namespace Krik.Api.Entities;

/// <summary>Hồ sơ nhân viên cố định tại cửa hàng (tab nhân sự sheet 1.Nhập DL).</summary>
public class StoreStaff
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    /// <summary>Mã NV (vd T103).</summary>
    public string StaffCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>QLCH, CHP, NVBH_FT, NVBH_PT, NVTN, NVK, NVBV</summary>
    public string PositionCode { get; set; } = string.Empty;

    /// <summary>CT hoặc TV</summary>
    public string ContractType { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }
    public decimal TeamBonusBase { get; set; }

    /// <summary>Liên kết login NV (để Sales chỉ thấy dòng của mình).</summary>
    public Guid? LinkedUserId { get; set; }
    public KrikUser? LinkedUser { get; set; }

    public ICollection<StaffDailyEntry> DailyEntries { get; set; } = new List<StaffDailyEntry>();
}
