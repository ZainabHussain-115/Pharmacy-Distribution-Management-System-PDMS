using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers
{
    
[AdminOnly]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WarehouseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var warehouses = await _context.Warehouses.ToListAsync();
            return View(warehouses);
        }

        // ── THING 2: WAREHOUSE STOCK VIEW ────────────────────────────────
        public async Task<IActionResult> Stock(int? id)
        {
            var warehouses = await _context.Warehouses.ToListAsync();
            ViewBag.Warehouses = warehouses;

            if (id.HasValue)
            {
                var warehouse = await _context.Warehouses.FindAsync(id.Value);
                if (warehouse == null) return NotFound();

                var batches = await _context.Batches
                    .Include(b => b.Medicine)
                        .ThenInclude(m => m.Manufacturer)
                    .Where(b => b.WarehouseID == id.Value)
                    .OrderBy(b => b.Medicine.Name)
                    .ToListAsync();

                // Group by medicine for summary
                var summary = batches
                    .GroupBy(b => new { b.Medicine.MedicineID, b.Medicine.Name, b.Medicine.Category, b.Medicine.UnitType })
                    .Select(g => new
                    {
                        MedicineName     = g.Key.Name,
                        Category         = g.Key.Category,
                        UnitType         = g.Key.UnitType,
                        TotalBatches     = g.Count(),
                        TotalQty         = g.Sum(b => b.QuantityAvailable),
                        HasExpired       = g.Any(b => b.ExpiryDate <= DateTime.Today),
                        HasLowStock      = g.Any(b => b.QuantityAvailable <= 20),
                        EarliestExpiry   = g.Min(b => b.ExpiryDate)
                    }).ToList();

                ViewBag.SelectedWarehouse = warehouse;
                ViewBag.Batches           = batches;
                ViewBag.Summary           = summary;
                ViewBag.TotalMedicines    = summary.Count;
                ViewBag.TotalUnits        = summary.Sum(s => s.TotalQty);
                ViewBag.UsedCapacity      = batches.Sum(b => b.QuantityAvailable);
            }

            return View();
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                _context.Add(warehouse);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Warehouse added!";
                return RedirectToAction(nameof(Index));
            }
            return View(warehouse);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();
            return View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Warehouse warehouse)
        {
            if (id != warehouse.WarehouseID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(warehouse);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Warehouse updated!";
                return RedirectToAction(nameof(Index));
            }
            return View(warehouse);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();
            return View(warehouse);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Warehouse deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
