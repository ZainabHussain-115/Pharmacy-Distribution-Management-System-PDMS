using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Models;
using PDMS.Filters;

namespace PDMS.Controllers
{
    [RequireLogin] 
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
        }
        [AdminOnly]    
        public IActionResult Create()
        {
            return View();
        }
        [AdminOnly]    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
        [AdminOnly]    
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }
        [AdminOnly]  
        [HttpPost]
        [ValidateAntiForgeryToken]  
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer updated!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
        [AdminOnly]    
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnly]    
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
