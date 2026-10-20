using OCRS.Models;
namespace OCRS.ViewModels.Dashboard;
public class DashboardViewModel { public int Students{get;set;} public int Instructors{get;set;} public int Courses{get;set;} public int Classes{get;set;} public int Enrollments{get;set;} public int OpenClasses{get;set;} public int FullClasses{get;set;} public double FillRate{get;set;} public List<Enrollment> RecentEnrollments{get;set;}=[]; public List<string> MonthLabels{get;set;}=[]; public List<int> MonthValues{get;set;}=[]; }
