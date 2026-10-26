using OCRS.Models.Enums;

namespace OCRS.DTOs.SOA;

public sealed record ClassDto(
    int ClassId,
    string ClassCode,
    string ClassName,
    int CourseId,
    string CourseName,
    string? InstructorName,
    DateTime StartDate,
    DateTime EndDate,
    DateTime RegistrationOpenAt,
    DateTime RegistrationCloseAt,
    int MaxCapacity,
    int CurrentEnrollment,
    int RemainingCapacity,
    LearningMode LearningMode,
    string? Location,
    ClassStatus Status,
    string RegistrationStatus,
    bool RequiresApproval);

public sealed record ClassScheduleDto(
    int ScheduleId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? RoomOrLink,
    string? Note);

public sealed record ClassAvailabilityDto(
    int ClassId,
    int MaxCapacity,
    int CurrentEnrollment,
    int RemainingCapacity,
    string RegistrationStatus);
