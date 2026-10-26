using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCRS.DTOs.SOA;
using OCRS.Services;

namespace OCRS.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/soa/users")]
public sealed class SoaUsersController(IUserService users) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await users.GetAsync(UserId);
        return user is null ? NotFound(new { message = "Không tìm thấy người dùng." }) : Ok(user);
    }

    [HttpPut("me")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CurrentUserDto>> Update(UpdateCurrentUserDto input)
    {
        var result = await users.UpdateAsync(UserId, input);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { message = result.Message });
    }
}
