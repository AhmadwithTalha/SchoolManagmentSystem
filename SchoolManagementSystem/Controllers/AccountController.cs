using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
using SchoolManagementSystem.Services;

namespace SchoolManagementSystem.Controllers
{

  
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly PrinciplePdfService _principlePdfService;

        public AccountController(
            UserManager<ApplicationUser> userManager,  
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment,
            ApplicationDbContext context,
            PrinciplePdfService principlePdfService
            )


        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _environment = environment;
            _context = context;
            _principlePdfService = principlePdfService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new PrincipleRegisterViewModel
            {
                Countries = _context.Countries
                                   .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                                   .ToList(),
                Cities = new List<SelectListItem>() // empty initially
            };
            return View(model);
        }

        [HttpGet]
        public JsonResult GetCitiesByCountry(int countryId)
        {
            var cities = _context.Cities
                                 .Where(c => c.CountryId == countryId)
                                 .Select(c => new { c.Id, c.Name })
                                 .ToList();
            return Json(cities);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PrincipleRegisterViewModel model)
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

            if (!ModelState.IsValid)
            {
                // Repopulate countries and cities
                model.Countries = _context.Countries
                                          .Select(c => new SelectListItem
                                          {
                                              Value = c.Id.ToString(),
                                              Text = c.Name
                                          }).ToList();

                model.Cities = model.CountryId != 0
                    ? _context.Cities
                              .Where(c => c.CountryId == model.CountryId)
                              .Select(c => new SelectListItem
                              {
                                  Value = c.Id.ToString(),
                                  Text = c.Name
                              }).ToList()
                    : new List<SelectListItem>();

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
                CountryId = model.CountryId,
                CityId = model.CityId,
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
            return RedirectToAction("Login", "Account");

        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);

            var user = await _context.Users
                .Include(u => u.Country)
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Register");

            return View(user);
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, DO NOT show login page
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Principal"))
                    return RedirectToAction("Dashboard", "Home");

                return RedirectToAction("Index", "Home");
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Account");
        }


        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }
        
        [HttpGet]
        public IActionResult DeleteProfile()
        {
            return View();
        }
        
        [HttpPost, ActionName("DeleteProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileConfirmed()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            await _signInManager.SignOutAsync();
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to delete your account!";
                return RedirectToAction("Profile");
            }

            TempData["SuccessMessage"] = "Your account has been deleted.";
            return RedirectToAction("Index", "Home");
        }

        // ================= UPDATE PROFILE =================
        
        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var model = new PrincipleUpdateProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CountryId = user.CountryId,
                CityId = user.CityId,
                Address = user.Address,
                ExistingProfileImage = user.ProfileImage,

                Countries = await _context.Countries
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync(),

                Cities = await _context.Cities
            .Where(c => c.CountryId == user.CountryId)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync()
            };

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(PrincipleUpdateProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                model.Countries = await _context.Countries
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync();

                model.Cities = await _context.Cities
                    .Where(c => c.CountryId == model.CountryId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync();
                model.ExistingProfileImage = user.ProfileImage;
                return View(model);
            }

            // ===== UPDATE TEXT DATA (ENCRYPTED SAME AS REGISTER) =====
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumberPublic = model.PhoneNumber;
            user.CountryId = (int)model.CountryId;
            user.CityId = model.CityId;
            user.Address = model.Address;

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // ===== IMAGE DECISION LOGIC =====
            // Priority: Live Capture > Upload > Existing

            // 1️⃣ LIVE CAPTURE (TOP PRIORITY)
            if (!string.IsNullOrEmpty(model.ProfileImageBase64))
            {
                string base64Data = model.ProfileImageBase64.Split(',')[1];
                byte[] bytes = Convert.FromBase64String(base64Data);

                string newFileName = Guid.NewGuid() + ".png";
                string filePath = Path.Combine(uploadsFolder, newFileName);

                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                user.ProfileImage = newFileName;
            }
            // 2️⃣ FILE UPLOAD
            else if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                string newFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, newFileName);

                using var fs = new FileStream(filePath, FileMode.Create);
                await model.ProfileImageFile.CopyToAsync(fs);

                user.ProfileImage = newFileName;
            }
            // 3️⃣ ELSE → KEEP OLD IMAGE (DO NOTHING)

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                model.ExistingProfileImage = user.ProfileImage;
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        //public async Task<IActionResult> ExportPdf()
        //{
        //    var principals = await _context.Users
        //        .Include(u => u.Country)
        //        .Include(u => u.City)
        //        .Where(u => !u.IsDeleted) // Only active
        //        .Where(u => _context.UserRoles
        //            .Any(ur => ur.UserId == u.Id &&
        //                       _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Principle")))
        //        .ToListAsync();

        //    var pdfBytes = _principlePdfService.GeneratePrinciplePdf(principals);

        //    return File(pdfBytes, "application/pdf", "Principals_Report.pdf");
        //}



    }
}
