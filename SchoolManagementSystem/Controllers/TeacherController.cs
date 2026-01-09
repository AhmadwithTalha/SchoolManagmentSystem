using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
using SchoolManagementSystem.Services;
using System.Drawing;
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

    public IActionResult ExportExcel()
    {
        // 1️⃣ Get all students
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var students = _context.Users
                               .Where(u => u.IsDeleted == false && _context.UserRoles
                               .Any(r => r.UserId == u.Id && r.RoleId == _context.Roles
                               .FirstOrDefault(role => role.Name == "Teacher").Id))
                               .Include(u => u.City)
                               .Include(u => u.Country)
                               .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        // 2️⃣ Create Excel package
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Teachers");

        // 3️⃣ Title row
        worksheet.Cells["A1:I1"].Merge = true;
        worksheet.Cells["A1"].Value = "Teachers Reports";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        // 4️⃣ Column headers
        worksheet.Cells["A3"].Value = "Sr No";
        worksheet.Cells["B3"].Value = "First Name";
        worksheet.Cells["C3"].Value = "Last Name";
        worksheet.Cells["D3"].Value = "Email";
        worksheet.Cells["E3"].Value = "Phone Number";
        worksheet.Cells["F3"].Value = "City";
        worksheet.Cells["G3"].Value = "Country";
        worksheet.Cells["H3"].Value = "Address";
        worksheet.Cells["I3"].Value = "Profile Picture";

        worksheet.Cells["A3:I3"].Style.Font.Bold = true;
        worksheet.Cells["A3:I3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A3:I3"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        worksheet.Cells["A3:I3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // 5️⃣ Fill data
        int row = 4;
        int sr = 1;
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        foreach (var student in students)
        {
            worksheet.Cells[row, 1].Value = sr++;
            worksheet.Cells[row, 2].Value = student.FirstName; // decrypted
            worksheet.Cells[row, 3].Value = student.LastName;
            worksheet.Cells[row, 4].Value = student.Email;
            worksheet.Cells[row, 5].Value = student.PhoneNumberPublic;
            worksheet.Cells[row, 6].Value = student.City?.Name;
            worksheet.Cells[row, 7].Value = student.Country?.Name;
            worksheet.Cells[row, 8].Value = student.Address;

            // Profile Image
            if (!string.IsNullOrEmpty(student.ProfileImage))
            {
                string imagePath = Path.Combine(wwwrootPath, "images", student.ProfileImage);
                if (System.IO.File.Exists(imagePath))
                {
                    Image img = Image.FromFile(imagePath); // load image
                    var picture = worksheet.Drawings.AddPicture($"img{row}", img);
                    picture.SetPosition(row - 1, 5, 8, 5); // rowIndex-1, offset, colIndex=I(9)
                    picture.SetSize(50, 50); // Resize to fit
                    worksheet.Row(row).Height = 40; // Increase row height
                }
            }

            worksheet.Cells[row, 1, row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            row++;
        }

        // 6️⃣ Auto-fit columns (except image)
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // 7️⃣ Return file
        var excelBytes = package.GetAsByteArray();
        return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Teachers_Report.xlsx");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportTeacherExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an Excel file.";
            return RedirectToAction("Index");
        }

        var errors = new List<string>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            TempData["Error"] = "Excel file is empty or invalid.";
            return RedirectToAction("Index");
        }

        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++) // header is row 1
        {
            string firstName = worksheet.Cells[row, 1].Text.Trim();
            string lastName = worksheet.Cells[row, 2].Text.Trim();
            string email = worksheet.Cells[row, 3].Text.Trim();
            string phone = worksheet.Cells[row, 4].Text.Trim();
            string cityName = worksheet.Cells[row, 5].Text.Trim();
            string countryName = worksheet.Cells[row, 6].Text.Trim();
            string address = worksheet.Cells[row, 7].Text.Trim();
            string password = worksheet.Cells[row, 8].Text.Trim();

            // Skip if required fields missing
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(countryName) ||
                string.IsNullOrEmpty(address))
            {
                errors.Add($"Row {row}: Missing required field(s).");
                continue;
            }

            // Email must be unique
            if (await _userManager.Users.AnyAsync(u => u.Email == email))
            {
                errors.Add($"Row {row}: Email '{email}' already exists.");
                continue;
            }

            // Country check
            var country = await _context.Countries.FirstOrDefaultAsync(c => c.Name.ToLower() == countryName.ToLower());
            if (country == null)
            {
                errors.Add($"Row {row}: Country '{countryName}' does not exist.");
                continue;
            }

            // City check
            var city = await _context.Cities.FirstOrDefaultAsync(c =>
                c.Name.ToLower() == cityName.ToLower() && c.CountryId == country.Id);
            if (city == null)
            {
                errors.Add($"Row {row}: City '{cityName}' does not exist in country '{countryName}'.");
                continue;
            }

            // Default password if empty
            if (string.IsNullOrEmpty(password))
                password = "Teacher@123"; // fallback

            // Default profile image
            string profileImage = "default-user.png";

            // Create teacher
            var teacher = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email, // email as username
                Email = email,
                PhoneNumber = phone,
                FirstName = firstName,
                LastName = lastName,
                CountryId = country.Id,
                CityId = city.Id,
                Address = address,
                ProfileImage = profileImage
            };

            // Create user in Identity
            var result = await _userManager.CreateAsync(teacher, password);
            if (!result.Succeeded)
            {
                errors.Add($"Row {row}: Could not create user '{email}'. " +
                           string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            // Ensure Teacher role exists
            if (!await _roleManager.RoleExistsAsync("Teacher"))
                await _roleManager.CreateAsync(new IdentityRole("Teacher"));

            // Assign Teacher role
            await _userManager.AddToRoleAsync(teacher, "Teacher");
        }

        if (errors.Any())
            TempData["Error"] = string.Join("<br/>", errors);
        else
            TempData["Success"] = "Teachers imported successfully!";

        return RedirectToAction("Index");
    }

}
