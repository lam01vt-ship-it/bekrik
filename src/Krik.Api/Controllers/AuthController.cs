using System.Security.Claims;
using Krik.Api.Contracts;
using Krik.Api.Data;
using Krik.Api.Options;
using Krik.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Krik.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(AppDbContext db, IJwtTokenService jwt, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken cancellationToken)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserAreas)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Unauthorized();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        var areaIds = user.UserAreas.Select(ua => ua.AreaId).Distinct().OrderBy(x => x).ToList();
        var token = jwt.CreateAccessToken(user, roles, areaIds);
        var expiresMinutes = jwtOptions.Value.ExpiresMinutes;

        return Ok(new LoginResponse(
            token,
            expiresMinutes * 60,
            new UserSummaryDto(user.Id, user.Email, user.FullName, roles, user.StoreId, areaIds)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserAreas)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Unauthorized();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(x => x).ToList();
        var areaIds = user.UserAreas.Select(ua => ua.AreaId).Distinct().OrderBy(x => x).ToList();
        return Ok(new UserSummaryDto(user.Id, user.Email, user.FullName, roles, user.StoreId, areaIds));
    }
}
