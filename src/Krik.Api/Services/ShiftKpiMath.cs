namespace Krik.Api.Services;

using System.Text.Json;

public static class ShiftKpiMath
{
    public static bool IsSalesPosition(string positionCode)
    {
        return positionCode is "NVBH_FT" or "NVBH_PT";
    }

    public static decimal WeightNv(decimal gcMorning, decimal gcAfternoon, decimal gcEvening, decimal gcExtra, bool isSales)
    {
        if (!isSales) return 0;
        return gcMorning + gcAfternoon + gcEvening + gcExtra;
    }

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

    public static decimal RebalancedDayTarget(
        decimal remainingWeeklyKpi,
        decimal dayRatio,
        decimal sumFutureDayRatios)
    {
        if (remainingWeeklyKpi <= 0 || sumFutureDayRatios <= 0)
            return 0m;
        return remainingWeeklyKpi * dayRatio / sumFutureDayRatios;
    }

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

    /// <summary>
    /// KPI mục tiêu một ngày từ KPI tháng và JSON tỷ trọng 7 ngày (T2→CN, index 0 = Monday).
    /// Nếu JSON không hợp lệ thì chia đều theo số ngày trong tháng.
    /// </summary>
    public static decimal DailyTargetFromMonthConfig(decimal monthlyTargetAmount, string dayRatiosJson, DateOnly workDate)
    {
        if (monthlyTargetAmount <= 0m)
            return 0m;

        var dim = DateTime.DaysInMonth(workDate.Year, workDate.Month);
        var proportionalFallback = monthlyTargetAmount / dim;

        if (string.IsNullOrWhiteSpace(dayRatiosJson))
            return proportionalFallback;

        decimal[]? arr = null;
        try
        {
            arr = JsonSerializer.Deserialize<decimal[]>(dayRatiosJson);
        }
        catch (JsonException)
        {
            return proportionalFallback;
        }

        if (arr is null || arr.Length != 7)
            return proportionalFallback;

        var sum = arr.Sum();
        if (sum <= 0m)
            return proportionalFallback;

        var dow = workDate.DayOfWeek;
        var ix = dow == DayOfWeek.Sunday ? 6 : (int)dow - 1;
        return monthlyTargetAmount * arr[ix] / sum;
    }
}
