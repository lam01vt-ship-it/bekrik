using Krik.Api.Services;
using Xunit;

namespace Krik.Api.Tests;

public class ShiftKpiMathTests
{
    [Fact]
    public void DailyPersonalTargets_zeroSum_weights_match_spec_5_1()
    {
        var kpiDay = 100m;
        var w1 = 6m;
        var w2 = 4m;
        var total = 10m;
        var (t1, p1) = ShiftKpiMath.DailyPersonalTargets(kpiDay, w1, total, 50m);
        var (t2, p2) = ShiftKpiMath.DailyPersonalTargets(kpiDay, w2, total, 50m);
        Assert.Equal(60m, t1);
        Assert.Equal(40m, t2);
        Assert.Equal(100m, t1 + t2);
        Assert.Equal(50m / 60m * 100m, p1, 5);
        Assert.Equal(50m / 40m * 100m, p2, 5);
    }

    [Fact]
    public void RebalancedDayTarget_matches_spec_5_2()
    {
        var remaining = 1000m;
        var d1 = 0.4m;
        var d2 = 0.6m;
        var sum = d1 + d2;
        var a1 = ShiftKpiMath.RebalancedDayTarget(remaining, d1, sum);
        var a2 = ShiftKpiMath.RebalancedDayTarget(remaining, d2, sum);
        Assert.Equal(400m, a1);
        Assert.Equal(600m, a2);
    }

    [Fact]
    public void PickCommissionPct_ft_tiers_from_spec_5_3()
    {
        var tiers = new List<(decimal min, decimal? max, decimal pct)>
        {
            (0, 90, 0),
            (90, 100, 2.2m),
            (100, 110, 2.5m),
            (110, 120, 2.8m),
            (120, null, 3.2m)
        };
        Assert.Equal(0m, ShiftKpiMath.PickCommissionPct(tiers, 89m));
        Assert.Equal(2.2m, ShiftKpiMath.PickCommissionPct(tiers, 95m));
        Assert.Equal(2.5m, ShiftKpiMath.PickCommissionPct(tiers, 105m));
        Assert.Equal(3.2m, ShiftKpiMath.PickCommissionPct(tiers, 130m));
    }

    [Fact]
    public void MonthlySalary_matches_spec_5_4()
    {
        var (b, c, tb, tot) = ShiftKpiMath.MonthlySalary(50000m, 100m, 10_000_000m, 2.2m, true, false, 0, 95m);
        Assert.Equal(5_000_000m, b);
        Assert.Equal(220_000m, c);
        Assert.Equal(0m, tb);
        Assert.Equal(5_220_000m, tot);

        var (_, _, tb2, tot2) = ShiftKpiMath.MonthlySalary(100m, 1m, 0, 0, false, true, 2_000_000m, 100m);
        Assert.Equal(2_000_000m, tb2);
        Assert.Equal(100m + 2_000_000m, tot2);
    }
}
