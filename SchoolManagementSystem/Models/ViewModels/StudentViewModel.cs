using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolManagementSystem.Helpers;
using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models.ViewModels
{
    public class StudentViewModel
    {
        public string? Id { get; set; } // null -> create, has value -> edit

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [RequiredIfCreating] // required only on create
        [DataType(DataType.Password)]
        public string? Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "City is required")]
        public int CityId { get; set; }

        public string? Address { get; set; }

        public IFormFile ProfileImageFile { get; set; }
        public string? ProfileImageBase64 { get; set; }
        public string? ExistingProfileImage { get; set; }

        public List<SelectListItem> Countries { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Cities { get; set; } = new List<SelectListItem>();
    }
}
