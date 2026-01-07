using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    public class City
    {


        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "City name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        
        public int CountryId { get; set; }

        public Country? Country { get; set; }

    }
    public class CityVIewModel
    {


        public int Id { get; set; }

        [Required(ErrorMessage = "City name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]

        public int CountryId { get; set; }


    }

}
