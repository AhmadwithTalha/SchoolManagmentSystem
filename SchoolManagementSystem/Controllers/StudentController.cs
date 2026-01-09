using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Helpers;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Models.ViewModels;
using SchoolManagementSystem.Services;
using System.Drawing;

namespace SchoolManagementSystem.Controllers
{
    [Authorize(Roles = "Principal")]
    public class StudentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly StudentPdfService _studentPdfService;

        public StudentController(UserManager<ApplicationUser> userManager,
                                 RoleManager<IdentityRole> roleManager,
                                 ApplicationDbContext context,
                                 IWebHostEnvironment environment,
                                 StudentPdfService studentPdfService
           )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _environment = environment;
            _studentPdfService = studentPdfService;
        }

        public async Task<IActionResult> Index()
        {

            var student = await _context.Users
           .Include(u => u.Country)
           .Include(u => u.City)
           .Where(u => !u.IsDeleted)
           .Where(u => _context.UserRoles
               .Any(ur => ur.UserId == u.Id &&
                          _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Student")))
           .ToListAsync();

            return View(student);
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
                //model.ExistingProfileImage = student.ProfileImage;

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

            string? profileFileName = model.ExistingProfileImage ?? null;
            if (string.IsNullOrEmpty(profileFileName))
            {
                profileFileName = "default-user.png";
            }

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


        public async Task<IActionResult> ExportPdf()
        {
            var students = await _context.Users
                .Include(u => u.Country)
                .Include(u => u.City)
                .Where(u => !u.IsDeleted) // only non-deleted
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id &&
                               _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Student")))
                .ToListAsync();

            var pdfBytes = _studentPdfService.GenerateStudentPdf(students);

            return File(pdfBytes, "application/pdf", "Students_Report.pdf");
        }

        [Authorize]
        public IActionResult ExportExcel()
        {
            // 1️⃣ Get all students
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var students = _context.Users
                                   .Where(u => u.IsDeleted == false && _context.UserRoles
                                   .Any(r => r.UserId == u.Id && r.RoleId == _context.Roles
                                   .FirstOrDefault(role => role.Name == "Student").Id))
                                   .Include(u => u.City)
                                   .Include(u => u.Country)
                                   .ToList();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // 2️⃣ Create Excel package
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Students");

            // 3️⃣ Title row
            worksheet.Cells["A1:I1"].Merge = true;
            worksheet.Cells["A1"].Value = "Student Reports";
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
                        "Student_Report.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudentExcel(IFormFile file)
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

            for (int row = 2; row <= rowCount; row++)
            {
                string firstName = worksheet.Cells[row, 1].Text.Trim();
                string lastName = worksheet.Cells[row, 2].Text.Trim();
                string email = worksheet.Cells[row, 3].Text.Trim();
                string phone = worksheet.Cells[row, 4].Text.Trim();
                string cityName = worksheet.Cells[row, 5].Text.Trim();
                string countryName = worksheet.Cells[row, 6].Text.Trim();
                string address = worksheet.Cells[row, 7].Text.Trim();
                string password = worksheet.Cells[row, 8].Text.Trim();

                // Required fields
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                    string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone) ||
                    string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(countryName))
                {
                    errors.Add($"Row {row}: Missing required field(s).");
                    continue;
                }

                // Email unique check
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

                // Default password if missing or too weak
                if (string.IsNullOrEmpty(password) || password.Length < 6)
                    password = "Student@123";

                string profileImage = "default-user.png";

                var student = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    Email = email,
                    PhoneNumber = phone,
                    FirstName = firstName,
                    LastName = lastName,
                    CountryId = country.Id,
                    CityId = city.Id,
                    Address = address,
                    ProfileImage = profileImage
                };

                var result = await _userManager.CreateAsync(student, password);

                if (!result.Succeeded)
                {
                    errors.Add($"Row {row}: Could not create '{email}'. " +
                               string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                if (!await _roleManager.RoleExistsAsync("Student"))
                    await _roleManager.CreateAsync(new IdentityRole("Student"));

                await _userManager.AddToRoleAsync(student, "Student");
            }

            if (errors.Any())
                TempData["Error"] = string.Join("<br/>", errors);
            else
                TempData["Success"] = "Students imported successfully!";

            return RedirectToAction("Index");
        }


    }
}
