
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
using SchoolManagementSystem.Services;
using SkiaSharp;


namespace SchoolManagementSystem.Controllers
{


    [Authorize]
    public class HomeController : Controller
    {
        private readonly PrinciplePdfService _principlePdfService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            PrinciplePdfService principlePdfService,
            RoleManager<IdentityRole> roleManager
        )
        {
            _context = context;                 
            _userManager = userManager;              
            _principlePdfService = principlePdfService; 
            _roleManager = roleManager;               
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Principal"))
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [Authorize(Roles = "Principal")]
        public async Task<IActionResult> Dashboard()
        {
            var users = _userManager.Users.ToList();
            var principals = await _userManager.GetUsersInRoleAsync("Principal");
            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
            var student = await _userManager.GetUsersInRoleAsync("Student");

            var model = new DashboardViewModel
            {
                TotalUsers = users.Where(s=>s.IsDeleted == false).Count(),
                TotalPrincipals = principals.Where(s => s.IsDeleted == false).Count(),
                TotalTeachers = teachers.Where(s => s.IsDeleted == false).Count(),
                TotalStudents = student.Where(s => s.IsDeleted == false).Count()
            };

            return View(model);
        }
      


    }
}
