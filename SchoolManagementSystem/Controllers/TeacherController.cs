using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
namespace SchoolManagementSystem.Controllers;
[Authorize(Roles = "Principal")]
public class TeacherController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _environment;

    public TeacherController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
    }

    // ===================== CREATE TEACHER =====================
    [Authorize(Roles = "Principal")]
    public IActionResult TeacherRegister()
    {
        return View();

    }
    [Authorize(Roles = "Principal")]
    public async Task<IActionResult> Index()
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        return View("Show", teachers);
    }


    [Authorize(Roles = "Principal")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeacherRegister(TeacherRegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string profileFileName = string.Empty;

        // FILE UPLOAD
        if (model.ProfileImageFile != null)
        {
            profileFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
            string uploadPath = Path.Combine(uploadsFolder, profileFileName);
            using var fs = new FileStream(uploadPath, FileMode.Create);
            await model.ProfileImageFile.CopyToAsync(fs);
        }

        // LIVE CAPTURE
        if (!string.IsNullOrEmpty(model.ProfileImageBase64))
        {
            string base64 = model.ProfileImageBase64.Split(',')[1];
            byte[] bytes = Convert.FromBase64String(base64);
            profileFileName = Guid.NewGuid() + ".png";
            await System.IO.File.WriteAllBytesAsync(
                Path.Combine(uploadsFolder, profileFileName), bytes);
        }
        var user = new ApplicationUser

                {
                    Id = Guid.NewGuid().ToString(),
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            PhoneNumberPublic = model.PhoneNumber,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CountryId = model.CountryId,
            CityId = model.CityId,
            Address = model.Address,
            ProfileImage = profileFileName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync("Teacher"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Teacher"));
        }

        await _userManager.AddToRoleAsync(user, "Teacher");

        TempData["SuccessMessage"] = "Teacher created successfully!";
        return RedirectToAction("Index", "Teacher");
    }


    [Authorize(Roles = "Principal")]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var teacher = await _userManager.FindByIdAsync(id);
        if (teacher == null) return NotFound();

        var model = new TeacherUpdateViewModel
        {
            Id = teacher.Id,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Email = teacher.Email,
            PhoneNumber = teacher.PhoneNumberPublic,
            CountryId = teacher.CountryId,
            CityId = teacher.CityId,
            Address = teacher.Address,
            ExistingProfileImage = teacher.ProfileImage
        };

        return View(model);
    }


    [Authorize(Roles = "Principal")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TeacherUpdateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var teacher = await _userManager.FindByIdAsync(model.Id);
        if (teacher == null) return NotFound();

        // Update text fields
        teacher.FirstName = model.FirstName;
        teacher.LastName = model.LastName;
        teacher.PhoneNumberPublic = model.PhoneNumber;
        teacher.CountryId = model.CountryId;
        teacher.CityId= model.CityId;
        teacher.Address = model.Address;

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Image logic: Live Capture > Upload > Keep old
        if (!string.IsNullOrEmpty(model.ProfileImageBase64))
        {
            string base64Data = model.ProfileImageBase64.Split(',')[1];
            byte[] bytes = Convert.FromBase64String(base64Data);
            string newFileName = Guid.NewGuid() + ".png";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadsFolder, newFileName), bytes);
            teacher.ProfileImage = newFileName;
        }
        else if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
        {
            string newFileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, newFileName);
            using var fs = new FileStream(filePath, FileMode.Create);
            await model.ProfileImageFile.CopyToAsync(fs);
            teacher.ProfileImage = newFileName;
        }

        await _userManager.UpdateAsync(teacher);

        TempData["SuccessMessage"] = "Teacher updated successfully!";
       
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        return View("Show", teachers);
    
}

}
