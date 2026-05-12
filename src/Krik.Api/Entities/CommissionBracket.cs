namespace Krik.Api.Entities;

public class CommissionBracket
{
    public Guid Id { get; set; }

    public string PositionCode { get; set; } = string.Empty;

    public string ContractType { get; set; } = string.Empty;

    public decimal KpiPctMin { get; set; }
    public decimal? KpiPctMax { get; set; }

    public decimal CommissionPct { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
