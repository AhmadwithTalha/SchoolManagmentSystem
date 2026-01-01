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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Create uploads folder if not exist
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string profileFileName = string.Empty;

            // Save file upload if present
            if (model.ProfileImageFile != null)
            {
                profileFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
                string uploadPath = Path.Combine(uploadsFolder, profileFileName);
                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await model.ProfileImageFile.CopyToAsync(fileStream);
                }
            }
            if (model.ProfileImageFile == null && string.IsNullOrEmpty(model.ProfileImageBase64))
            {
                ModelState.AddModelError("", "Profile image is required (either upload or capture).");
                return View(model);
            }

            // Save base64 image if present (live webcam)
            if (!string.IsNullOrEmpty(model.ProfileImageBase64))
            {
                string base64 = model.ProfileImageBase64.Split(',')[1];
                byte[] bytes = Convert.FromBase64String(base64);
                string base64FileName = Guid.NewGuid() + ".png";
                string base64FilePath = Path.Combine(uploadsFolder, base64FileName);
                await System.IO.File.WriteAllBytesAsync(base64FilePath, bytes);
                profileFileName = base64FileName;
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
                PhoneNumberPublic = model.PhoneNumber,
                ProfileImage = profileFileName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Principal"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Principal"));
            }

            await _userManager.AddToRoleAsync(user, "Principal");
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = "Registration successful!";
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
