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
    decimal TongDoanhThuHeThong,
    bool IsDayLocked);

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
    decimal PercentNv,
    bool CanPatch);

public sealed record DailySheetDto(
    Guid StoreId,
    DateOnly WorkDate,
    bool MonthLocked,
    bool DayLocked,
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

public sealed record MonthLockPatchDto(bool Locked);

public sealed record DayLockPatchDto(bool Locked);

public sealed record StaffPositionPatchDto(string PositionCode);

public sealed record MonthlyDashboardDto(
    string YearMonth,
    decimal MonthlyTarget,
    decimal RevenueFromStaffEntries,
    decimal TongDoanhThuHeThongThang,
    decimal KpiAchievedPct,
    bool DiscrepancyOver5Pct,
    bool IsMonthLocked,
    IReadOnlyList<MonthlyDailySeriesItemDto> DailySeries,
    IReadOnlyList<MonthlyTopStaffDto> TopStaff);

public sealed record MonthlyDailySeriesItemDto(
    DateOnly WorkDate,
    decimal StaffRevenue,
    decimal ChannelRevenue,
    decimal StoreDayKpiTarget,
    bool IsDayLocked);

public sealed record MonthlyTopStaffDto(
    Guid StaffId,
    string StaffCode,
    string FullName,
    string PositionCode,
    decimal TotalRevenue,
    decimal TotalHours);

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
