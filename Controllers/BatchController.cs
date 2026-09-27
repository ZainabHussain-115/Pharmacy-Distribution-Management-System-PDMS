using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers
{[RequireLogin] 
    public class BatchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BatchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            var batches = await _context.Batches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .ToListAsync();

            return View(batches);
        }

        // =========================
        // CREATE GET
        // =========================
        public IActionResult Create()
        {
            ViewBag.Medicines = new SelectList(
                _context.Medicines,
                "MedicineID",
                "Name"
            );

            ViewBag.Warehouses = new SelectList(
                _context.Warehouses,
                "WarehouseID",
                "Name"
            );

            return View();
        }

        // =========================
        // CREATE POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Batch batch)
        {
            // Expiry must be after manufacturing
            if (batch.ExpiryDate <= batch.ManufacturingDate)
            {
                ModelState.AddModelError(
                    "ExpiryDate",
                    "Expiry date must be after manufacturing date."
                );
            }

            // Expiry cannot be in past
            if (batch.ExpiryDate < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ExpiryDate",
                    "Cannot add an expired batch."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Initial stock
                    batch.QuantityAvailable = batch.QuantityReceived;

                    // Purchase date auto
                    batch.PurchaseDate = DateTime.Today;

                    _context.Batches.Add(batch);

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Batch added successfully!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            ViewBag.Medicines = new SelectList(
                _context.Medicines,
                "MedicineID",
                "Name",
                batch.MedicineID
            );

            ViewBag.Warehouses = new SelectList(
                _context.Warehouses,
                "WarehouseID",
                "Name",
                batch.WarehouseID
            );

            return View(batch);
        }

        // =========================
        // EDIT GET
        // =========================
        [AdminOnly]    
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var batch = await _context.Batches.FindAsync(id);

            if (batch == null)
                return NotFound();

            ViewBag.Medicines = new SelectList(
                _context.Medicines,
                "MedicineID",
                "Name",
                batch.MedicineID
            );

            ViewBag.Warehouses = new SelectList(
                _context.Warehouses,
                "WarehouseID",
                "Name",
                batch.WarehouseID
            );

            return View(batch);
        }

        // =========================
        // EDIT POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]    
        public async Task<IActionResult> Edit(int id, Batch batch)
        {
            if (id != batch.BatchID)
                return NotFound();

            // Expiry after manufacturing
            if (batch.ExpiryDate <= batch.ManufacturingDate)
            {
                ModelState.AddModelError(
                    "ExpiryDate",
                    "Expiry date must be after manufacturing date."
                );
            }

            // Expiry cannot be in past
            if (batch.ExpiryDate < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ExpiryDate",
                    "Cannot set expiry date in the past."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(batch);

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Batch updated successfully!";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            ViewBag.Medicines = new SelectList(
                _context.Medicines,
                "MedicineID",
                "Name",
                batch.MedicineID
            );

            ViewBag.Warehouses = new SelectList(
                _context.Warehouses,
                "WarehouseID",
                "Name",
                batch.WarehouseID
            );

            return View(batch);
        }

        // =========================
        // DELETE GET
        // =========================
        [AdminOnly]    
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var batch = await _context.Batches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .FirstOrDefaultAsync(b => b.BatchID == id);

            if (batch == null)
                return NotFound();

            return View(batch);
        }

        // =========================
        // DELETE POST
        // =========================
        [AdminOnly]    
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var batch = await _context.Batches.FindAsync(id);

            if (batch != null)
            {
                _context.Batches.Remove(batch);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Batch deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}