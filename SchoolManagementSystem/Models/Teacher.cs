using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagementSystem.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }   // FK → AspNetUsers

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Address { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
