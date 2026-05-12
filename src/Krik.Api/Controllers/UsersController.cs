using Krik.Api.Contracts;
using Krik.Api.Data;
using Krik.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Krik.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = KrikRoles.AdminHR)]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var result = users.Select(u => new UserListItemDto(
            u.Id,
            u.Email,
            u.FullName,
            u.StoreId,
            u.UserRoles.Select(ur => ur.Role.Name).OrderBy(x => x).ToList())).ToList();

        return Ok(result);
    }
}
