using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Models;

[Authorize(Roles = "Principal")]
public class CityController : Controller
{
    private readonly ApplicationDbContext _context;

    public CityController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= INDEX =================
    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities
            .Include(c => c.Country)
            .ToListAsync();

        return View(cities);
    }

    // ================= CREATE =================
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
}
