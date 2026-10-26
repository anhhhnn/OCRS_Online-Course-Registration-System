using Microsoft.AspNetCore.Mvc;
using OCRS.DTOs.SOA;
using OCRS.Services;

namespace OCRS.Controllers.Api;

[ApiController]
[Route("api/soa")]
public sealed class SoaClassesController(IClassService classes) : ControllerBase
{
    [HttpGet("courses/{courseId:int}/classes")]
    public async Task<ActionResult<IReadOnlyList<ClassDto>>> ForCourse(int courseId, CancellationToken ct) => Ok(await classes.GetForCourseAsync(courseId, ct));

    [HttpGet("classes/{id:int}")]
    public async Task<ActionResult<ClassDto>> Get(int id, CancellationToken ct)
    {
        var item = await classes.GetAsync(id, ct);
        return item is null ? NotFound(new { message = "Không tìm thấy lớp học." }) : Ok(item);
    }

    [HttpGet("classes/{id:int}/schedule")]
    public async Task<ActionResult<IReadOnlyList<ClassScheduleDto>>> Schedule(int id, CancellationToken ct)
    {
        var items = await classes.GetScheduleAsync(id, ct);
        return items is null ? NotFound(new { message = "Không tìm thấy lớp học." }) : Ok(items);
    }

    [HttpGet("classes/{id:int}/availability")]
    public async Task<ActionResult<ClassAvailabilityDto>> Availability(int id, CancellationToken ct)
    {
        var item = await classes.GetAvailabilityAsync(id, ct);
        return item is null ? NotFound(new { message = "Không tìm thấy lớp học." }) : Ok(item);
    }
}
