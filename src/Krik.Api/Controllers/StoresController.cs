using System.Security.Claims;
using ClosedXML.Excel;
using Krik.Api.Contracts;
using Krik.Api.Data;
using Krik.Api.Entities;
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

    [HttpPost]
    public async Task<ActionResult<StoreDto>> CreateStore([FromBody] StoreCreateDto body, CancellationToken cancellationToken)
    {
        if (!StoreAccess.CanManageStores(User))
            return Forbid();

        var code = body.Code.Trim();
        var name = body.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return BadRequest("Mã và tên cửa hàng không được để trống.");

        if (!await db.Areas.AsNoTracking().AnyAsync(a => a.Id == body.AreaId, cancellationToken))
            return BadRequest("Khu vực không tồn tại.");

        if (User.IsInRole(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            if (!areaIds.Contains(body.AreaId))
                return Forbid();
        }

        var entity = new Store
        {
            Id = Guid.NewGuid(),
            AreaId = body.AreaId,
            Code = code.ToUpperInvariant(),
            Name = name
        };
        db.Stores.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Mã cửa hàng đã tồn tại.");
        }

        var area = await db.Areas.AsNoTracking().FirstAsync(a => a.Id == entity.AreaId, cancellationToken);
        return Ok(new StoreDto(entity.Id, entity.Code, entity.Name, entity.AreaId, area.Code, area.Name));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StoreDto>> UpdateStore(Guid id, [FromBody] StoreUpdateDto body, CancellationToken cancellationToken)
    {
        if (!StoreAccess.CanManageStores(User))
            return Forbid();

        var name = body.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Tên cửa hàng không được để trống.");

        if (!await db.Areas.AsNoTracking().AnyAsync(a => a.Id == body.AreaId, cancellationToken))
            return BadRequest("Khu vực không tồn tại.");

        if (User.IsInRole(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            if (!areaIds.Contains(body.AreaId))
                return Forbid();
        }

        var entity = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            return NotFound();

        if (User.IsInRole(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            if (!areaIds.Contains(entity.AreaId))
                return Forbid();
        }

        entity.Name = name;
        entity.AreaId = body.AreaId;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Không cập nhật được cửa hàng.");
        }

        var area = await db.Areas.AsNoTracking().FirstAsync(a => a.Id == entity.AreaId, cancellationToken);
        return Ok(new StoreDto(entity.Id, entity.Code, entity.Name, entity.AreaId, area.Code, area.Name));
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] StoreBatchDeleteDto body, CancellationToken cancellationToken)
    {
        if (!StoreAccess.CanManageStores(User))
            return Forbid();

        if (body.StoreIds.Count == 0)
            return BadRequest("Chọn ít nhất một cửa hàng.");

        var ids = body.StoreIds.Distinct().ToList();
        var stores = await db.Stores.AsNoTracking().Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
        if (stores.Count != ids.Count)
            return BadRequest("Có mã cửa hàng không tồn tại.");

        if (User.IsInRole(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            if (stores.Any(s => !areaIds.Contains(s.AreaId)))
                return Forbid();
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Users
                .Where(u => u.StoreId != null && ids.Contains(u.StoreId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.StoreId, (Guid?)null), cancellationToken);

            await db.Stores.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return Conflict("Không xoá được (còn dữ liệu tham chiếu).");
        }

        return NoContent();
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        IQueryable<Store> q = db.Stores.AsNoTracking().Include(s => s.Area).OrderBy(s => s.Code);

        if (roles.Contains(KrikRoles.AdminHR))
        {
        }
        else if (roles.Contains(KrikRoles.AreaManager))
        {
            var areaIds = User.FindAll("area_id").Select(c => Guid.Parse(c.Value)).ToHashSet();
            q = q.Where(s => areaIds.Contains(s.AreaId));
        }
        else if (roles.Contains(KrikRoles.StoreManager) || roles.Contains(KrikRoles.SalesStaff))
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user?.StoreId is not { } sid)
                q = q.Where(s => false);
            else
                q = q.Where(s => s.Id == sid);
        }
        else
            return Forbid();

        var rows = await q.Select(s => new { s.Code, s.Name, AreaCode = s.Area.Code, AreaName = s.Area.Name }).ToListAsync(cancellationToken);

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Cửa hàng");
        var headers = new[] { "Mã cửa hàng", "Tên cửa hàng", "Mã khu", "Tên khu" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.Code;
            ws.Cell(r, 2).Value = x.Name;
            ws.Cell(r, 3).Value = x.AreaCode;
            ws.Cell(r, 4).Value = x.AreaName;
            r++;
        }

        ws.Row(1).Style.Font.Bold = true;
        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cua-hang.xlsx");
    }
}
