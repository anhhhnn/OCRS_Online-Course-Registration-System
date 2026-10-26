using System.ComponentModel.DataAnnotations;
using OCRS.Models.Enums;
using OCRS.ViewModels.Admin;

namespace OCRS.Tests;

public sealed class ClassFormViewModelTests
{
    private static List<ValidationResult> Validate(ClassFormViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    private static ClassFormViewModel Valid() => new()
    {
        ClassCode="TEST-01",ClassName="Lớp kiểm thử",CourseId=1,StartDate=DateTime.Today.AddDays(10),EndDate=DateTime.Today.AddDays(30),
        RegistrationOpenAt=DateTime.Now.AddDays(-1),RegistrationCloseAt=DateTime.Now.AddDays(5),MaxCapacity=30,LearningMode=LearningMode.Offline,Location="Phòng A"
    };

    [Fact] public void Valid_model_has_no_errors() => Assert.Empty(Validate(Valid()));
    [Fact] public void Zero_capacity_is_rejected() { var model=Valid();model.MaxCapacity=0;Assert.Contains(Validate(model),x=>x.MemberNames.Contains(nameof(model.MaxCapacity))); }
    [Fact] public void End_before_start_is_rejected() { var model=Valid();model.EndDate=model.StartDate.AddDays(-1);Assert.Contains(Validate(model),x=>x.MemberNames.Contains(nameof(model.EndDate))); }
    [Fact] public void Online_class_requires_meeting_link() { var model=Valid();model.LearningMode=LearningMode.Online;model.Location=null;Assert.Contains(Validate(model),x=>x.MemberNames.Contains(nameof(model.MeetingLink))); }
}
