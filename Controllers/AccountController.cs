using Microsoft.AspNetCore.Mvc;

namespace Able_Tailor_ERP_System.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
