using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
