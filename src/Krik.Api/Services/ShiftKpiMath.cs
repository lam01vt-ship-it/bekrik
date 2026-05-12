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

        if (!TryParseDayRatios7(dayRatiosJson, out var arr, out var sum))
            return proportionalFallback;

        var dow = workDate.DayOfWeek;
        var ix = dow == DayOfWeek.Sunday ? 6 : (int)dow - 1;
        return monthlyTargetAmount * arr[ix] / sum;
    }

    /// <summary>Chia tháng thành 5 &quot;lát&quot; (ngày 1–7, 8–14, …) theo công thức đề bài.</summary>
    public static int WeekSliceIndexInMonth(DateOnly workDate)
    {
        var dim = DateTime.DaysInMonth(workDate.Year, workDate.Month);
        if (dim <= 0)
            return 0;
        return Math.Min(4, (workDate.Day - 1) * 5 / dim);
    }

    /// <summary>
    /// KPI ngày cửa hàng theo rebalance tuần (mục 5.2): phần KPI còn lại của &quot;lát tuần&quot; trong tháng
    /// chia theo tỷ trọng các ngày từ <paramref name="workDate"/> đến hết lát.
    /// </summary>
    public static bool TryWeeklyRebalancedStoreDayKpi(
        decimal monthlyTargetAmount,
        string weekRatiosJson,
        string dayRatiosJson,
        DateOnly workDate,
        IReadOnlyDictionary<DateOnly, decimal> staffRevenueByDate,
        out decimal storeDayKpi)
    {
        storeDayKpi = 0m;
        if (monthlyTargetAmount <= 0m)
            return false;

        decimal[]? weeks = null;
        try
        {
            weeks = JsonSerializer.Deserialize<decimal[]>(weekRatiosJson ?? "");
        }
        catch (JsonException)
        {
            return false;
        }

        if (weeks is null || weeks.Length != 5)
            return false;

        var sumW = weeks.Sum();
        if (sumW <= 0m)
            return false;

        var dim = DateTime.DaysInMonth(workDate.Year, workDate.Month);
        var wk = WeekSliceIndexInMonth(workDate);
        var weeklyTarget = monthlyTargetAmount * weeks[wk] / sumW;

        var sliceDates = new List<DateOnly>(dim);
        for (var day = 1; day <= dim; day++)
        {
            var dt = new DateOnly(workDate.Year, workDate.Month, day);
            if (WeekSliceIndexInMonth(dt) == wk)
                sliceDates.Add(dt);
        }

        if (sliceDates.Count == 0)
            return false;

        var sumPast = 0m;
        foreach (var dt in sliceDates)
        {
            if (dt < workDate)
                sumPast += staffRevenueByDate.GetValueOrDefault(dt);
        }

        var remaining = Math.Max(0m, weeklyTarget - sumPast);
        var futureDates = sliceDates.Where(dt => dt >= workDate).ToList();
        if (futureDates.Count == 0)
            return false;

        decimal[] dayArr;
        decimal daySum;
        if (!TryParseDayRatios7(dayRatiosJson, out dayArr!, out daySum))
        {
            dayArr = Enumerable.Repeat(1m, 7).ToArray();
            daySum = 7m;
        }

        var sumFutureRatios = 0m;
        foreach (var dt in futureDates)
        {
            var ix = DayOfWeekToIndex(dt.DayOfWeek);
            sumFutureRatios += dayArr[ix];
        }

        if (sumFutureRatios <= 0m)
            return false;

        var ixToday = DayOfWeekToIndex(workDate.DayOfWeek);
        var dayRatioToday = dayArr[ixToday];
        storeDayKpi = RebalancedDayTarget(remaining, dayRatioToday, sumFutureRatios);
        return true;
    }

    private static int DayOfWeekToIndex(DayOfWeek dow) =>
        dow == DayOfWeek.Sunday ? 6 : (int)dow - 1;

    private static bool TryParseDayRatios7(string dayRatiosJson, out decimal[] arr, out decimal sum)
    {
        arr = Array.Empty<decimal>();
        sum = 0m;
        if (string.IsNullOrWhiteSpace(dayRatiosJson))
            return false;
        try
        {
            var a = JsonSerializer.Deserialize<decimal[]>(dayRatiosJson);
            if (a is null || a.Length != 7)
                return false;
            sum = a.Sum();
            if (sum <= 0m)
                return false;
            arr = a;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
