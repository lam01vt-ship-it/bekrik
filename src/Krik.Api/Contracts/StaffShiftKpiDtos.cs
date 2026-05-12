namespace Krik.Api.Contracts;

public sealed record StoreStaffDto(
    Guid Id,
    string StaffCode,
    string FullName,
    string PositionCode,
    string ContractType,
    decimal HourlyRate,
    decimal TeamBonusBase,
    Guid? LinkedUserId,
    string? LinkedEmail);

public sealed record StoreStaffWriteDto(
    string StaffCode,
    string FullName,
    string PositionCode,
    string ContractType,
    decimal HourlyRate,
    decimal TeamBonusBase,
    Guid? LinkedUserId,
    string? LoginEmail,
    string? LoginPassword);

public sealed record StoreDailySummaryDto(
    DateOnly WorkDate,
    decimal ChannelRevenueMorning,
    decimal ChannelRevenueAfternoon,
    decimal ChannelRevenueEvening,
    int StoreCustomers,
    int StoreOrders,
    int StoreProducts,
    decimal StoreDayKpiTarget,
    decimal TongDoanhThuHeThong);

public sealed record DailyEntryRowDto(
    Guid EntryId,
    Guid StaffId,
    string StaffCode,
    string FullName,
    string PositionCode,
    string ContractType,
    int Version,
    decimal HoursMorning,
    decimal HoursAfternoon,
    decimal HoursEvening,
    decimal HoursExtra,
    decimal RevenueMorning,
    decimal RevenueAfternoon,
    decimal RevenueEvening,
    int Customers,
    int TryOns,
    int Orders,
    int Products,
    decimal WeightNv,
    decimal TargetNv,
    decimal RevenueTotal,
    decimal PercentNv);

public sealed record DailySheetDto(
    Guid StoreId,
    DateOnly WorkDate,
    StoreDailySummaryDto Summary,
    IReadOnlyList<DailyEntryRowDto> Rows);

public sealed record DailyEntryPatchDto(
    Guid EntryId,
    int ExpectedVersion,
    decimal? HoursMorning,
    decimal? HoursAfternoon,
    decimal? HoursEvening,
    decimal? HoursExtra,
    decimal? RevenueMorning,
    decimal? RevenueAfternoon,
    decimal? RevenueEvening,
    int? Customers,
    int? TryOns,
    int? Orders,
    int? Products);

public sealed record StoreMonthlyKpiConfigDto(
    Guid StoreId,
    string YearMonth,
    decimal MonthlyTargetAmount,
    string WeekRatiosJson,
    string DayRatiosJson,
    string ShiftRatiosJson,
    bool IsMonthLocked,
    DateTimeOffset UpdatedAt);

public sealed record StoreMonthlyKpiConfigWriteDto(
    decimal MonthlyTargetAmount,
    string WeekRatiosJson,
    string DayRatiosJson,
    string ShiftRatiosJson);

public sealed record MonthlyDashboardDto(
    string YearMonth,
    decimal MonthlyTarget,
    decimal RevenueFromStaffEntries,
    decimal TongDoanhThuHeThongThang,
    decimal KpiAchievedPct,
    bool DiscrepancyOver5Pct,
    bool IsMonthLocked);

public sealed record PayrollRowDto(
    Guid StaffId,
    string StaffCode,
    string FullName,
    string PositionCode,
    string ContractType,
    decimal HourlyRate,
    decimal HoursMorning,
    decimal HoursAfternoon,
    decimal HoursEvening,
    decimal HoursExtra,
    decimal TotalHours,
    decimal RevenueMorning,
    decimal RevenueAfternoon,
    decimal RevenueEvening,
    decimal TotalRevenue,
    decimal CommissionPctApplied,
    decimal SalaryRevenue,
    decimal SalaryFixed,
    decimal TeamBonus,
    decimal TotalSalary,
    decimal? HypotheticalSalaryRevenueAt95);
