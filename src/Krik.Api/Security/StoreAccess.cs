using System.Security.Claims;
using Krik.Api.Data;
using Krik.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Security;

public static class StoreAccess
{
    public static async Task<bool> CanAccessStoreAsync(
        AppDbContext db,
        ClaimsPrincipal user,
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return false;

        if (roles.Contains(KrikRoles.AdminHR))
            return await db.Stores.AsNoTracking().AnyAsync(s => s.Id == storeId, cancellationToken);

        if (roles.Contains(KrikRoles.AreaManager))
        {
            var areaIds = user.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            return await db.Stores.AsNoTracking().AnyAsync(s => s.Id == storeId && areaIds.Contains(s.AreaId), cancellationToken);
        }

        if (roles.Contains(KrikRoles.StoreManager) || roles.Contains(KrikRoles.SalesStaff))
        {
            var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            return u?.StoreId == storeId;
        }

        return false;
    }

    public static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole(KrikRoles.AdminHR);

    public static bool IsAdminOrAreaOrStore(ClaimsPrincipal user) =>
        user.IsInRole(KrikRoles.AdminHR) ||
        user.IsInRole(KrikRoles.AreaManager) ||
        user.IsInRole(KrikRoles.StoreManager);

    /// <summary>
    /// AreaManager đề xuất + cập nhật cấu hình KPI tháng cho cửa hàng trong khu vực.
    /// StoreManager (QLCH) chỉ sửa được ngày mùng 1 của chính tháng cấu hình, theo giờ local của server.
    /// AdminHR (nhân sự) không trực tiếp sửa — chỉ <see cref="CanAcceptKpiMonth"/> (accept + khoá tháng).
    /// </summary>
    public static bool CanEditKpiMonthConfig(ClaimsPrincipal user, DateOnly yearMonth, DateOnly serverToday)
    {
        if (user.IsInRole(KrikRoles.AreaManager))
            return true;

        if (!user.IsInRole(KrikRoles.StoreManager))
            return false;

        return serverToday.Day == 1 &&
            yearMonth.Year == serverToday.Year &&
            yearMonth.Month == serverToday.Month;
    }

    /// <summary>HR workflow: chỉ AdminHR mới accept (chốt) và khoá / mở khoá tháng KPI.</summary>
    public static bool CanAcceptKpiMonth(ClaimsPrincipal user) => IsAdmin(user);

    public static bool CanEditStaffMaster(ClaimsPrincipal user) => IsAdminOrAreaOrStore(user);

    public static bool CanManageStores(ClaimsPrincipal user) =>
        user.IsInRole(KrikRoles.AdminHR) || user.IsInRole(KrikRoles.AreaManager);

    public static bool CanListAreas(ClaimsPrincipal user) =>
        user.IsInRole(KrikRoles.AdminHR) || user.IsInRole(KrikRoles.AreaManager);

    /// <summary>Ngày làm việc &lt; hôm nay: chỉ QL khu vực / QL cửa hàng / Admin HR được sửa bảng công.</summary>
    public static bool CanEditPastShiftDailyWorkDate(ClaimsPrincipal user) =>
        user.IsInRole(KrikRoles.AdminHR) ||
        user.IsInRole(KrikRoles.AreaManager) ||
        user.IsInRole(KrikRoles.StoreManager);
}
