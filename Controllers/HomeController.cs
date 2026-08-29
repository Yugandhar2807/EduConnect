using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Faculty"))
                    return RedirectToAction("Dashboard", "Faculty");
                if (User.IsInRole("Student"))
                    return RedirectToAction("Dashboard", "Student");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.StatusCode = statusCode;
            ViewBag.Message = statusCode switch
            {
                404 => "The page you are looking for could not be found.",
                403 => "You do not have permission to access this resource.",
                _ => "An unexpected error occurred while processing your request.",
            };
            return View();
        }
    }
}
