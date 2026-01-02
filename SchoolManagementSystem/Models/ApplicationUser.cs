

//namespace SchoolManagementSystem.Models;

//using Microsoft.AspNetCore.Identity;
//using System.ComponentModel.DataAnnotations;

//public class ApplicationUser : IdentityUser
//{
//    public ApplicationUser()
//    {
//        // Automatically generate Guid for each new user
//        UserGuid = Guid.NewGuid().ToString();
//    }

//    [Required]
//    public string UserGuid { get; set; } // New column

//    [Required]
//    public string FirstName { get; set; }

//    [Required]
//    public string LastName { get; set; }

//    [Required]
//    public string Country { get; set; }

//    [Required]
//    public string City { get; set; }

//    [Required]
//    public string Address { get; set; }

//    [Required]
//    public string ProfileImage { get; set; }

//    public bool IsDeleted { get; set; } = false;
//}
using Microsoft.AspNetCore.Identity;
using SchoolManagementSystem.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Store encrypted in DB
        public string EncryptedFirstName { get; set; } = string.Empty;// Pascal notation
        public string EncryptedLastName { get; set; } = string.Empty;
        public string EncryptedCountry { get; set; } = string.Empty;
        public string EncryptedCity { get; set; } = string.Empty;
        public string EncryptedAddress { get; set; } = string.Empty;
        public string EncryptedPhoneNumber { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        // Not mapped properties for use in views
        [NotMapped]
        public string FirstName
        {
            get => EncryptionHelper.Decrypt(EncryptedFirstName);
            set => EncryptedFirstName = EncryptionHelper.Encrypt(value);
        }

        [NotMapped]
        public string LastName
        {
            get => EncryptionHelper.Decrypt(EncryptedLastName);
            set => EncryptedLastName = EncryptionHelper.Encrypt(value);
        }

        [NotMapped]
        public string Country
        {
            get => EncryptionHelper.Decrypt(EncryptedCountry);
            set => EncryptedCountry = EncryptionHelper.Encrypt(value);
        }

        [NotMapped]
        public string City
        {
            get => EncryptionHelper.Decrypt(EncryptedCity);
            set => EncryptedCity = EncryptionHelper.Encrypt(value);
        }

        [NotMapped]
        public string Address
        {
            get => EncryptionHelper.Decrypt(EncryptedAddress);
            set => EncryptedAddress = EncryptionHelper.Encrypt(value);
        }

        [NotMapped]
        public string PhoneNumberPublic
        {
            get => EncryptionHelper.Decrypt(EncryptedPhoneNumber);
            set => EncryptedPhoneNumber = EncryptionHelper.Encrypt(value);
        }
    }
}
