using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Helpers;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;

namespace SchoolManagementSystem.Controllers
{
    [Authorize(Roles = "Principal")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(UserManager<ApplicationUser> userManager,
                                 RoleManager<IdentityRole> roleManager,
                                 ApplicationDbContext context,
                                 IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            // Get all users in Student role
            var students = await _userManager.GetUsersInRoleAsync("Student");

            // Optionally include Country/City for display
            var studentList = await _context.Users
                                            .Include(u => u.Country)
                                            .Include(u => u.City)
                                            .Where(u => !u.IsDeleted)
                                            //.Where(u => students.Select(s => s.Id).Contains(u.Id))
                                            .ToListAsync();

            return View(studentList);
        }

        // ✅ GET Create/Edit
        [HttpGet]
        public async Task<IActionResult> SaveStudent(string? id)
        {
            var model = new StudentViewModel();

            if (!string.IsNullOrEmpty(id))
            {
                var student = await _context.Users
                                  .Include(u => u.Country)
                                  .Include(u => u.City)
                                  .FirstOrDefaultAsync(u => u.Id == id);

                if (student == null) return NotFound();

                model.Id = student.Id;
                model.FirstName = student.FirstName;
                model.LastName = student.LastName;
                model.Email = student?.Email;
                model.PhoneNumber = student?.PhoneNumber;
                model.CountryId = student.CountryId;
                model.CityId = student.CityId;
                model.Address = student.Address;
                model.ExistingProfileImage = student.ProfileImage;
            }

            model.Countries = await _context.Countries
                               .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                               .ToListAsync();

            model.Cities = !string.IsNullOrEmpty(id)
                           ? await _context.Cities
                               .Where(c => c.CountryId == model.CountryId)
                               .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                               .ToListAsync()
                           : new List<SelectListItem>();

            return View("SaveStudent",model);
        }

        // ✅ POST Create/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStudent(StudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Countries = await _context.Countries
                                   .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                                   .ToListAsync();

                model.Cities = await _context.Cities
                                   .Where(c => c.CountryId == model.CountryId)
                                   .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                                   .ToListAsync();

                return View(model);
            }

            string profileFileName = model.ExistingProfileImage;

            // Image handling
            if (model.ProfileImageFile != null)
            {
                profileFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
                string path = Path.Combine(_environment.WebRootPath, "images", profileFileName);
                using var stream = new FileStream(path, FileMode.Create);
                await model.ProfileImageFile.CopyToAsync(stream);
            }
            else if (!string.IsNullOrEmpty(model.ProfileImageBase64))
            {
                var bytes = Convert.FromBase64String(model.ProfileImageBase64.Split(',')[1]);
                profileFileName = Guid.NewGuid() + ".png";
                string path = Path.Combine(_environment.WebRootPath, "images", profileFileName);
                await System.IO.File.WriteAllBytesAsync(path, bytes);
            }

            if (string.IsNullOrEmpty(model.Id))
            {
                // CREATE
                var student = new ApplicationUser
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
                    ProfileImage = profileFileName
                };

                var result = await _userManager.CreateAsync(student, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                        ModelState.AddModelError("", err.Description);

                    model.Countries = await _context.Countries
                                       .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                                       .ToListAsync();

                    return View(model);
                }

                if (!await _roleManager.RoleExistsAsync("Student"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Student"));
                }

                await _userManager.AddToRoleAsync(student, "Student");
            }
            else
            {
                // EDIT
                var student = await _context.Users.FindAsync(model.Id);
                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.CountryId = model.CountryId;
                student.CityId = model.CityId;
                student.PhoneNumberPublic = model.PhoneNumber;
                student.PhoneNumber = model.PhoneNumber;
                student.Address = model.Address;
                student.ProfileImage = profileFileName;
                student.PasswordHash = model.Password;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    await _userManager.RemovePasswordAsync(student);
                    await _userManager.AddPasswordAsync(student, model.Password);
                }

                _context.Update(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Student");
        }

        //Delete
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var student = await _context.Users.FindAsync(id);
            if (student == null) return NotFound();

            student.IsDeleted = true;   // soft delete
            _context.Update(student);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        //AJAX Get Cities for Country
        public JsonResult GetCitiesByCountry(int countryId)
        {
            var cities = _context.Cities
                                 .Where(c => c.CountryId == countryId)
                                 .Select(c => new { c.Id, c.Name })
                                 .ToList();
            return Json(cities);
        }
    }
}
