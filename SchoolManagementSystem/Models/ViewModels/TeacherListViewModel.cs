namespace SchoolManagementSystem.Models.ViewModels
{
    public class TeacherListViewModel
    {
        public List<ApplicationUser> Teachers { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
