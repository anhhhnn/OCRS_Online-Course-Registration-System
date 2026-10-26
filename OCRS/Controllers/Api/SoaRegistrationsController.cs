using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCRS.DTOs.SOA;
using OCRS.Services;

namespace OCRS.Controllers.Api;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/soa/registrations")]
public sealed class SoaRegistrationsController(IRegistrationService registrations) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<RegistrationResultDto>> Register(RegistrationRequestDto request, CancellationToken ct)
    {
        var result = await registrations.RegisterAsync(request.ClassId, UserId, ct);
        var dto = Map(result);
        if (result.Succeeded) return CreatedAtAction(nameof(MyStatus), new { classId = request.ClassId }, dto);
        return result.ErrorCode switch
        {
            "class_not_found" => NotFound(dto),
            "duplicate_registration" or "class_unavailable" or "registration_conflict" => Conflict(dto),
            _ => BadRequest(dto)
        };
    }

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> Mine(CancellationToken ct) => Ok(await registrations.GetMineAsync(UserId, ct));

    [HttpGet("classes/{classId:int}/my-status")]
    public async Task<ActionResult<MyRegistrationStatusDto>> MyStatus(int classId, CancellationToken ct) => Ok(await registrations.GetMyStatusAsync(classId, UserId, ct));

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<RegistrationResultDto>> Cancel(int id, CancellationToken ct)
    {
        var result = await registrations.CancelAsync(id, UserId, false, ct);
        var dto = Map(result);
        if (result.Succeeded) return Ok(dto);
        return result.ErrorCode switch
        {
            "enrollment_not_found" => NotFound(dto),
            "forbidden" => Forbid(),
            "already_inactive" => Conflict(dto),
            _ => BadRequest(dto)
        };
    }

    private static RegistrationResultDto Map(RegistrationResult result) => new(result.Succeeded, result.Status, result.Message, result.EnrollmentId, result.ErrorCode);
}
