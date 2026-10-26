using System.ComponentModel.DataAnnotations;

namespace OCRS.DTOs.SOA;

public sealed class RegistrationRequestDto
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }
}

public sealed record RegistrationResultDto(
    bool Success,
    string Status,
    string Message,
    int? EnrollmentId = null,
    string? ErrorCode = null);

public sealed record EnrollmentDto(
    int EnrollmentId,
    int ClassId,
    string ClassCode,
    string ClassName,
    string CourseName,
    string Status,
    DateTime RegisteredAt,
    DateTime? CancelledAt);

public sealed record MyRegistrationStatusDto(
    int ClassId,
    bool IsRegistered,
    int? EnrollmentId,
    string? Status);
