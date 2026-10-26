using System.ComponentModel.DataAnnotations;
using OCRS.Models.Enums;

namespace OCRS.ViewModels.Admin;

public sealed class ClassFormViewModel : IValidatableObject
{
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã lớp."), StringLength(20)]
    [Display(Name = "Mã lớp")]
    public string ClassCode { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên lớp."), StringLength(160)]
    [Display(Name = "Tên lớp")]
    public string ClassName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khóa học.")]
    [Display(Name = "Khóa học")]
    public int CourseId { get; set; }

    [Display(Name = "Giảng viên")]
    public int? InstructorId { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Mở đăng ký")]
    public DateTime RegistrationOpenAt { get; set; }

    [Display(Name = "Đóng đăng ký")]
    public DateTime RegistrationCloseAt { get; set; }

    [Range(1, 1000, ErrorMessage = "Sĩ số tối đa phải từ 1 đến 1000.")]
    [Display(Name = "Sĩ số tối đa")]
    public int MaxCapacity { get; set; } = 30;

    [Display(Name = "Hình thức")]
    public LearningMode LearningMode { get; set; }

    [StringLength(250), Display(Name = "Địa điểm")]
    public string? Location { get; set; }

    [Url(ErrorMessage = "Liên kết lớp học không hợp lệ."), StringLength(500)]
    [Display(Name = "Liên kết lớp học")]
    public string? MeetingLink { get; set; }

    [Display(Name = "Trạng thái")]
    public ClassStatus Status { get; set; } = ClassStatus.Draft;

    [Display(Name = "Cần quản trị viên duyệt")]
    public bool RequiresApproval { get; set; }

    public string? RowVersion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
            yield return new ValidationResult("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.", [nameof(EndDate)]);
        if (RegistrationCloseAt <= RegistrationOpenAt)
            yield return new ValidationResult("Thời gian đóng phải sau thời gian mở đăng ký.", [nameof(RegistrationCloseAt)]);
        if (LearningMode is LearningMode.Online or LearningMode.Hybrid && string.IsNullOrWhiteSpace(MeetingLink))
            yield return new ValidationResult("Vui lòng nhập liên kết cho lớp trực tuyến hoặc kết hợp.", [nameof(MeetingLink)]);
        if (LearningMode is LearningMode.Offline or LearningMode.Hybrid && string.IsNullOrWhiteSpace(Location))
            yield return new ValidationResult("Vui lòng nhập địa điểm cho lớp trực tiếp hoặc kết hợp.", [nameof(Location)]);
    }
}
