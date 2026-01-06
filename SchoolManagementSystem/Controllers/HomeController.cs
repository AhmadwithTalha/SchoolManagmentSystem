

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;


namespace SchoolManagementSystem.Controllers
{


    [Authorize]
    public class HomeController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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

            var model = new DashboardViewModel
            {
                TotalUsers = users.Count,
                TotalPrincipals = principals.Count,
                TotalTeachers = teachers.Count,
                TotalStudents = 0
            };

            return View(model);
        }
















        //public IActionResult Index()
        //{
        //    // If Principal ? go to dashboard
        //    if (User.IsInRole("Principal"))
        //    {
        //        return RedirectToAction("Dashboard");
        //    }

        //    // Teacher stays here (empty / profile only)
        //    return View();
        //}

        //public IActionResult Home()
        //{
        //    return View();
        //}

       
        

        //[Authorize(Roles = "Principal")]
        //public IActionResult Dashboard()
        //{
        //    return View();
        //}
    }
}
