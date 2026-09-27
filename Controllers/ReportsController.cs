using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Filters;

namespace PDMS.Controllers
{
    [RequireLogin]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportsController(ApplicationDbContext context) { _context = context; }

        // ── REPORTS INDEX ─────────────────────────────────────────────
        // Shows different report cards based on role
        public IActionResult Index()
        {
            ViewBag.Role = HttpContext.Session.GetString("Role") ?? "Staff";
            return View();
        }

        // ── ADMIN ONLY REPORTS ────────────────────────────────────────

        [AdminOnly]  // 💰 Profit is sensitive — Admin only
        public async Task<IActionResult> OverallProfit()
        {
            var totalRevenue = await _context.Sales
                .SumAsync(s => (decimal?)s.NetAmount) ?? 0;
            var totalCost = await _context.SaleDetails
                .Include(sd => sd.Batch).ThenInclude(b => b.Medicine)
                .SumAsync(sd => (decimal?)(sd.QuantitySold * sd.Batch.Medicine.PurchasePrice)) ?? 0;
            var totalProfit  = totalRevenue - totalCost;

            var monthly = await _context.Sales
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month,
                    Revenue = g.Sum(x => x.NetAmount), SalesCount = g.Count() })
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                .Take(12).ToListAsync();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalCost    = totalCost;
            ViewBag.TotalProfit  = totalProfit;
            ViewBag.ProfitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue * 100) : 0;
            ViewBag.Monthly      = monthly;
            return View();
        }

        [AdminOnly]  // 👑 Customer of month — Admin only
        public async Task<IActionResult> CustomerOfMonth(int? month, int? year)
        {
            month ??= DateTime.Now.Month;
            year  ??= DateTime.Now.Year;
            var data = await _context.Sales
                .Include(s => s.Customer)
                .Where(s => s.SaleDate.Month == month && s.SaleDate.Year == year)
                .GroupBy(s => new { s.Customer.CustomerID, s.Customer.Name })
                .Select(g => new { CustomerID = g.Key.CustomerID, CustomerName = g.Key.Name,
                    TotalOrders = g.Count(), TotalSpent = g.Sum(x => x.NetAmount) })
                .OrderByDescending(x => x.TotalSpent).ToListAsync();
            ViewBag.Month = month; ViewBag.Year = year;
            ViewBag.ReportData = data; ViewBag.TopCustomer = data.FirstOrDefault();
            return View();
        }

        [AdminOnly]  // 📊 Stock valuation — Admin only
        public async Task<IActionResult> StockValuation()
        {
            var data = await _context.Batches
                .Include(b => b.Medicine).Include(b => b.Warehouse)
                .Where(b => b.QuantityAvailable > 0)
                .Select(b => new {
                    MedicineName = b.Medicine.Name, BatchNumber = b.BatchNumber,
                    WarehouseName = b.Warehouse.Name, QuantityAvailable = b.QuantityAvailable,
                    PurchasePrice = b.Medicine.PurchasePrice, SellingPrice = b.Medicine.SellingPrice,
                    CostValue  = b.QuantityAvailable * b.Medicine.PurchasePrice,
                    SaleValue  = b.QuantityAvailable * b.Medicine.SellingPrice,
                    ExpiryDate = b.ExpiryDate
                }).OrderBy(b => b.MedicineName).ToListAsync();
            ViewBag.ReportData     = data;
            ViewBag.TotalCostValue = data.Sum(x => x.CostValue);
            ViewBag.TotalSaleValue = data.Sum(x => x.SaleValue);
            ViewBag.PotentialProfit = data.Sum(x => x.SaleValue) - data.Sum(x => x.CostValue);
            return View();
        }

        [AdminOnly]  // 📅 Full date range sales — Admin only
        public async Task<IActionResult> SalesByDate(DateTime? from, DateTime? to)
        {
            from ??= DateTime.Today.AddDays(-30);
            to   ??= DateTime.Today;
            var sales = await _context.Sales
                .Include(s => s.Customer).Include(s => s.User)
                .Where(s => s.SaleDate.Date >= from.Value.Date && s.SaleDate.Date <= to.Value.Date)
                .OrderByDescending(s => s.SaleDate).ToListAsync();
            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To   = to.Value.ToString("yyyy-MM-dd");
            ViewBag.Sales = sales;
            ViewBag.TotalRevenue = sales.Sum(s => s.NetAmount);
            ViewBag.TotalSales   = sales.Count;
            return View();
        }

        // ── SHARED REPORTS (Admin + Staff) ────────────────────────────

        // Medicine of month — both can see
        public async Task<IActionResult> MedicineOfMonth(int? month, int? year)
        {
            month ??= DateTime.Now.Month;
            year  ??= DateTime.Now.Year;
            var data = await _context.SaleDetails
                .Include(sd => sd.Batch).ThenInclude(b => b.Medicine)
                .Include(sd => sd.Sale)
                .Where(sd => sd.Sale.SaleDate.Month == month && sd.Sale.SaleDate.Year == year)
                .GroupBy(sd => new { sd.Batch.Medicine.MedicineID, sd.Batch.Medicine.Name })
                .Select(g => new { MedicineID = g.Key.MedicineID, MedicineName = g.Key.Name,
                    TotalQtySold = g.Sum(x => x.QuantitySold), TotalRevenue = g.Sum(x => x.SubTotal) })
                .OrderByDescending(x => x.TotalQtySold).ToListAsync();
            ViewBag.Month = month; ViewBag.Year = year;
            ViewBag.ReportData = data; ViewBag.TopMedicine = data.FirstOrDefault();
            return View();
        }

        // Monthly revenue — both can see
        public async Task<IActionResult> MonthlyRevenue(int? year)
        {
            year ??= DateTime.Now.Year;
            var data = await _context.Sales
                .Where(s => s.SaleDate.Year == year)
                .GroupBy(s => s.SaleDate.Month)
                .Select(g => new { Month = g.Key, TotalSales = g.Count(),
                    TotalRevenue = g.Sum(x => x.NetAmount), TotalDiscount = g.Sum(x => (decimal?)x.Discount) })
                .OrderBy(x => x.Month).ToListAsync();
            ViewBag.Year = year;
            ViewBag.ReportData = data;
            ViewBag.TotalYearRevenue = data.Sum(x => x.TotalRevenue);
            return View();
        }

        // Low stock — both can see (important for staff restocking)
        public async Task<IActionResult> LowStock()
        {
            var data = await _context.Batches
                .Include(b => b.Medicine).Include(b => b.Warehouse)
                .Where(b => b.QuantityAvailable <= b.Medicine.ReorderLevel && b.QuantityAvailable > 0)
                .OrderBy(b => b.QuantityAvailable).ToListAsync();
            ViewBag.ReportData = data;
            return View();
        }

        // Expiry report — both can see
        public async Task<IActionResult> ExpiryReport(int? days)
        {
            days ??= 90;
            var cutoff = DateTime.Today.AddDays(days.Value);
            var expiring = await _context.Batches.Include(b => b.Medicine).Include(b => b.Warehouse)
                .Where(b => b.ExpiryDate <= cutoff && b.ExpiryDate > DateTime.Today)
                .OrderBy(b => b.ExpiryDate).ToListAsync();
            var expired = await _context.Batches.Include(b => b.Medicine).Include(b => b.Warehouse)
                .Where(b => b.ExpiryDate <= DateTime.Today && b.QuantityAvailable > 0)
                .OrderBy(b => b.ExpiryDate).ToListAsync();
            ViewBag.Days = days; ViewBag.Expiring = expiring; ViewBag.Expired = expired;
            return View();
        }

        // Best selling — both can see
        public async Task<IActionResult> BestSelling()
        {
            var data = await _context.SaleDetails
                .Include(sd => sd.Batch).ThenInclude(b => b.Medicine).ThenInclude(m => m.Manufacturer)
                .GroupBy(sd => new { sd.Batch.Medicine.MedicineID, sd.Batch.Medicine.Name,
                    ManufacturerName = sd.Batch.Medicine.Manufacturer.Name })
                .Select(g => new { MedicineID = g.Key.MedicineID, MedicineName = g.Key.Name,
                    ManufacturerName = g.Key.ManufacturerName,
                    TotalQtySold = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.SubTotal), TimesSold = g.Count() })
                .OrderByDescending(x => x.TotalQtySold).Take(10).ToListAsync();
            ViewBag.ReportData = data;
            return View();
        }

        // Customer history — Staff sees only their customers' sales
        public async Task<IActionResult> CustomerHistory(int? customerId)
        {
            string role = HttpContext.Session.GetString("Role") ?? "Staff";
            var customers = await _context.Customers.ToListAsync();
            ViewBag.Customers = customers;
            ViewBag.Role = role;

            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                var salesQuery = _context.Sales
                    .Include(s => s.SaleDetails).ThenInclude(sd => sd.Batch).ThenInclude(b => b.Medicine)
                    .Where(s => s.CustomerID == customerId.Value);

                // Staff only sees their own sales for this customer
                if (role == "Staff")
                {
                    int uid = int.Parse(HttpContext.Session.GetString("UserID")!);
                    salesQuery = salesQuery.Where(s => s.CreatedBy == uid);
                }

                var sales = await salesQuery.OrderByDescending(s => s.SaleDate).ToListAsync();
                ViewBag.SelectedCustomer = customer;
                ViewBag.Sales            = sales;
                ViewBag.TotalSpent       = sales.Sum(s => s.NetAmount);
                ViewBag.TotalOrders      = sales.Count;
            }
            return View();
        }
    }
}
