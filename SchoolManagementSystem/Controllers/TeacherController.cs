using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
using SchoolManagementSystem.Services;
namespace SchoolManagementSystem.Controllers;

[Authorize(Roles = "Principal")]
public class TeacherController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _context;
    private readonly TeacherPdfService _teacherPdfService;


    public TeacherController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment environment,
        ApplicationDbContext context,
        TeacherPdfService teacherPdfService
        
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
        _context = context;
        _teacherPdfService = teacherPdfService;
    }

    // ===================== CREATE TEACHER =====================
    [Authorize(Roles = "Principal")]
    public IActionResult TeacherRegister()
    {
        var model = new TeacherRegisterViewModel
        {
            Countries = _context.Countries
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList(),

            Cities = new List<SelectListItem>() // empty initially
        };

        return View(model);
    }
    [Authorize(Roles = "Principal")]

    public async Task<IActionResult> Index()
    {
        
        var teachers = await _context.Users
       .Include(u => u.Country)
       .Include(u => u.City)
       .Where(u => !u.IsDeleted)
       .Where(u => _context.UserRoles
           .Any(ur => ur.UserId == u.Id &&
                      _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Teacher")))
       .ToListAsync();

        return View("Show", teachers);
    }

    [Authorize(Roles = "Principal")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeacherRegister(TeacherRegisterViewModel model)
    {
        
        if (!ModelState.IsValid)
        {
            model.Countries = _context.Countries
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            model.Cities = _context.Cities
                .Where(c => c.CountryId == model.CountryId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return View(model);
        }

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

        //TempData["SuccessMessage"] = "Teacher created successfully!";
        return RedirectToAction("Index", "Teacher");
    }


    [Authorize(Roles = "Principal")]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var teacher = await _userManager.FindByIdAsync(id);
        if (teacher == null)
            return NotFound();

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
            ExistingProfileImage = teacher.ProfileImage,

            // ✅ Load dropdowns
            Countries = await _context.Countries
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync(),

            Cities = await _context.Cities
                .Where(c => c.CountryId == teacher.CountryId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync()
        };

        return View(model);
    }
    //Delete
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var teacher = await _context.Users.FindAsync(id);
        if (teacher == null) return NotFound();

        teacher.IsDeleted = true;   // soft delete
        _context.Update(teacher);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }


    [Authorize(Roles = "Principal")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TeacherUpdateViewModel model)
    {
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

            return View(model);
        }

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

       
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportPdf()
    {
        var teachers = await _context.Users
            .Include(u => u.Country)
            .Include(u => u.City)
            .Where(u => !u.IsDeleted)
            .Where(u => _context.UserRoles
                .Any(ur => ur.UserId == u.Id &&
                           _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Teacher")))
            .ToListAsync();

        var pdfBytes = _teacherPdfService.GenerateTeacherPdf(teachers);

        return File(pdfBytes, "application/pdf", "Teachers_Report.pdf");
    }


}
