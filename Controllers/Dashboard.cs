using Microsoft.AspNetCore.Mvc;

namespace Able_Tailor_ERP_System.Controllers
{
    public class Dashboard : Controller
    {
        public IActionResult Index()
        {
            return View();
        } 
    }
}
