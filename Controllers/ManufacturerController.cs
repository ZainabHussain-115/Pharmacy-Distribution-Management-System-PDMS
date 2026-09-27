using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers

{
    [AdminOnly]
    public class ManufacturerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManufacturerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var manufacturers = await _context.Manufacturers.ToListAsync();
            return View(manufacturers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manufacturer manufacturer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(manufacturer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Manufacturer added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(manufacturer);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Manufacturer manufacturer)
        {
            if (id != manufacturer.ManufacturerID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(manufacturer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Manufacturer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(manufacturer);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer != null)
            {
                _context.Manufacturers.Remove(manufacturer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Manufacturer deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}