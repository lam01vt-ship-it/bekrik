using Krik.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Data;

public static class StaffShiftKpiDemoSeed
{
    private static readonly Guid K01 = Guid.Parse("33333333-3333-3333-3333-333333333301");
    private static readonly Guid SalesUserId = Guid.Parse("44444444-4444-4444-4444-444444444404");

    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.StoreStaff.AnyAsync(s => s.StoreId == K01, cancellationToken))
            return;

        await SeedCommissionBracketsAsync(db, cancellationToken);

        var staff = CreateStaffRows();
        db.StoreStaff.AddRange(staff);
        await db.SaveChangesAsync(cancellationToken);

        var ym = new DateOnly(2026, 5, 1);
        db.StoreMonthlyKpiConfigs.Add(new StoreMonthlyKpiConfig
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666601"),
            StoreId = K01,
            YearMonth = ym,
            MonthlyTargetAmount = 1_500_000_000m,
            WeekRatiosJson = "[20,20,20,20,20]",
            DayRatiosJson = "[14.29,14.29,14.29,14.29,14.28,14.28,14.28]",
            ShiftRatiosJson = """{"weekday":{"morning":33.3,"afternoon":33.3,"evening":33.4},"weekend":{"morning":33.3,"afternoon":33.3,"evening":33.4}}""",
            IsMonthLocked = false,
            UpdatedAt = DateTimeOffset.UtcNow

        });

        var start = new DateOnly(2026, 5, 5);
        for (var i = 0; i < 7; i++)
        {
            var d = start.AddDays(i);
            var factor = 0.92m + 0.01m * i;
            var chMorning = 3_500_000m * factor;
            var chAfternoon = 11_000_000m * factor;
            var chEvening = 14_000_000m * factor;
            db.StoreDailySummaries.Add(new StoreDailySummary
            {
                Id = Guid.NewGuid(),
                StoreId = K01,
                WorkDate = d,
                ChannelRevenueMorning = chMorning,
                ChannelRevenueAfternoon = chAfternoon,
                ChannelRevenueEvening = chEvening,
                StoreCustomers = 30 + i,
                StoreOrders = 75 + i * 2,
                StoreProducts = 120 + i * 3,
                StoreDayKpiTarget = 42_000_000m,
                TongDoanhThuHeThong = chMorning + chAfternoon + chEvening
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var staffList = await db.StoreStaff.AsNoTracking().Where(s => s.StoreId == K01).ToListAsync(cancellationToken);
        var rnd = new Random(42);
        for (var di = 0; di < 7; di++)
        {
            var d = start.AddDays(di);
            foreach (var s in staffList)
            {
                var isSales = s.PositionCode is "NVBH_FT" or "NVBH_PT";
                var hM = isSales ? (decimal)(rnd.NextDouble() * 4 + 2) : (decimal)(rnd.NextDouble() * 3);
                var hA = isSales ? (decimal)(rnd.NextDouble() * 4 + 2) : (decimal)(rnd.NextDouble() * 3);
                var hE = isSales ? (decimal)(rnd.NextDouble() * 4) : (decimal)(rnd.NextDouble() * 2);
                var hX = 0m;
                db.StaffDailyEntries.Add(new StaffDailyEntry
                {
                    Id = Guid.NewGuid(),
                    StoreStaffId = s.Id,
                    WorkDate = d,
                    HoursMorning = Math.Round(hM, 2),
                    HoursAfternoon = Math.Round(hA, 2),
                    HoursEvening = Math.Round(hE, 2),
                    HoursExtra = hX,
                    RevenueMorning = isSales ? Math.Round((decimal)rnd.NextDouble() * 4_000_000m, 0) : 0,
                    RevenueAfternoon = isSales ? Math.Round((decimal)rnd.NextDouble() * 5_000_000m, 0) : 0,
                    RevenueEvening = isSales ? Math.Round((decimal)rnd.NextDouble() * 6_000_000m, 0) : 0,
                    Customers = isSales ? rnd.Next(20) : 0,
                    TryOns = isSales ? rnd.Next(15) : 0,
                    Orders = isSales ? rnd.Next(30) : 0,
                    Products = isSales ? rnd.Next(40) : 0,
                    Version = 1
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<StoreStaff> CreateStaffRows()
    {
        Guid S(string g) => Guid.Parse(g);
        return
        [
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555501"),
                StoreId = K01,
                StaffCode = "T103",
                FullName = "Nguyễn Thị My",
                PositionCode = "QLCH",
                ContractType = "CT",
                HourlyRate = 85_000m,
                TeamBonusBase = 3_000_000m,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555502"),
                StoreId = K01,
                StaffCode = "T1204",
                FullName = "Hoàng Thị Thu Hằng",
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                HourlyRate = 48_000m,
                TeamBonusBase = 0,
                LinkedUserId = SalesUserId
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555503"),
                StoreId = K01,
                StaffCode = "T322",
                FullName = "Lê Yến",
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                HourlyRate = 48_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555504"),
                StoreId = K01,
                StaffCode = "T1212",
                FullName = "Lê Hữu Hợp",
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                HourlyRate = 48_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555505"),
                StoreId = K01,
                StaffCode = "T1392",
                FullName = "Phùng Thị Hiêng",
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                HourlyRate = 48_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555506"),
                StoreId = K01,
                StaffCode = "T1630",
                FullName = "Vũ Thị Duyên",
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                HourlyRate = 48_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555507"),
                StoreId = K01,
                StaffCode = "T1752",
                FullName = "Nguyễn Thị Hải Yến",
                PositionCode = "NVBH_PT",
                ContractType = "TV",
                HourlyRate = 35_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            },
            new StoreStaff
            {
                Id = S("55555555-5555-5555-5555-555555555508"),
                StoreId = K01,
                StaffCode = "T50",
                FullName = "Nguyễn Văn Biên",
                PositionCode = "NVBV",
                ContractType = "CT",
                HourlyRate = 32_000m,
                TeamBonusBase = 0,
                LinkedUserId = null
            }
        ];
    }

    private static async Task SeedCommissionBracketsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.CommissionBrackets.AnyAsync(cancellationToken))
            return;

        var from = new DateOnly(2026, 1, 1);
        void AddFt(decimal min, decimal? max, decimal pct) =>
            db.CommissionBrackets.Add(new CommissionBracket
            {
                Id = Guid.NewGuid(),
                PositionCode = "NVBH_FT",
                ContractType = "CT",
                KpiPctMin = min,
                KpiPctMax = max,
                CommissionPct = pct,
                EffectiveFrom = from,
                EffectiveTo = null
            });

        AddFt(0, 90, 0);
        AddFt(90, 100, 2.2m);
        AddFt(100, 110, 2.5m);
        AddFt(110, 120, 2.8m);
        AddFt(120, null, 3.2m);

        void AddPt(decimal min, decimal? max, decimal pct) =>
            db.CommissionBrackets.Add(new CommissionBracket
            {
                Id = Guid.NewGuid(),
                PositionCode = "NVBH_PT",
                ContractType = "TV",
                KpiPctMin = min,
                KpiPctMax = max,
                CommissionPct = pct,
                EffectiveFrom = from,
                EffectiveTo = null
            });

        AddPt(0, 90, 0);
        AddPt(90, 100, 0.6m);
        AddPt(100, 110, 0.8m);
        AddPt(110, 120, 1.0m);
        AddPt(120, null, 1.2m);

        await db.SaveChangesAsync(cancellationToken);
    }
}
