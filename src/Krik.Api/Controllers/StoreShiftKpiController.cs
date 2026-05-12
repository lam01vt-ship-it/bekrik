using System.Security.Claims;
using ClosedXML.Excel;
using Krik.Api.Contracts;
using Krik.Api.Data;
using Krik.Api.Entities;
using Krik.Api.Security;
using Krik.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Krik.Api.Controllers;

[ApiController]
[Route("api/stores/{storeId:guid}/shift-kpi")]
[Authorize]
public sealed class StoreShiftKpiController(AppDbContext db) : ControllerBase
{
    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<StoreStaffDto>>> GetStaff(Guid storeId, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();

        var q = db.StoreStaff.AsNoTracking().Where(s => s.StoreId == storeId);
        if (IsSalesOnlySelf())
        {
            var uid = GetUserId();
            if (uid is null) return Unauthorized();
            q = q.Where(s => s.LinkedUserId == uid);
        }

        var list = await q.OrderBy(s => s.StaffCode)
            .Select(s => new StoreStaffDto(
                s.Id,
                s.StaffCode,
                s.FullName,
                s.PositionCode,
                s.ContractType,
                s.HourlyRate,
                s.TeamBonusBase,
                s.LinkedUserId,
                s.LinkedUser != null ? s.LinkedUser.Email : null))
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost("staff")]
    public async Task<ActionResult<StoreStaffDto>> PostStaff(Guid storeId, [FromBody] StoreStaffWriteDto body, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!StoreAccess.CanEditStaffMaster(User))
            return Forbid();

        var err = ValidateStaffWrite(body);
        if (err is not null) return BadRequest(err);
        if (body.LinkedUserId is not null)
            return BadRequest("Tạo tài khoản đăng nhập bằng email và mật khẩu, không gửi liên kết người dùng có sẵn.");

        var loginErr = ValidateLoginCredentials(body.LoginEmail, body.LoginPassword, required: true);
        if (loginErr is not null) return BadRequest(loginErr);

        var emailNorm = NormalizeEmail(body.LoginEmail!);
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm, cancellationToken))
            return Conflict("Email đăng nhập đã tồn tại.");

        var entity = new StoreStaff
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            StaffCode = body.StaffCode.Trim(),
            FullName = body.FullName.Trim(),
            PositionCode = body.PositionCode.Trim().ToUpperInvariant(),
            ContractType = body.ContractType.Trim().ToUpperInvariant(),
            HourlyRate = body.HourlyRate,
            TeamBonusBase = body.TeamBonusBase,
            LinkedUserId = null
        };

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var roleName = RoleNameForPosition(entity.PositionCode);
            var role = await db.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
            var user = new KrikUser
            {
                Id = Guid.NewGuid(),
                Email = emailNorm,
                FullName = entity.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.LoginPassword!),
                StoreId = storeId
            };
            user.UserRoles.Add(new UserRole { User = user, Role = role });
            db.Users.Add(user);

            entity.LinkedUserId = user.Id;
            db.StoreStaff.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Ok(new StoreStaffDto(entity.Id, entity.StaffCode, entity.FullName, entity.PositionCode, entity.ContractType, entity.HourlyRate, entity.TeamBonusBase, entity.LinkedUserId, user.Email));
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return Conflict("Trùng mã NV trong cửa hàng.");
        }
    }

    [HttpPut("staff/{staffId:guid}")]
    public async Task<ActionResult<StoreStaffDto>> PutStaff(Guid storeId, Guid staffId, [FromBody] StoreStaffWriteDto body, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!StoreAccess.CanEditStaffMaster(User))
            return Forbid();

        var err = ValidateStaffWrite(body);
        if (err is not null) return BadRequest(err);

        var entity = await db.StoreStaff.FirstOrDefaultAsync(s => s.Id == staffId && s.StoreId == storeId, cancellationToken);
        if (entity is null) return NotFound();

        entity.StaffCode = body.StaffCode.Trim();
        entity.FullName = body.FullName.Trim();
        entity.PositionCode = body.PositionCode.Trim().ToUpperInvariant();
        entity.ContractType = body.ContractType.Trim().ToUpperInvariant();
        entity.HourlyRate = body.HourlyRate;
        entity.TeamBonusBase = body.TeamBonusBase;

        if (HasLoginCredentials(body.LoginEmail, body.LoginPassword))
        {
            if (entity.LinkedUserId is not null)
                return BadRequest("Nhân viên đã có tài khoản đăng nhập.");
            var loginErr = ValidateLoginCredentials(body.LoginEmail, body.LoginPassword, required: true);
            if (loginErr is not null) return BadRequest(loginErr);
            var emailNorm = NormalizeEmail(body.LoginEmail!);
            if (await db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm, cancellationToken))
                return Conflict("Email đăng nhập đã tồn tại.");

            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var roleName = RoleNameForPosition(entity.PositionCode);
                var role = await db.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
                var user = new KrikUser
                {
                    Id = Guid.NewGuid(),
                    Email = emailNorm,
                    FullName = entity.FullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.LoginPassword!),
                    StoreId = storeId
                };
                user.UserRoles.Add(new UserRole { User = user, Role = role });
                db.Users.Add(user);
                entity.LinkedUserId = user.Id;
                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(cancellationToken);
                return Conflict("Không tạo được tài khoản (email trùng).");
            }

            return Ok(new StoreStaffDto(entity.Id, entity.StaffCode, entity.FullName, entity.PositionCode, entity.ContractType, entity.HourlyRate, entity.TeamBonusBase, entity.LinkedUserId, emailNorm));
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Trùng mã NV trong cửa hàng.");
        }

        var linkedEmail = entity.LinkedUserId is { } uid
            ? await db.Users.AsNoTracking().Where(u => u.Id == uid).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken)
            : null;
        return Ok(new StoreStaffDto(entity.Id, entity.StaffCode, entity.FullName, entity.PositionCode, entity.ContractType, entity.HourlyRate, entity.TeamBonusBase, entity.LinkedUserId, linkedEmail));
    }

    [HttpDelete("staff/{staffId:guid}")]
    public async Task<IActionResult> DeleteStaff(Guid storeId, Guid staffId, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!StoreAccess.CanEditStaffMaster(User))
            return Forbid();

        var entity = await db.StoreStaff.FirstOrDefaultAsync(s => s.Id == staffId && s.StoreId == storeId, cancellationToken);
        if (entity is null) return NotFound();
        db.StoreStaff.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("daily")]
    public async Task<ActionResult<DailySheetDto>> GetDaily(Guid storeId, [FromQuery] string workDate, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!DateOnly.TryParse(workDate, out var d))
            return BadRequest("Tham số ngày phải có định dạng yyyy-MM-dd.");

        await EnsureDailyEntriesAsync(storeId, d, cancellationToken);
        var sheet = await TryBuildDailySheetAsync(storeId, d, cancellationToken);
        if (sheet is null)
            return Unauthorized();
        return Ok(sheet);
    }

    [HttpGet("daily-export")]
    public async Task<IActionResult> ExportDaily(Guid storeId, [FromQuery] string workDate, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!DateOnly.TryParse(workDate, out var d))
            return BadRequest("Tham số ngày phải có định dạng yyyy-MM-dd.");

        await EnsureDailyEntriesAsync(storeId, d, cancellationToken);
        var sheet = await TryBuildDailySheetAsync(storeId, d, cancellationToken);
        if (sheet is null)
            return Unauthorized();

        using var wb = new XLWorkbook();
        var wsSum = wb.AddWorksheet("Tổng hợp");
        wsSum.Cell(1, 1).Value = "Chỉ tiêu";
        wsSum.Cell(1, 2).Value = "Giá trị";
        var sum = sheet.Summary;
        wsSum.Cell(2, 1).Value = "Ngày";
        wsSum.Cell(2, 2).Value = sum.WorkDate.ToString("yyyy-MM-dd");
        wsSum.Cell(3, 1).Value = "Doanh thu kênh ca sáng";
        wsSum.Cell(3, 2).Value = sum.ChannelRevenueMorning;
        wsSum.Cell(4, 1).Value = "Doanh thu kênh ca chiều";
        wsSum.Cell(4, 2).Value = sum.ChannelRevenueAfternoon;
        wsSum.Cell(5, 1).Value = "Doanh thu kênh ca tối";
        wsSum.Cell(5, 2).Value = sum.ChannelRevenueEvening;
        wsSum.Cell(6, 1).Value = "Khách hàng (CH)";
        wsSum.Cell(6, 2).Value = sum.StoreCustomers;
        wsSum.Cell(7, 1).Value = "Đơn hàng (CH)";
        wsSum.Cell(7, 2).Value = sum.StoreOrders;
        wsSum.Cell(8, 1).Value = "Sản phẩm (CH)";
        wsSum.Cell(8, 2).Value = sum.StoreProducts;
        wsSum.Cell(9, 1).Value = "KPI ngày cửa hàng";
        wsSum.Cell(9, 2).Value = sum.StoreDayKpiTarget;
        wsSum.Cell(10, 1).Value = "Tổng doanh thu kênh (ba ca)";
        wsSum.Cell(10, 2).Value = sum.TongDoanhThuHeThong;
        wsSum.Row(1).Style.Font.Bold = true;
        wsSum.SheetView.FreezeRows(1);

        var ws = wb.AddWorksheet("Bảng công");
        var headers = new[]
        {
            "STT", "Mã NV", "Họ tên", "Chức danh", "Hợp đồng",
            "Giờ công ca sáng", "Giờ công ca chiều", "Giờ công ca tối", "Giờ công bổ sung",
            "Doanh thu ca sáng", "Doanh thu ca chiều", "Doanh thu ca tối",
            "Khách hàng", "Lượt thử đồ", "Đơn hàng", "Sản phẩm",
            "Mục tiêu DT NV", "Doanh thu NV (ba ca)", "% KPI NV",
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var r = 2;
        var idx = 1;
        foreach (var x in sheet.Rows)
        {
            ws.Cell(r, 1).Value = idx++;
            ws.Cell(r, 2).Value = x.StaffCode;
            ws.Cell(r, 3).Value = x.FullName;
            ws.Cell(r, 4).Value = x.PositionCode;
            ws.Cell(r, 5).Value = x.ContractType;
            ws.Cell(r, 6).Value = x.HoursMorning;
            ws.Cell(r, 7).Value = x.HoursAfternoon;
            ws.Cell(r, 8).Value = x.HoursEvening;
            ws.Cell(r, 9).Value = x.HoursExtra;
            ws.Cell(r, 10).Value = x.RevenueMorning;
            ws.Cell(r, 11).Value = x.RevenueAfternoon;
            ws.Cell(r, 12).Value = x.RevenueEvening;
            ws.Cell(r, 13).Value = x.Customers;
            ws.Cell(r, 14).Value = x.TryOns;
            ws.Cell(r, 15).Value = x.Orders;
            ws.Cell(r, 16).Value = x.Products;
            ws.Cell(r, 17).Value = x.TargetNv;
            ws.Cell(r, 18).Value = x.RevenueTotal;
            ws.Cell(r, 19).Value = x.PercentNv;
            r++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);
        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"bang-con-ngay-{storeId}-{workDate}.xlsx");
    }

    private async Task<decimal> ResolveStoreDayKpiTargetAsync(
        Guid storeId,
        DateOnly workDate,
        StoreDailySummary? summary,
        CancellationToken cancellationToken)
    {
        var ym = new DateOnly(workDate.Year, workDate.Month, 1);
        var cfg = await db.StoreMonthlyKpiConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.YearMonth == ym, cancellationToken);
        if (cfg is { MonthlyTargetAmount: > 0m })
            return ShiftKpiMath.DailyTargetFromMonthConfig(cfg.MonthlyTargetAmount, cfg.DayRatiosJson, workDate);

        return summary?.StoreDayKpiTarget ?? 0m;
    }

    private async Task<DailySheetDto?> TryBuildDailySheetAsync(
        Guid storeId,
        DateOnly d,
        CancellationToken cancellationToken)
    {
        var summary = await db.StoreDailySummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.WorkDate == d, cancellationToken);

        var kpiDay = await ResolveStoreDayKpiTargetAsync(storeId, d, summary, cancellationToken);

        var summaryDto = summary is null
            ? new StoreDailySummaryDto(d, 0, 0, 0, 0, 0, 0, kpiDay, 0)
            : new StoreDailySummaryDto(
                summary.WorkDate,
                summary.ChannelRevenueMorning,
                summary.ChannelRevenueAfternoon,
                summary.ChannelRevenueEvening,
                summary.StoreCustomers,
                summary.StoreOrders,
                summary.StoreProducts,
                kpiDay,
                summary.TongDoanhThuHeThong);

        var allStaff = await db.StoreStaff.AsNoTracking().Where(s => s.StoreId == storeId).OrderBy(s => s.StaffCode).ToListAsync(cancellationToken);
        var staffIdSet = allStaff.Select(s => s.Id).ToList();
        var entryRows = await db.StaffDailyEntries
            .AsNoTracking()
            .Where(e => e.WorkDate == d && staffIdSet.Contains(e.StoreStaffId))
            .ToListAsync(cancellationToken);
        var allEntries = entryRows
            .GroupBy(e => e.StoreStaffId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

        var totalW = allStaff.Sum(s =>
        {
            var e = allEntries.GetValueOrDefault(s.Id);
            return ShiftKpiMath.WeightNv(e?.HoursMorning ?? 0, e?.HoursAfternoon ?? 0, e?.HoursEvening ?? 0, e?.HoursExtra ?? 0, ShiftKpiMath.IsSalesPosition(s.PositionCode));
        });

        IEnumerable<StoreStaff> visibleStaff = allStaff;
        if (IsSalesOnlySelf())
        {
            var uid = GetUserId();
            if (uid is null)
                return null;
            visibleStaff = allStaff.Where(s => s.LinkedUserId == uid);
        }

        var rows = new List<DailyEntryRowDto>();
        foreach (var s in visibleStaff)
        {
            if (!allEntries.TryGetValue(s.Id, out var entry))
                continue;

            var isSales = ShiftKpiMath.IsSalesPosition(s.PositionCode);
            var w = ShiftKpiMath.WeightNv(entry.HoursMorning, entry.HoursAfternoon, entry.HoursEvening, entry.HoursExtra, isSales);
            var rev = entry.RevenueMorning + entry.RevenueAfternoon + entry.RevenueEvening;
            var (target, pct) = ShiftKpiMath.DailyPersonalTargets(kpiDay, w, totalW, rev);
            rows.Add(new DailyEntryRowDto(
                entry.Id,
                s.Id,
                s.StaffCode,
                s.FullName,
                s.PositionCode,
                s.ContractType,
                entry.Version,
                entry.HoursMorning,
                entry.HoursAfternoon,
                entry.HoursEvening,
                entry.HoursExtra,
                entry.RevenueMorning,
                entry.RevenueAfternoon,
                entry.RevenueEvening,
                entry.Customers,
                entry.TryOns,
                entry.Orders,
                entry.Products,
                w,
                target,
                rev,
                pct));
        }

        return new DailySheetDto(storeId, d, summaryDto, rows);
    }

    [HttpPatch("daily-entry")]
    public async Task<ActionResult<DailyEntryRowDto>> PatchDailyEntry(Guid storeId, [FromBody] DailyEntryPatchDto body, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();

        var entry = await db.StaffDailyEntries
            .Include(e => e.StoreStaff)
            .FirstOrDefaultAsync(e => e.Id == body.EntryId && e.StoreStaff.StoreId == storeId, cancellationToken);
        if (entry is null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (entry.WorkDate < today && !StoreAccess.CanEditPastShiftDailyWorkDate(User))
            return Forbid();

        if (IsSalesOnlySelf())
        {
            var uid = GetUserId();
            if (uid is null || entry.StoreStaff.LinkedUserId != uid)
                return Forbid();
        }

        if (entry.Version != body.ExpectedVersion)
        {
            var refreshed = await BuildRowDtoAsync(storeId, entry, cancellationToken);
            return Conflict(new { message = "Dữ liệu đã thay đổi (race). Tải lại trước khi sửa.", row = refreshed });
        }

        try
        {
            if (body.HoursMorning is { } hm) entry.HoursMorning = hm < 0 ? throw new InvalidOperationException() : hm;
            if (body.HoursAfternoon is { } ha) entry.HoursAfternoon = ha < 0 ? throw new InvalidOperationException() : ha;
            if (body.HoursEvening is { } he) entry.HoursEvening = he < 0 ? throw new InvalidOperationException() : he;
            if (body.HoursExtra is { } hx) entry.HoursExtra = hx < 0 ? throw new InvalidOperationException() : hx;
            if (body.RevenueMorning is { } rm) entry.RevenueMorning = rm < 0 ? throw new InvalidOperationException() : rm;
            if (body.RevenueAfternoon is { } ra) entry.RevenueAfternoon = ra < 0 ? throw new InvalidOperationException() : ra;
            if (body.RevenueEvening is { } re) entry.RevenueEvening = re < 0 ? throw new InvalidOperationException() : re;
            if (body.Customers is { } c) entry.Customers = c < 0 ? throw new InvalidOperationException() : c;
            if (body.TryOns is { } t) entry.TryOns = t < 0 ? throw new InvalidOperationException() : t;
            if (body.Orders is { } o) entry.Orders = o < 0 ? throw new InvalidOperationException() : o;
            if (body.Products is { } p) entry.Products = p < 0 ? throw new InvalidOperationException() : p;
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Giờ công và doanh thu phải ≥ 0.");
        }

        entry.Version++;
        await db.SaveChangesAsync(cancellationToken);

        var dto = await BuildRowDtoAsync(storeId, entry, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("kpi-months/{yearMonth}")]
    public async Task<ActionResult<StoreMonthlyKpiConfigDto>> GetKpiMonth(Guid storeId, string yearMonth, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!TryParseYearMonth(yearMonth, out var ym))
            return BadRequest("Tham số tháng phải có định dạng yyyy-MM.");

        var cfg = await db.StoreMonthlyKpiConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.YearMonth == ym, cancellationToken);
        if (cfg is null)
        {
            return Ok(new StoreMonthlyKpiConfigDto(storeId, yearMonth, 0, "[]", "[]", "{}", false, DateTimeOffset.UtcNow));
        }

        return Ok(new StoreMonthlyKpiConfigDto(storeId, yearMonth, cfg.MonthlyTargetAmount, cfg.WeekRatiosJson, cfg.DayRatiosJson, cfg.ShiftRatiosJson, cfg.IsMonthLocked, cfg.UpdatedAt));
    }

    [HttpPut("kpi-months/{yearMonth}")]
    public async Task<ActionResult<StoreMonthlyKpiConfigDto>> PutKpiMonth(Guid storeId, string yearMonth, [FromBody] StoreMonthlyKpiConfigWriteDto body, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!StoreAccess.CanEditKpiMonthConfig(User))
            return Forbid();
        if (!TryParseYearMonth(yearMonth, out var ym))
            return BadRequest("Tham số tháng phải có định dạng yyyy-MM.");

        if (body.MonthlyTargetAmount < 0)
            return BadRequest("KPI tháng phải ≥ 0.");

        var entity = await db.StoreMonthlyKpiConfigs.FirstOrDefaultAsync(c => c.StoreId == storeId && c.YearMonth == ym, cancellationToken);
        if (entity is null)
        {
            entity = new StoreMonthlyKpiConfig { Id = Guid.NewGuid(), StoreId = storeId, YearMonth = ym };
            db.StoreMonthlyKpiConfigs.Add(entity);
        }

        if (entity.IsMonthLocked)
            return Conflict("Tháng đã khoá — không sửa cấu hình.");

        entity.MonthlyTargetAmount = body.MonthlyTargetAmount;
        entity.WeekRatiosJson = body.WeekRatiosJson;
        entity.DayRatiosJson = body.DayRatiosJson;
        entity.ShiftRatiosJson = body.ShiftRatiosJson;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new StoreMonthlyKpiConfigDto(storeId, yearMonth, entity.MonthlyTargetAmount, entity.WeekRatiosJson, entity.DayRatiosJson, entity.ShiftRatiosJson, entity.IsMonthLocked, entity.UpdatedAt));
    }

    [HttpGet("monthly-dashboard")]
    public async Task<ActionResult<MonthlyDashboardDto>> GetMonthlyDashboard(Guid storeId, [FromQuery] string yearMonth, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!TryParseYearMonth(yearMonth, out var ym))
            return BadRequest("Tham số tháng phải có định dạng yyyy-MM.");

        var last = new DateOnly(ym.Year, ym.Month, DateTime.DaysInMonth(ym.Year, ym.Month));
        var cfg = await db.StoreMonthlyKpiConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.YearMonth == ym, cancellationToken);
        var target = cfg?.MonthlyTargetAmount ?? 0m;

        var revenueStaff = await (
            from e in db.StaffDailyEntries
            join s in db.StoreStaff on e.StoreStaffId equals s.Id
            where s.StoreId == storeId && e.WorkDate >= ym && e.WorkDate <= last
            select e.RevenueMorning + e.RevenueAfternoon + e.RevenueEvening
        ).SumAsync(cancellationToken);

        var tongDoanhThuHeThongThang = await db.StoreDailySummaries.AsNoTracking()
            .Where(x => x.StoreId == storeId && x.WorkDate >= ym && x.WorkDate <= last)
            .Select(x => x.TongDoanhThuHeThong)
            .SumAsync(cancellationToken);

        var kpiPct = target > 0 ? revenueStaff / target * 100m : 0m;
        var denom = Math.Max(tongDoanhThuHeThongThang, 1m);
        var discrepancy = Math.Abs(revenueStaff - tongDoanhThuHeThongThang) / denom > 0.05m;

        return Ok(new MonthlyDashboardDto(yearMonth, target, revenueStaff, tongDoanhThuHeThongThang, kpiPct, discrepancy, cfg?.IsMonthLocked ?? false));
    }

    [HttpGet("payroll")]
    public async Task<ActionResult<IReadOnlyList<PayrollRowDto>>> GetPayroll(Guid storeId, [FromQuery] string yearMonth, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!TryParseYearMonth(yearMonth, out var ym))
            return BadRequest("Tham số tháng phải có định dạng yyyy-MM.");

        var rows = await BuildPayrollAsync(storeId, ym, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("payroll-export")]
    public async Task<IActionResult> ExportPayroll(Guid storeId, [FromQuery] string yearMonth, CancellationToken cancellationToken)
    {
        if (!await StoreAccess.CanAccessStoreAsync(db, User, storeId, cancellationToken))
            return Forbid();
        if (!TryParseYearMonth(yearMonth, out var ym))
            return BadRequest("Tham số tháng phải có định dạng yyyy-MM.");

        var rows = await BuildPayrollAsync(storeId, ym, cancellationToken);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Bảng lương");
        var headers = new[]
        {
            "Họ tên", "Mã NV", "Chức danh", "HĐ", "Lương/giờ",
            "GC Sáng", "GC Chiều", "GC Tối", "GC BS", "Tổng GC",
            "DT Sáng", "DT Chiều", "DT Tối", "Tổng DT",
            "% HH", "Lương DT", "Lương cứng", "Thưởng team", "Tổng lương"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.FullName;
            ws.Cell(r, 2).Value = x.StaffCode;
            ws.Cell(r, 3).Value = x.PositionCode;
            ws.Cell(r, 4).Value = x.ContractType;
            ws.Cell(r, 5).Value = x.HourlyRate;
            ws.Cell(r, 6).Value = x.HoursMorning;
            ws.Cell(r, 7).Value = x.HoursAfternoon;
            ws.Cell(r, 8).Value = x.HoursEvening;
            ws.Cell(r, 9).Value = x.HoursExtra;
            ws.Cell(r, 10).Value = x.TotalHours;
            ws.Cell(r, 11).Value = x.RevenueMorning;
            ws.Cell(r, 12).Value = x.RevenueAfternoon;
            ws.Cell(r, 13).Value = x.RevenueEvening;
            ws.Cell(r, 14).Value = x.TotalRevenue;
            ws.Cell(r, 15).Value = x.CommissionPctApplied;
            ws.Cell(r, 16).Value = x.SalaryRevenue;
            ws.Cell(r, 17).Value = x.SalaryFixed;
            ws.Cell(r, 18).Value = x.TeamBonus;
            ws.Cell(r, 19).Value = x.TotalSalary;
            r++;
        }

        ws.Row(1).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);
        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"payroll-{storeId}-{yearMonth}.xlsx");
    }

    private async Task<List<PayrollRowDto>> BuildPayrollAsync(Guid storeId, DateOnly ym, CancellationToken cancellationToken)
    {
        var last = new DateOnly(ym.Year, ym.Month, DateTime.DaysInMonth(ym.Year, ym.Month));
        var cfg = await db.StoreMonthlyKpiConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.YearMonth == ym, cancellationToken);
        var target = cfg?.MonthlyTargetAmount ?? 0m;

        var revenueStaffMonth = await (
            from e in db.StaffDailyEntries
            join s in db.StoreStaff on e.StoreStaffId equals s.Id
            where s.StoreId == storeId && e.WorkDate >= ym && e.WorkDate <= last
            select e.RevenueMorning + e.RevenueAfternoon + e.RevenueEvening
        ).SumAsync(cancellationToken);

        var storeKpiPct = target > 0 ? revenueStaffMonth / target * 100m : 0m;
        var refDay = last;

        var staffQ = db.StoreStaff.AsNoTracking().Where(s => s.StoreId == storeId);
        if (IsSalesOnlySelf())
        {
            var uid = GetUserId();
            staffQ = staffQ.Where(s => s.LinkedUserId == uid);
        }

        var staffList = await staffQ.ToListAsync(cancellationToken);
        var bracketsDb = await db.CommissionBrackets.AsNoTracking()
            .Where(b => b.EffectiveFrom <= refDay && (b.EffectiveTo == null || b.EffectiveTo >= ym))
            .ToListAsync(cancellationToken);

        var list = new List<PayrollRowDto>();
        foreach (var s in staffList.OrderBy(x => x.StaffCode))
        {
            var agg = await db.StaffDailyEntries
                .Where(e => e.StoreStaffId == s.Id && e.WorkDate >= ym && e.WorkDate <= last)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Hm = g.Sum(e => e.HoursMorning),
                    Ha = g.Sum(e => e.HoursAfternoon),
                    He = g.Sum(e => e.HoursEvening),
                    Hx = g.Sum(e => e.HoursExtra),
                    Rm = g.Sum(e => e.RevenueMorning),
                    Ra = g.Sum(e => e.RevenueAfternoon),
                    Re = g.Sum(e => e.RevenueEvening)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var hm = agg?.Hm ?? 0;
            var ha = agg?.Ha ?? 0;
            var he = agg?.He ?? 0;
            var hx = agg?.Hx ?? 0;
            var rm = agg?.Rm ?? 0;
            var ra = agg?.Ra ?? 0;
            var re = agg?.Re ?? 0;
            var totalH = hm + ha + he + hx;
            var totalR = rm + ra + re;

            var isSales = ShiftKpiMath.IsSalesPosition(s.PositionCode);
            var tiers = bracketsDb
                .Where(b => b.PositionCode == s.PositionCode && b.ContractType == s.ContractType)
                .Select(b => (b.KpiPctMin, b.KpiPctMax, b.CommissionPct))
                .ToList();
            var pct = ShiftKpiMath.PickCommissionPct(tiers, storeKpiPct);
            var pct95 = ShiftKpiMath.PickCommissionPct(tiers, 95m);

            var (baseSalary, commission, teamBonus, total) = ShiftKpiMath.MonthlySalary(
                s.HourlyRate,
                totalH,
                totalR,
                pct,
                isSales,
                s.PositionCode == "QLCH",
                s.TeamBonusBase,
                storeKpiPct);

            var hypo = isSales ? totalR * pct95 / 100m : (decimal?)null;

            list.Add(new PayrollRowDto(
                s.Id,
                s.StaffCode,
                s.FullName,
                s.PositionCode,
                s.ContractType,
                s.HourlyRate,
                hm, ha, he, hx, totalH,
                rm, ra, re, totalR,
                pct,
                commission,
                baseSalary,
                teamBonus,
                total,
                hypo));
        }

        return list;
    }

    private async Task<DailyEntryRowDto> BuildRowDtoAsync(Guid storeId, StaffDailyEntry entry, CancellationToken cancellationToken)
    {
        var summary = await db.StoreDailySummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.WorkDate == entry.WorkDate, cancellationToken);
        var kpiDay = await ResolveStoreDayKpiTargetAsync(storeId, entry.WorkDate, summary, cancellationToken);

        var staffList = await db.StoreStaff.AsNoTracking().Where(s => s.StoreId == storeId).ToListAsync(cancellationToken);
        var staffIds = staffList.Select(s => s.Id).ToList();
        var entryList = await db.StaffDailyEntries
            .AsNoTracking()
            .Where(e => e.WorkDate == entry.WorkDate && staffIds.Contains(e.StoreStaffId))
            .ToListAsync(cancellationToken);
        var entries = entryList
            .GroupBy(e => e.StoreStaffId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

        var totalW = staffList.Sum(s =>
        {
            var e = entries.GetValueOrDefault(s.Id);
            return ShiftKpiMath.WeightNv(e?.HoursMorning ?? 0, e?.HoursAfternoon ?? 0, e?.HoursEvening ?? 0, e?.HoursExtra ?? 0, ShiftKpiMath.IsSalesPosition(s.PositionCode));
        });

        var s = await db.StoreStaff.AsNoTracking().FirstAsync(x => x.Id == entry.StoreStaffId, cancellationToken);
        var w = ShiftKpiMath.WeightNv(entry.HoursMorning, entry.HoursAfternoon, entry.HoursEvening, entry.HoursExtra, ShiftKpiMath.IsSalesPosition(s.PositionCode));
        var rev = entry.RevenueMorning + entry.RevenueAfternoon + entry.RevenueEvening;
        var (target, pct) = ShiftKpiMath.DailyPersonalTargets(kpiDay, w, totalW, rev);

        return new DailyEntryRowDto(
            entry.Id,
            s.Id,
            s.StaffCode,
            s.FullName,
            s.PositionCode,
            s.ContractType,
            entry.Version,
            entry.HoursMorning,
            entry.HoursAfternoon,
            entry.HoursEvening,
            entry.HoursExtra,
            entry.RevenueMorning,
            entry.RevenueAfternoon,
            entry.RevenueEvening,
            entry.Customers,
            entry.TryOns,
            entry.Orders,
            entry.Products,
            w,
            target,
            rev,
            pct);
    }

    private async Task EnsureDailyEntriesAsync(Guid storeId, DateOnly workDate, CancellationToken cancellationToken)
    {
        var staffIds = await db.StoreStaff.Where(s => s.StoreId == storeId).Select(s => s.Id).ToListAsync(cancellationToken);
        foreach (var sid in staffIds)
        {
            if (await db.StaffDailyEntries.AnyAsync(e => e.StoreStaffId == sid && e.WorkDate == workDate, cancellationToken))
                continue;

            db.StaffDailyEntries.Add(new StaffDailyEntry
            {
                Id = Guid.NewGuid(),
                StoreStaffId = sid,
                WorkDate = workDate,
                Version = 1
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsPostgresUniqueViolation(ex))
            {
                db.ChangeTracker.Clear();
            }
        }

        if (await db.StoreDailySummaries.AnyAsync(s => s.StoreId == storeId && s.WorkDate == workDate, cancellationToken))
            return;

        db.StoreDailySummaries.Add(new StoreDailySummary
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            WorkDate = workDate,
            StoreDayKpiTarget = 0
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPostgresUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
        }
    }

    private static bool IsPostgresUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg && pg.SqlState == "23505")
                return true;
        }

        return false;
    }

    private static string? ValidateStaffWrite(StoreStaffWriteDto body)
    {
        if (string.IsNullOrWhiteSpace(body.StaffCode) || string.IsNullOrWhiteSpace(body.FullName))
            return "Mã NV và họ tên bắt buộc.";
        if (body.HourlyRate < 0 || body.TeamBonusBase < 0)
            return "Lương/giờ và thưởng team phải ≥ 0.";
        return null;
    }

    private static bool HasLoginCredentials(string? email, string? password) =>
        !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password);

    private static string? ValidateLoginCredentials(string? email, string? password, bool required)
    {
        if (!required)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
                return null;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return "Email và mật khẩu đăng nhập là bắt buộc.";
        var e = email.Trim();
        if (e.Length < 5 || !e.Contains('@', StringComparison.Ordinal))
            return "Email không hợp lệ.";
        if (password.Length < 6)
            return "Mật khẩu tối thiểu 6 ký tự.";
        return null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string RoleNameForPosition(string positionCode) =>
        positionCode is "QLCH" or "CHP" ? KrikRoles.StoreManager : KrikRoles.SalesStaff;

    private static bool TryParseYearMonth(string s, out DateOnly ym)
    {
        ym = default;
        var p = s.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length != 2) return false;
        if (!int.TryParse(p[0], out var y) || !int.TryParse(p[1], out var m)) return false;
        if (m is < 1 or > 12) return false;
        ym = new DateOnly(y, m, 1);
        return true;
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }

    private bool IsSalesOnlySelf()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        if (roles.Contains(KrikRoles.AdminHR) || roles.Contains(KrikRoles.AreaManager) || roles.Contains(KrikRoles.StoreManager))
            return false;
        return roles.Contains(KrikRoles.SalesStaff);
    }
}
