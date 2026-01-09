using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

[Authorize(Roles = "Principal")]
public class CityController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CityPdfService _cityPdfService;
    public CityController(ApplicationDbContext context, CityPdfService cityPdfService)
    {
        _context = context;
        _cityPdfService = cityPdfService;
    }

    // INDEX
    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities
            .Include(c => c.Country)
            .ToListAsync();

        return View(cities);
    }

    //  CREATE
    public IActionResult Create()
    {
        ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CityVIewModel city)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", city.CountryId);
            return View(city);
        }
        City model = new City();
        if (city != null)
        {
            model.Name = city.Name;
            model.CountryId = city.CountryId;
        }


        _context.Cities.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ================= EDIT =================
    public async Task<IActionResult> Edit(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null) return NotFound();

        ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", city.CountryId);
        return View(city);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(City city)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", city.CountryId);
            return View(city);
        }

        _context.Update(city);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ================= DELETE =================
    public async Task<IActionResult> Delete(int id)
    {
        var city = await _context.Cities
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (city == null) return NotFound();

        return View(city);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        _context.Cities.Remove(city!);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportPdf()
    {
        var cities = await _context.Cities
            .Include(c => c.Country)
            .ToListAsync();

        var pdfBytes = _cityPdfService.GenerateCityPdf(cities);

        return File(
            pdfBytes,
            "application/pdf",
            "Cities_Report.pdf"
        );
    }
    [Authorize] // optional
    public IActionResult ExportExcel()
    {
        // 1️⃣ Get city data WITH country
        var cities = _context.Cities
                             .Include(c => c.Country) // 🔥 IMPORTANT
                             .OrderBy(c => c.Name)
                             .ToList();

        // 2️⃣ Create Excel file in memory
        using var package = new ExcelPackage();

        // 3️⃣ Create worksheet
        var worksheet = package.Workbook.Worksheets.Add("Cities");

        // 4️⃣ Title
        worksheet.Cells["A1:C1"].Merge = true;
        worksheet.Cells["A1"].Value = "City Reports";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        // 5️⃣ Column Headers
        worksheet.Cells["A3"].Value = "Sr #";
        worksheet.Cells["B3"].Value = "City Name";
        worksheet.Cells["C3"].Value = "Country Name";

        worksheet.Cells["A3:C3"].Style.Font.Bold = true;
        worksheet.Cells["A3:C3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A3:C3"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        worksheet.Cells["A3:C3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // 6️⃣ Data Rows
        int row = 4;
        int sr = 1;

        foreach (var city in cities)
        {
            worksheet.Cells[row, 1].Value = sr++;
            worksheet.Cells[row, 2].Value = city.Name;
            worksheet.Cells[row, 3].Value = city.Country?.Name; // ⭐ KEY LINE

            worksheet.Cells[row, 1, row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            row++;
        }

        // 7️⃣ Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // 8️⃣ Download Excel
        var excelBytes = package.GetAsByteArray();

        return File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "City_Report.xlsx"
        );
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "Please select an Excel file to upload.";
            return RedirectToAction(nameof(Index));
        }

        var errors = new List<string>();
        int createdCount = 0;

        using (var stream = new MemoryStream())
        {
            await excelFile.CopyToAsync(stream);

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0]; // first sheet
                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++) // row 1 = header
                {
                    string cityName = worksheet.Cells[row, 1].Text.Trim();
                    string countryName = worksheet.Cells[row, 2].Text.Trim();

                    // 1️⃣ Validate empty
                    if (string.IsNullOrEmpty(cityName))
                    {
                        errors.Add($"Row {row}: City Name is empty");
                        continue;
                    }
                    if (string.IsNullOrEmpty(countryName))
                    {
                        errors.Add($"Row {row}: Country Name is empty");
                        continue;
                    }

                    // 2️⃣ Find Country
                    var country = _context.Countries
                        .FirstOrDefault(c => c.Name.ToLower() == countryName.ToLower());

                    if (country == null)
                    {
                        errors.Add($"Row {row}: Country '{countryName}' does not exist");
                        continue;
                    }

                    // 3️⃣ Check duplicate city in same country
                    bool cityExists = _context.Cities
                        .Any(c => c.Name.ToLower() == cityName.ToLower() && c.CountryId == country.Id);

                    if (cityExists)
                    {
                        errors.Add($"Row {row}: City '{cityName}' already exists in country '{countryName}'");
                        continue;
                    }

                    // 4️⃣ Save new city
                    var newCity = new City
                    {
                        Name = cityName,
                        CountryId = country.Id
                    };
                    _context.Cities.Add(newCity);
                    createdCount++;
                }

                // Save all valid rows
                await _context.SaveChangesAsync();
            }
        }
        TempData["Success"] = $"{createdCount} cities imported successfully.";
        if (errors.Count > 0)
            TempData["Errors"] = string.Join("<br/>", errors);

        return RedirectToAction(nameof(Index));
    }
}