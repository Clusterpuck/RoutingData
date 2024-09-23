using Microsoft.AspNetCore.Mvc;

namespace RoutingData.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
