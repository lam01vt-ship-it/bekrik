namespace Krik.Api.Entities;

/// <summary>Bracket hoa hồng version theo thời gian — không hardcode trong code.</summary>
public class CommissionBracket
{
    public Guid Id { get; set; }

    /// <summary>Vd NVBH_FT</summary>
    public string PositionCode { get; set; } = string.Empty;

    public string ContractType { get; set; } = string.Empty;

    public decimal KpiPctMin { get; set; }
    public decimal? KpiPctMax { get; set; }

    public decimal CommissionPct { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
