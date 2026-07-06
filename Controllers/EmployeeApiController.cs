using Capstone.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Authorize]
    [Route("api/employee")]
    [ApiController]
    public class EmployeeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeeApiController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: api/employee/check-link (Diagnostic endpoint)
        [HttpGet("check-link")]
        public async Task<IActionResult> CheckEmployeeLink()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { message = "User not authenticated" });

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            var allEmployees = await _db.Employees
                .Select(e => new { e.EmployeeNumber, e.Name, e.UserId })
                .ToListAsync();

            return Ok(new
            {
                currentUser = new { user.Id, user.UserName, user.Email },
                linkedEmployee = employee != null ? new { employee.Id, employee.EmployeeNumber, employee.Name, employee.UserId } : null,
                isLinked = employee != null,
                allEmployees = allEmployees
            });
        }

        // POST: api/employee/fix-link (Manual fix endpoint - remove in production)
        [HttpPost("fix-link")]
        public async Task<IActionResult> FixEmployeeLink()
        {
            var user = await _userManager.FindByNameAsync("Employee");
            if (user == null)
            {
                return NotFound(new { message = "Employee user account not found" });
            }

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == "EMP-001");
            if (employee == null)
            {
                return NotFound(new { message = "Employee record EMP-001 not found" });
            }

            // Link the employee to the user
            employee.UserId = user.Id;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Employee link fixed successfully",
                employee = new { employee.Id, employee.EmployeeNumber, employee.Name, employee.UserId },
                user = new { user.Id, user.UserName }
            });
        }

        // GET: api/employee/dashboard-summary
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var employee = await _db.Employees
                .Include(e => e.Visits)
                    .ThenInclude(v => v.Prescriptions)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee profile not found." });
            }

            var totalVisits = employee.Visits.Count;
            var recentVisit = employee.Visits.OrderByDescending(v => v.DateOfConsultation).FirstOrDefault();
            var activePrescriptions = employee.Visits
                .SelectMany(v => v.Prescriptions)
                .Where(p => !p.IsDispensed)
                .Count();

            var upcomingReminders = await _db.HealthReminders
                .Where(r => r.EmployeeId == employee.Id && !r.IsCompleted && r.DueDate >= DateTime.Now)
                .OrderBy(r => r.DueDate)
                .Take(3)
                .ToListAsync();

            return Ok(new
            {
                totalVisits,
                lastVisitDate = recentVisit?.DateOfConsultation.ToString("MMM dd, yyyy"),
                activePrescriptions,
                upcomingReminders = upcomingReminders.Select(r => new
                {
                    r.ReminderType,
                    r.DueDate,
                    daysUntil = (r.DueDate.Date - DateTime.Now.Date).Days
                })
            });
        }

        // GET: api/employee/recent-consultations
        [HttpGet("recent-consultations")]
        public async Task<IActionResult> GetRecentConsultations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var employee = await _db.Employees
                .Include(e => e.Visits)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee profile not found." });
            }

            var recentConsultations = employee.Visits
                .OrderByDescending(v => v.DateOfConsultation)
                .Take(5)
                .Select(v => new
                {
                    v.Id,
                    v.DateOfConsultation,
                    v.ChiefComplain,
                    v.Outcome,
                    v.QueueStatus
                })
                .ToList();

            return Ok(recentConsultations);
        }

        // GET: api/employee/health-summary
        [HttpGet("health-summary")]
        public async Task<IActionResult> GetHealthSummary()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var employee = await _db.Employees
                .Include(e => e.Visits)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound(new { message = "Employee profile not found." });
            }

            var latestVisit = employee.Visits
                .OrderByDescending(v => v.DateOfConsultation)
                .FirstOrDefault();

            var commonConditions = employee.Visits
                .Where(v => !string.IsNullOrWhiteSpace(v.Outcome))
                .GroupBy(v => v.Outcome)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            return Ok(new
            {
                latestVitals = latestVisit != null ? new
                {
                    bp = latestVisit.VitalBP,
                    pr = latestVisit.VitalPR,
                    temp = latestVisit.VitalTemp,
                    weight = latestVisit.VitalWT,
                    date = latestVisit.DateOfConsultation.ToString("MMM dd, yyyy")
                } : null,
                commonConditions,
                totalConsultations = employee.Visits.Count
            });
        }
    }
}
