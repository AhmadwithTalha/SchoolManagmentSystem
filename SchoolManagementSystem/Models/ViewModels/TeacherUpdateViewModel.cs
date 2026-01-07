using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
namespace SchoolManagementSystem.Models
{
    public class TeacherUpdateViewModel
    {
        public string Id { get; set; }

        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        public int CountryId { get; set; }

        [Required]
        public int CityId { get; set; }

        public string Address { get; set; }

        public IFormFile? ProfileImageFile { get; set; }
        public string? ProfileImageBase64 { get; set; }
        public string? ExistingProfileImage { get; set; }

        // ✅ DROPDOWNS
        public List<SelectListItem> Countries { get; set; } = new();
        public List<SelectListItem> Cities { get; set; } = new();
    }
}
