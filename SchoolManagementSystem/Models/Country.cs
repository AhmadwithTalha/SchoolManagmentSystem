using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystem.Models
{
    public class Country
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Country name is required")]
        public string Name { get; set; } = string.Empty;
        public ICollection<City> Cities { get; set; }
            = new List<City>();
    }
}
