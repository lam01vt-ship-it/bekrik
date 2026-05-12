namespace Krik.Api.Services;

/// <summary>Công thức cốt lõi đề mục 5 — dùng chung API + unit test.</summary>
public static class ShiftKpiMath
{
    public static bool IsSalesPosition(string positionCode)
    {
        return positionCode is "NVBH_FT" or "NVBH_PT";
    }

    /// <summary>5.1 — weight_NV = (gc sáng + chiều + tối + bổ sung) × is_sales</summary>
    public static decimal WeightNv(decimal gcMorning, decimal gcAfternoon, decimal gcEvening, decimal gcExtra, bool isSales)
    {
        if (!isSales) return 0;
        return gcMorning + gcAfternoon + gcEvening + gcExtra;
    }

    /// <summary>5.1 — target_NV và % (percent_NV = revenue / target × 100).</summary>
    public static (decimal targetNv, decimal percentNv) DailyPersonalTargets(
        decimal storeDayKpi,
        decimal weightNv,
        decimal totalWeight,
        decimal revenueNv)
    {
        if (storeDayKpi <= 0 || totalWeight <= 0 || weightNv <= 0)
            return (0m, 0m);

        var targetNv = storeDayKpi * weightNv / totalWeight;
        if (targetNv <= 0)
            return (0m, 0m);

        var percent = revenueNv / targetNv * 100m;
        return (targetNv, percent);
    }

    /// <summary>5.2 — adjusted target cho một ngày tương lai trong tuần (remaining × ratio / sumFutureRatios).</summary>
    public static decimal RebalancedDayTarget(
        decimal remainingWeeklyKpi,
        decimal dayRatio,
        decimal sumFutureDayRatios)
    {
        if (remainingWeeklyKpi <= 0 || sumFutureDayRatios <= 0)
            return 0m;
        return remainingWeeklyKpi * dayRatio / sumFutureDayRatios;
    }

    /// <summary>5.3 — chọn % hoa hồng theo KPI% đạt (kpiAchievedPct).</summary>
    public static decimal PickCommissionPct(
        IReadOnlyList<(decimal min, decimal? max, decimal pct)> brackets,
        decimal kpiAchievedPct)
    {
        foreach (var (min, max, pct) in brackets.OrderBy(b => b.min))
        {
            var okMin = kpiAchievedPct >= min;
            var okMax = max is null || kpiAchievedPct < max.Value;
            if (okMin && okMax)
                return pct;
        }

        return 0m;
    }

    /// <summary>5.4 — base + commission + team bonus QLCH.</summary>
    public static (decimal baseSalary, decimal commission, decimal teamBonus, decimal total) MonthlySalary(
        decimal hourlyRate,
        decimal totalHours,
        decimal totalRevenue,
        decimal commissionPct,
        bool isSales,
        bool isQlch,
        decimal teamBonusBase,
        decimal storeKpiAchievedPct)
    {
        var baseSalary = hourlyRate * totalHours;
        var commission = isSales ? totalRevenue * commissionPct / 100m : 0m;

        decimal teamBonus = 0;
        if (isQlch && teamBonusBase > 0)
        {
            teamBonus = storeKpiAchievedPct >= 100m ? teamBonusBase
                : storeKpiAchievedPct >= 90m ? teamBonusBase * 0.5m
                : 0m;
        }

        return (baseSalary, commission, teamBonus, baseSalary + commission + teamBonus);
    }
}
