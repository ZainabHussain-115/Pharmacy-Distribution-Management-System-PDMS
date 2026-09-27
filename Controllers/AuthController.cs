using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDMS.Data;
using PDMS.Filters;
using PDMS.Models;
using System.Security.Cryptography;
using System.Text;

namespace PDMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AuthController(ApplicationDbContext context) { _context = context; }

        // ── LANDING ───────────────────────────────────────────────────
        public IActionResult Landing()
        {
            if (HttpContext.Session.GetString("UserID") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── LOGIN ─────────────────────────────────────────────────────
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserID") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter username and password.";
                return View();
            }

            string hashed = HashPassword(password);

            // Try hashed password first, then plain (for legacy data)
            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Username == username && (u.PasswordHash == hashed || u.PasswordHash == password) && u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "❌ Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetString("UserID",   user.UserID.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role",     user.Role ?? "Staff");

            TempData["Success"] = $"Welcome, {user.FullName}! 👋";
            return RedirectToAction("Index", "Home");
        }

        // ── LOGOUT ────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── ACCESS DENIED ─────────────────────────────────────────────
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ── MANAGE USERS (Admin only) ─────────────────────────────────
        [AdminOnly]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // ── ADD USER (Admin only) ─────────────────────────────────────
        [AdminOnly]
        public IActionResult AddUser() => View();

        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(User user, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Password is required.");
                return View(user);
            }
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("", $"Username '{user.Username}' already exists.");
                return View(user);
            }

            user.PasswordHash = HashPassword(password);
            user.CreatedAt    = DateTime.Now;
            user.IsActive     = true;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' created as {user.Role}.";
            return RedirectToAction("ManageUsers");
        }

        // ── TOGGLE ACTIVE/INACTIVE (Admin only) ──────────────────────
        [AdminOnly]
        [HttpPost]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Prevent admin from disabling themselves
                if (user.UserID.ToString() == HttpContext.Session.GetString("UserID"))
                {
                    TempData["Error"] = "You cannot disable your own account.";
                    return RedirectToAction("ManageUsers");
                }
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"'{user.FullName}' {(user.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction("ManageUsers");
        }

        // ── RESET PASSWORD (Admin only) ───────────────────────────────
        [AdminOnly]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Password reset for '{user.FullName}'.";
            }
            return RedirectToAction("ManageUsers");
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
