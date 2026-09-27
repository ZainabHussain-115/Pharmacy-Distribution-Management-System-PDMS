using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Filters;

namespace PDMS.Controllers
{
    [RequireLogin]  // All dashboard actions require login
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            string role = HttpContext.Session.GetString("Role") ?? "Staff";

            // Basic stats shown to everyone
            ViewBag.TotalMedicines     = await _context.Medicines.CountAsync();
            ViewBag.TotalCustomers     = await _context.Customers.CountAsync();
            ViewBag.TotalSales         = await _context.Sales.CountAsync();
            ViewBag.TotalManufacturers = await _context.Manufacturers.CountAsync();
            ViewBag.Role               = role;

            // Revenue only shown to Admin
            if (role == "Admin")
            {
                ViewBag.TotalRevenue = await _context.Sales
                    .SumAsync(s => (decimal?)s.NetAmount) ?? 0;

                ViewBag.TotalProfit = await _context.SaleDetails
                    .Include(sd => sd.Batch).ThenInclude(b => b.Medicine)
                    .SumAsync(sd => (decimal?)(sd.QuantitySold *
                        (sd.UnitPrice - sd.Batch.Medicine.PurchasePrice))) ?? 0;
            }

            // Low stock — shown to both
            ViewBag.LowStockBatches = await _context.Batches
                .Include(b => b.Medicine)
                .Include(b => b.Warehouse)
                .Where(b => b.QuantityAvailable <= 20 && b.QuantityAvailable > 0)
                .ToListAsync();

            // Expiring soon — shown to both
            var soon = DateTime.Today.AddDays(90);
            ViewBag.ExpiringBatches = await _context.Batches
                .Include(b => b.Medicine)
                .Where(b => b.ExpiryDate <= soon && b.ExpiryDate > DateTime.Today)
                .OrderBy(b => b.ExpiryDate)
                .Take(10)
                .ToListAsync();

            // Recent sales — Admin sees all, Staff sees only their own
            if (role == "Admin")
            {
                ViewBag.RecentSales = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.User)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                int uid = int.Parse(HttpContext.Session.GetString("UserID")!);
                ViewBag.RecentSales = await _context.Sales
                    .Include(s => s.Customer)
                    .Where(s => s.CreatedBy == uid)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();
            }

            return View();
        }

        public IActionResult Error() => View();
    }
}
