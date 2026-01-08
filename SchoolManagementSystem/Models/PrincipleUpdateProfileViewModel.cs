using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManagementSystem.Models
{
    public class PrincipleUpdateProfileViewModel
    {
        public int? Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } // display only
        public string PhoneNumber { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
        public string Address { get; set; }
        [ValidateNever]
        public List<SelectListItem> Countries { get; set; }
        [ValidateNever]
        public List<SelectListItem> Cities { get; set; }
        public IFormFile ProfileImageFile { get; set; } 
        public string? ProfileImageBase64 { get; set; }  
        public string? ExistingProfileImage { get; set; } 
    }
}
