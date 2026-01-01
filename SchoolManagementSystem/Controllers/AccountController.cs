using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;

namespace SchoolManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Create uploads folder if it doesn't exist
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Save image
            string uniqueFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImageFile.CopyToAsync(fileStream);
            }

            // Create user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Country = model.Country,
                City = model.City,
                Address = model.Address,
                ProfileImage = uniqueFileName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(model);
            }

            // Ensure "Principal" role exists
            if (!await _roleManager.RoleExistsAsync("Principal"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Principal"));
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, "Principal");

            // Auto login after registration
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Register");

            return View(user);
        }
    }
}
