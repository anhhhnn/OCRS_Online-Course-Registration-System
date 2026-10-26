using Microsoft.AspNetCore.Mvc;
using OCRS.DTOs.SOA;
using OCRS.Models.Enums;
using OCRS.Services;

namespace OCRS.Controllers.Api;

[ApiController]
[Route("api/soa/courses")]
public sealed class SoaCoursesController(ICourseService courses) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> GetAll([FromQuery] int? categoryId, [FromQuery] LearningMode? learningMode, CancellationToken ct) =>
        Ok(await courses.GetPublishedAsync(new(null, categoryId, learningMode), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CourseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> Get(int id, CancellationToken ct)
    {
        var item = await courses.GetPublishedByIdAsync(id, ct);
        return item is null ? NotFound(new { message = "Không tìm thấy khóa học công khai." }) : Ok(item);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> Search([FromQuery] string keyword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return BadRequest(new { message = "Từ khóa tìm kiếm không được để trống." });
        return Ok(await courses.GetPublishedAsync(new(keyword), ct));
    }
}
