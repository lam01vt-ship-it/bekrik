using System.Security.Claims;
using Krik.Api.Contracts;
using Krik.Api.Data;
using Krik.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StoresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StoreDto>>> GetStores(CancellationToken cancellationToken)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        if (roles.Contains(KrikRoles.AdminHR))
        {
            var all = await db.Stores
                .AsNoTracking()
                .Include(s => s.Area)
                .OrderBy(s => s.Code)
                .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.AreaId, s.Area.Code, s.Area.Name))
                .ToListAsync(cancellationToken);
            return Ok(all);
        }

        if (roles.Contains(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            var list = await db.Stores
                .AsNoTracking()
                .Include(s => s.Area)
                .Where(s => areaIds.Contains(s.AreaId))
                .OrderBy(s => s.Code)
                .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.AreaId, s.Area.Code, s.Area.Name))
                .ToListAsync(cancellationToken);
            return Ok(list);
        }

        if (roles.Contains(KrikRoles.StoreManager) || roles.Contains(KrikRoles.SalesStaff))
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user?.StoreId is not { } storeId)
                return Ok(Array.Empty<StoreDto>());

            var one = await db.Stores
                .AsNoTracking()
                .Include(s => s.Area)
                .Where(s => s.Id == storeId)
                .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.AreaId, s.Area.Code, s.Area.Name))
                .ToListAsync(cancellationToken);
            return Ok(one);
        }

        return Forbid();
    }
}
