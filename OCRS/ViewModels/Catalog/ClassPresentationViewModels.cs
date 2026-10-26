using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.ViewModels.Catalog;

public sealed record EnrollmentSnapshot(int EnrollmentId, EnrollmentStatus Status);

public sealed class ClassCardViewModel
{
    public required CourseClass Class { get; init; }
    public EnrollmentSnapshot? Enrollment { get; init; }
    public int Remaining => Math.Max(0, Class.MaxCapacity - Class.CurrentEnrollment);
    public int FilledPercent => Math.Clamp(Class.CurrentEnrollment * 100 / Math.Max(1, Class.MaxCapacity), 0, 100);
}

public sealed class CourseDetailsViewModel
{
    public required Course Course { get; init; }
    public required IReadOnlyList<ClassCardViewModel> Classes { get; init; }
}

public sealed class ClassDetailsViewModel
{
    public required CourseClass Class { get; init; }
    public EnrollmentSnapshot? Enrollment { get; init; }
    public required IReadOnlyList<CourseClass> OtherClasses { get; init; }
    public int Remaining => Math.Max(0, Class.MaxCapacity - Class.CurrentEnrollment);
    public int FilledPercent => Math.Clamp(Class.CurrentEnrollment * 100 / Math.Max(1, Class.MaxCapacity), 0, 100);
}
