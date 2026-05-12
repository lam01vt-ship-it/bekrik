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

    /// <summary>Chỉ Admin được sửa cấu hình KPI tháng (đề 4.1).</summary>
    public static bool CanEditKpiMonthConfig(ClaimsPrincipal user) => IsAdmin(user);

    /// <summary>QLCH+ có thể sửa roster; Sales chỉ self (check tại controller qua LinkedUserId).</summary>
    public static bool CanEditStaffMaster(ClaimsPrincipal user) => IsAdminOrAreaOrStore(user);
}
