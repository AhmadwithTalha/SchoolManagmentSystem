//using Microsoft.AspNetCore.Identity;
//using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models;
//{
//    public class ApplicationUser : IdentityUser
//    {
//       public string UserGuid = Guid.NewGuid().ToString();

//        [Required]
//        public string FirstName { get; set; }

//        [Required]
//        public string LastName { get; set; }

//        [Required]
//        public string Country { get; set; }

//        [Required]
//        public string City { get; set; }

//        [Required]
//        public string Address { get; set; }

//        [Required]
//        public string ProfileImage { get; set; } // Store filename

//        // Optional: soft delete flag
//        public bool IsDeleted { get; set; } = false;
//    }
//}
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        // Automatically generate Guid for each new user
        UserGuid = Guid.NewGuid().ToString();
    }

    [Required]
    public string UserGuid { get; set; } // New column

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Country { get; set; }

    [Required]
    public string City { get; set; }

    [Required]
    public string Address { get; set; }

    [Required]
    public string ProfileImage { get; set; }

    public bool IsDeleted { get; set; } = false;
}
