using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Faculty") || User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Faculty");
                }
                else if (User.IsInRole("Student"))
                {
                    return RedirectToAction("Dashboard", "Student");
                }
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
