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
    #region Fields and Constructor
    private readonly ApplicationDbContext _context;
    private readonly CityPdfService _cityPdfService;

    public CityController(ApplicationDbContext context, CityPdfService cityPdfService)
    {
        _context = context;
        _cityPdfService = cityPdfService;
    }
    #endregion

    #region Index / List All Cities
    // Show list of cities along with their countries
    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities
            .Include(c => c.Country)
            .ToListAsync();

        return View(cities);
    }
    #endregion

    #region Create New City
    // Show create form
    public IActionResult Create()
    {
        ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name");
        return View();
    }

    // Handle create form submission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CityVIewModel city)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", city.CountryId);
            return View(city);
        }

        var model = new City
        {
            Name = city.Name,
            CountryId = city.CountryId
        };

        _context.Cities.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Edit City
    // Show edit form
    public async Task<IActionResult> Edit(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null) return NotFound();

        ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", city.CountryId);
        return View(city);
    }

    // Handle edit form submission
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
    #endregion

    #region Delete City
    // Show delete confirmation page
    public async Task<IActionResult> Delete(int id)
    {
        var city = await _context.Cities
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (city == null) return NotFound();

        return View(city);
    }

    // Handle delete confirmation
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        _context.Cities.Remove(city!);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Export Cities as PDF
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
    #endregion

    #region Export Cities as Excel
    [Authorize] // optional
    public IActionResult ExportExcel()
    {
        // Get cities with country
        var cities = _context.Cities
                             .Include(c => c.Country)
                             .OrderBy(c => c.Name)
                             .ToList();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Cities");

        // Title
        worksheet.Cells["A1:C1"].Merge = true;
        worksheet.Cells["A1"].Value = "City Reports";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        // Column headers
        worksheet.Cells["A3"].Value = "Sr #";
        worksheet.Cells["B3"].Value = "City Name";
        worksheet.Cells["C3"].Value = "Country Name";

        worksheet.Cells["A3:C3"].Style.Font.Bold = true;
        worksheet.Cells["A3:C3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A3:C3"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        worksheet.Cells["A3:C3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // Data rows
        int row = 4;
        int sr = 1;

        foreach (var city in cities)
        {
            worksheet.Cells[row, 1].Value = sr++;
            worksheet.Cells[row, 2].Value = city.Name;
            worksheet.Cells[row, 3].Value = city.Country?.Name;

            worksheet.Cells[row, 1, row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        var excelBytes = package.GetAsByteArray();

        return File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "City_Report.xlsx"
        );
    }
    #endregion

    #region Import Cities from Excel
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
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    string cityName = worksheet.Cells[row, 1].Text.Trim();
                    string countryName = worksheet.Cells[row, 2].Text.Trim();

                    // Validate empty
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

                    // Find Country
                    var country = _context.Countries
                        .FirstOrDefault(c => c.Name.ToLower() == countryName.ToLower());

                    if (country == null)
                    {
                        errors.Add($"Row {row}: Country '{countryName}' does not exist");
                        continue;
                    }

                    // Check duplicate city
                    bool cityExists = _context.Cities
                        .Any(c => c.Name.ToLower() == cityName.ToLower() && c.CountryId == country.Id);

                    if (cityExists)
                    {
                        errors.Add($"Row {row}: City '{cityName}' already exists in country '{countryName}'");
                        continue;
                    }

                    // Save new city
                    var newCity = new City
                    {
                        Name = cityName,
                        CountryId = country.Id
                    };
                    _context.Cities.Add(newCity);
                    createdCount++;
                }

                await _context.SaveChangesAsync();
            }
        }

        TempData["Success"] = $"{createdCount} cities imported successfully.";
        if (errors.Count > 0)
            TempData["Errors"] = string.Join("<br/>", errors);

        return RedirectToAction(nameof(Index));
    }
    #endregion
}
