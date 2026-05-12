namespace Krik.Api.Entities;

public class StoreStaff
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public string StaffCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PositionCode { get; set; } = string.Empty;

    public string ContractType { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }
    public decimal TeamBonusBase { get; set; }

    public Guid? LinkedUserId { get; set; }
    public KrikUser? LinkedUser { get; set; }

    public ICollection<StaffDailyEntry> DailyEntries { get; set; } = new List<StaffDailyEntry>();
}
