using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.DTOs.SOA;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Services;

public interface ICourseService
{
    Task<IReadOnlyList<CourseDto>> GetPublishedAsync(CourseQueryDto query, CancellationToken ct = default);
    Task<CourseDto?> GetPublishedByIdAsync(int id, CancellationToken ct = default);
    Task<(IReadOnlyList<Course> Items, int TotalPages)> GetPublishedPageAsync(string? keyword, int? categoryId, LearningMode? learningMode, int page, int pageSize, CancellationToken ct = default);
    Task<Course?> GetPublishedDetailsAsync(int id, CancellationToken ct = default);
}

public sealed class CourseService(ApplicationDbContext db) : ICourseService
{
    private IQueryable<Course> Published() => db.Courses.AsNoTracking()
        .Where(x => x.Status == CourseStatus.Published);

    private static IQueryable<Course> Filter(IQueryable<Course> query, string? keyword, int? categoryId, LearningMode? learningMode)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(x => x.CourseName.Contains(term) || x.CourseCode.Contains(term));
        }
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (learningMode.HasValue) query = query.Where(x => x.LearningMode == learningMode.Value);
        return query;
    }

    public async Task<IReadOnlyList<CourseDto>> GetPublishedAsync(CourseQueryDto query, CancellationToken ct = default) =>
        await Filter(Published(), query.Keyword, query.CategoryId, query.LearningMode)
            .OrderBy(x => x.CourseName)
            .Select(x => new CourseDto(x.CourseId, x.CourseCode, x.CourseName, x.CategoryId,
                x.Category.CategoryName, x.Description, x.Duration, x.TuitionFee, x.ImageUrl,
                x.LearningMode, x.Status))
            .ToListAsync(ct);

    public Task<CourseDto?> GetPublishedByIdAsync(int id, CancellationToken ct = default) =>
        Published().Where(x => x.CourseId == id)
            .Select(x => new CourseDto(x.CourseId, x.CourseCode, x.CourseName, x.CategoryId,
                x.Category.CategoryName, x.Description, x.Duration, x.TuitionFee, x.ImageUrl,
                x.LearningMode, x.Status))
            .SingleOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<Course> Items, int TotalPages)> GetPublishedPageAsync(
        string? keyword, int? categoryId, LearningMode? learningMode, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var query = Filter(Published().Include(x => x.Category), keyword, categoryId, learningMode);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CourseName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, (int)Math.Ceiling(count / (double)pageSize));
    }

    public Task<Course?> GetPublishedDetailsAsync(int id, CancellationToken ct = default) =>
        Published().AsSplitQuery().Include(x => x.Category)
            .Include(x => x.Classes)
                .ThenInclude(x => x.Instructor).ThenInclude(x => x!.User)
            .Include(x => x.Classes)
                .ThenInclude(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.CourseId == id, ct);
}
