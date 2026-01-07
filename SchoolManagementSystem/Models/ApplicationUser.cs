using Microsoft.AspNetCore.Http;
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
        [ForeignKey("Country")]
        public int EncryptedCountryId { get; set; }
        [ForeignKey("City")]
        public int EncryptedCityId { get; set; } 
        public string EncryptedAddress { get; set; } = string.Empty;
        public string EncryptedPhoneNumber { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;


        public Country Country { get; set; }
        public City City { get; set; }

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
        public int CountryId
        {
            get => EncryptedCountryId;
            set => EncryptedCountryId = value;
        }
       

        [NotMapped]
        public int CityId
        {
            get => EncryptedCityId;
            set => EncryptedCityId = value;
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
