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
public sealed class AreasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AreaListItemDto>>> List(CancellationToken cancellationToken)
    {
        if (!StoreAccess.CanListAreas(User))
            return Forbid();

        if (StoreAccess.IsAdmin(User))
        {
            var all = await db.Areas.AsNoTracking()
                .OrderBy(a => a.Code)
                .Select(a => new AreaListItemDto(a.Id, a.Code, a.Name))
                .ToListAsync(cancellationToken);
            return Ok(all);
        }

        var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
        var list = await db.Areas.AsNoTracking()
            .Where(a => areaIds.Contains(a.Id))
            .OrderBy(a => a.Code)
            .Select(a => new AreaListItemDto(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);
        return Ok(list);
    }
}
