using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers
{
    [AdminOnly]
    public class MedicineController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Medicine
        public async Task<IActionResult> Index()
        {
            var medicines = await _context.Medicines
                .Include(m => m.Manufacturer)
                .ToListAsync();
            return View(medicines);
        }

        // GET: Medicine/Create
        public IActionResult Create()
        {
            ViewBag.Manufacturers = new SelectList(_context.Manufacturers, "ManufacturerID", "Name");
            return View();
        }

        // POST: Medicine/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medicine added successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Manufacturers = new SelectList(_context.Manufacturers, "ManufacturerID", "Name", medicine.ManufacturerID);
            return View(medicine);
        }

        // GET: Medicine/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            ViewBag.Manufacturers = new SelectList(_context.Manufacturers, "ManufacturerID", "Name", medicine.ManufacturerID);
            return View(medicine);
        }

        // POST: Medicine/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicine medicine)
        {
            if (id != medicine.MedicineID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medicine updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Manufacturers = new SelectList(_context.Manufacturers, "ManufacturerID", "Name", medicine.ManufacturerID);
            return View(medicine);
        }

        // GET: Medicine/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines
                .Include(m => m.Manufacturer)
                .FirstOrDefaultAsync(m => m.MedicineID == id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        // POST: Medicine/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medicine deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
