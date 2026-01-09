using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Services;
using SkiaSharp;
using System.Drawing;


namespace SchoolManagementSystem.Controllers
{
    [Authorize(Roles = "Principal")]
    public class CountryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CountryPdfService _pdfService;

        public CountryController(ApplicationDbContext context, CountryPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: Country
        public async Task<IActionResult> Index()
        {
            var countries = await _context.Countries.ToListAsync();
            return View(countries);
        }

        // GET: Country/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Country/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Country country)
        //{
        //    if (!ModelState.IsValid)
        //        return View(country);

        //    _context.Countries.Add(country);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Country country)
        {
            if (!ModelState.IsValid)
                return View(country);

            // Check duplicate
            bool exists = await _context.Countries
                .AnyAsync(c => c.Name.ToLower() == country.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "This country already exists.");
                return View(country);
            }

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Country/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
                return NotFound();

            return View(country);
        }

        // POST: Country/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Country country)
        {
            if (!ModelState.IsValid)
                return View(country);

            _context.Countries.Update(country);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Country/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
                return NotFound();

            return View(country);
        }

        // POST: Country/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var country = await _context.Countries.FindAsync(id);

            _context.Countries.Remove(country!);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> ExportPdf()
        {
            var countries = await _context.Countries.ToListAsync();

            var pdfBytes = _pdfService.GenerateCountryPdf(countries);

            return File(
                pdfBytes,
                "application/pdf",
                "Countries_Report.pdf"
            );
        }


        //[Authorize] // 
        public IActionResult ExportExcel()
        {
            // 1️⃣ Get data from database
            var countries = _context.Countries
                                    .OrderBy(c => c.Name)
                                    .ToList();

            // 2️⃣ Create Excel package
            using var package = new ExcelPackage();

            // 3️⃣ Add worksheet
            var worksheet = package.Workbook.Worksheets.Add("Countries");

            // 4️⃣ Title
            worksheet.Cells["A1:B1"].Merge = true;
            worksheet.Cells["A1"].Value = "Country Reports";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // 5️⃣ Column Headers
            worksheet.Cells["A3"].Value = "Sr #";
            worksheet.Cells["B3"].Value = "Country Name";

            worksheet.Cells["A3:B3"].Style.Font.Bold = true;
            worksheet.Cells["A3:B3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A3:B3"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            worksheet.Cells["A3:B3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            // 6️⃣ Data Rows
            int row = 4;
            int sr = 1;

            foreach (var country in countries)
            {
                worksheet.Cells[row, 1].Value = sr++;
                worksheet.Cells[row, 2].Value = country.Name;

                worksheet.Cells[row, 1, row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                row++;
            }

            // 7️⃣ Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 8️⃣ Return file for download
            var excelBytes = package.GetAsByteArray();

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Country_Report.xlsx"
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

                for (int row = 2; row <= rowCount; row++) // start from row 2, assuming row 1 is header
                {
                    string countryName = worksheet.Cells[row, 1].Text.Trim();

                    // 1️⃣ Check if empty
                    if (string.IsNullOrEmpty(countryName))
                    {
                        errors.Add($"Row {row}: Country Name is empty");
                        continue;
                    }

                    // 2️⃣ Check duplicate
                    bool exists = _context.Countries
                        .Any(c => c.Name.ToLower() == countryName.ToLower());

                    if (exists)
                    {
                        errors.Add($"Row {row}: Country '{countryName}' already exists");
                        continue;
                    }

                    // 3️⃣ Save new country
                    _context.Countries.Add(new Country { Name = countryName });
                    createdCount++;
                }

                await _context.SaveChangesAsync();
            }
        }

        // 4️⃣ Show summary to user
        TempData["Success"] = $"{createdCount} countries imported successfully.";
        if (errors.Count > 0)
            TempData["Errors"] = string.Join("<br/>", errors);

        return RedirectToAction(nameof(Index));
    }

}
}
