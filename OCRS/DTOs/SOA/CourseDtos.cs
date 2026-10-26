using OCRS.Models.Enums;

namespace OCRS.DTOs.SOA;

public sealed record CourseDto(
    int CourseId,
    string CourseCode,
    string CourseName,
    int CategoryId,
    string CategoryName,
    string? Description,
    int Duration,
    decimal TuitionFee,
    string? ImageUrl,
    LearningMode LearningMode,
    CourseStatus Status);

public sealed record CourseQueryDto(
    string? Keyword = null,
    int? CategoryId = null,
    LearningMode? LearningMode = null);
