using Krik.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krik.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = KrikRoles.AdminHR)]
public sealed class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "admin-only" });
}
