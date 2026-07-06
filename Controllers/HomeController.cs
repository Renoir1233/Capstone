using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Capstone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // If user is authenticated, check if they are an employee
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                
                // Get the user's ID from Identity
                var userId = _db.Users
                    .Where(u => u.UserName == username)
                    .Select(u => u.Id)
                    .FirstOrDefault();

                _logger.LogInformation($"Authenticated user: {username}, UserId: {userId}");

                if (userId != null)
                {
                    // Check if this user is linked to an employee record
                    var employee = await _db.Employees
                        .FirstOrDefaultAsync(e => e.UserId == userId);

                    _logger.LogInformation($"Employee check for userId {userId}: {(employee != null ? $"Found - {employee.Name}" : "Not found")}");

                    if (employee != null)
                    {
                        // This is an employee - redirect to employee portal
                        _logger.LogInformation($"Redirecting {username} to Employee Portal");
                        return RedirectToAction("Dashboard", "EmployeePortal");
                    }
                }

                // Not an employee - show admin dashboard
                _logger.LogInformation($"User {username} accessing admin dashboard");
                return View();
            }

            // Not authenticated - show login
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
