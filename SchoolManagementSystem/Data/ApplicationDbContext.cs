using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Optional: You can add DbSets for TeacherDetails, StudentDetails later
        // public DbSet<TeacherDetail> TeacherDetails { get; set; }
        // public DbSet<StudentDetail> StudentDetails { get; set; }
    }
}
