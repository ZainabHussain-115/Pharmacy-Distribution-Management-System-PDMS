using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers
{[RequireLogin] 
    public class SaleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SaleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sale
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            return View(sales);
        }

        // GET: Sale/Create
        public IActionResult Create()
        {
            ViewBag.Customers = new SelectList(_context.Customers, "CustomerID", "Name");
            ViewBag.Users     = new SelectList(_context.Users,     "UserID",     "FullName");

            var availableBatches = _context.Batches
                .Include(b => b.Medicine)
                .Where(b => b.ExpiryDate > DateTime.Today && b.QuantityAvailable > 0)
                .Select(b => new {
                    b.BatchID,
                    Display = b.Medicine.Name + " | Batch: " + b.BatchNumber +
                              " | Qty: " + b.QuantityAvailable +
                              " | Exp: " + b.ExpiryDate.ToString("yyyy-MM-dd") +
                              " | Price: Rs." + b.Medicine.SellingPrice,
                    b.Medicine.SellingPrice
                }).ToList();

            ViewBag.Batches = new SelectList(availableBatches, "BatchID", "Display");
            return View();
        }

        // POST: Sale/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sale sale, int[] BatchIDs, int[] Quantities)
        {
            if (BatchIDs == null || BatchIDs.Length == 0)
                ModelState.AddModelError("", "Please add at least one medicine to the sale.");

            decimal totalAmount = 0;
            var saleDetails = new List<SaleDetail>();

            if (BatchIDs != null)
            {
                for (int i = 0; i < BatchIDs.Length; i++)
                {
                    var batch = await _context.Batches
                        .Include(b => b.Medicine)
                        .FirstOrDefaultAsync(b => b.BatchID == BatchIDs[i]);

                    if (batch == null) { ModelState.AddModelError("", "Batch not found."); continue; }

                    // ❌ Block expired medicine
                    if (batch.ExpiryDate <= DateTime.Today)
                    {
                        ModelState.AddModelError("", $"❌ '{batch.Medicine.Name}' (Batch {batch.BatchNumber}) is expired.");
                        continue;
                    }

                    // ❌ Block overselling
                    if (Quantities[i] > batch.QuantityAvailable)
                    {
                        ModelState.AddModelError("", $"❌ Not enough stock for '{batch.Medicine.Name}'. Available: {batch.QuantityAvailable}, Requested: {Quantities[i]}");
                        continue;
                    }

                    decimal unitPrice = batch.Medicine.SellingPrice ?? 0;
                    decimal subTotal  = unitPrice * Quantities[i];
                    totalAmount      += subTotal;

                    saleDetails.Add(new SaleDetail
                    {
                        BatchID      = BatchIDs[i],
                        QuantitySold = Quantities[i],
                        UnitPrice    = unitPrice,
                        SubTotal     = subTotal
                    });
                }
            }

            // ❌ CREDIT LIMIT CHECK (Thing 4)
            if (sale.PaymentMethod == "Credit" && saleDetails.Count > 0)
            {
                var customer = await _context.Customers.FindAsync(sale.CustomerID);
                if (customer != null && customer.CreditLimit.HasValue)
                {
                    decimal netAmount = totalAmount - sale.Discount + sale.TaxAmount;

                    // Get total existing pending/credit sales for this customer
                    decimal existingCredit = await _context.Sales
                        .Where(s => s.CustomerID == sale.CustomerID
                                 && s.PaymentMethod == "Credit"
                                 && s.PaymentStatus == "Pending")
                        .SumAsync(s => (decimal?)s.NetAmount) ?? 0;

                    if (existingCredit + netAmount > customer.CreditLimit.Value)
                    {
                        decimal available = customer.CreditLimit.Value - existingCredit;
                        ModelState.AddModelError("",
                            $"❌ Credit limit exceeded for '{customer.Name}'. " +
                            $"Credit Limit: Rs.{customer.CreditLimit.Value:N0} | " +
                            $"Already Used: Rs.{existingCredit:N0} | " +
                            $"Available: Rs.{(available < 0 ? 0 : available):N0} | " +
                            $"This Sale: Rs.{netAmount:N0}");
                    }
                }
            }

            if (ModelState.IsValid && saleDetails.Count > 0)
            {
                sale.TotalAmount = totalAmount;
                sale.NetAmount   = totalAmount - sale.Discount + sale.TaxAmount;
                sale.SaleDate    = DateTime.Now;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                foreach (var detail in saleDetails)
                {
                    detail.SaleID = sale.SaleID;
                    _context.SaleDetails.Add(detail);

                    var batch = await _context.Batches.FindAsync(detail.BatchID);
                    if (batch != null)
                        batch.QuantityAvailable -= detail.QuantitySold;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Sale #{sale.SaleID} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns on error
            ViewBag.Customers = new SelectList(_context.Customers, "CustomerID", "Name", sale.CustomerID);
            ViewBag.Users     = new SelectList(_context.Users,     "UserID",     "FullName", sale.CreatedBy);
            var batches = _context.Batches.Include(b => b.Medicine)
                .Where(b => b.ExpiryDate > DateTime.Today && b.QuantityAvailable > 0)
                .Select(b => new {
                    b.BatchID,
                    Display = b.Medicine.Name + " | Batch: " + b.BatchNumber +
                              " | Qty: " + b.QuantityAvailable + " | Price: Rs." + b.Medicine.SellingPrice
                }).ToList();
            ViewBag.Batches = new SelectList(batches, "BatchID", "Display");
            return View(sale);
        }

        // ── THING 1: UPDATE PAYMENT STATUS ───────────────────────────────

        // GET: Sale/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SaleID == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        // POST: Sale/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string PaymentStatus, string PaymentMethod)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null) return NotFound();

            sale.PaymentStatus = PaymentStatus;
            sale.PaymentMethod = PaymentMethod;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Sale #{id} updated to {PaymentStatus}!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Sale/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Batch)
                        .ThenInclude(b => b.Medicine)
                .FirstOrDefaultAsync(s => s.SaleID == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        // GET: Sale/Delete/5
        [AdminOnly] 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SaleID == id);
            if (sale == null) return NotFound();
            return View(sale);
        }

        // POST: Sale/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnly] 
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale != null)
            {
                // Restore stock
                foreach (var detail in sale.SaleDetails)
                {
                    var batch = await _context.Batches.FindAsync(detail.BatchID);
                    if (batch != null)
                        batch.QuantityAvailable += detail.QuantitySold;
                }
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sale deleted and stock restored.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
