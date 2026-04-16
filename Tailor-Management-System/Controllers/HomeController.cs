using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tailor_Management_System.Data;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TailorDbContext _context;

        public HomeController(ILogger<HomeController> logger, TailorDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        private IActionResult RequireAuth()
        {
            if (HttpContext.Session.GetString("token") == null)
                return RedirectToAction("Login", "Auth");
            return null!;
        }

        [Route("")]
        [Route("index.html")] // Handle old React entrance URL
        public IActionResult Index()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        [Route("dashboard")] // Alias to handle React's old route
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index");
        }

        public IActionResult Orders()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Customers()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Inventory()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Income()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Appointment()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Tailors()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Profile()
        {
            var redirect = RequireAuth(); if (redirect != null) return redirect;
            return View();
        }

        public IActionResult Privacy() => View();

        // Session-based notifications endpoint – never returns 401 due to expired JWT
        [HttpGet("/Home/Notifications")]
        public async Task<IActionResult> Notifications()
        {
            var userIdStr = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Json(new { error = "Not authenticated" });

            var pending = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.UserId == userId && (o.Status == "Pending" || o.Status == "In Progress"))
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new
                {
                    id = o.Id,
                    customerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                    services = o.Services,
                    status = o.Status,
                    createdAt = o.CreatedAt
                })
                .ToListAsync();

            return Json(pending);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
