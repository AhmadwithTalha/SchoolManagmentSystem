using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Models;

namespace SchoolManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
    
        }

        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<ApplicationUser>()
        //        .HasOne(u => u.Country)
        //        .WithMany()
        //        .HasForeignKey(u => u.Id)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<ApplicationUser>()
        //        .HasOne(u => u.City)
        //        .WithMany()
        //        .HasForeignKey(u => u.Id)
        //        .OnDelete(DeleteBehavior.Restrict);
        //}

    }
}
