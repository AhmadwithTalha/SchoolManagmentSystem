namespace SchoolManagementSystem.Models
{
    public class TeacherUpdateViewModel
    {
        public string Id { get; set; } // ✅ Add this
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } // display only
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }

        public IFormFile ProfileImageFile { get; set; } // optional
        public string ProfileImageBase64 { get; set; }  // optional
        public string? ExistingProfileImage { get; set; } // optional, just display
    }
}
