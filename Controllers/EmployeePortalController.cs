using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Authorize]
    public class EmployeePortalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeePortalController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: EmployeePortal/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            // Pass employee data to the view
            ViewBag.EmployeeId = employee.Id;
            ViewBag.EmployeeName = employee.Name;
            ViewBag.EmployeeNumber = employee.EmployeeNumber;
            ViewBag.Department = employee.Department;
            ViewBag.Position = employee.Position;
            ViewBag.EmployeeEmail = employee.Email;

            return View();
        }

        // GET: EmployeePortal/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            return View(employee);
        }

        // GET: EmployeePortal/MedicalRecords
        public async Task<IActionResult> MedicalRecords()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .Include(e => e.Visits)
                    .ThenInclude(v => v.Prescriptions)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            ViewBag.EmployeeName = employee.Name;
            return View(employee);
        }

        // GET: EmployeePortal/Prescriptions
        public async Task<IActionResult> Prescriptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .Include(e => e.Visits)
                    .ThenInclude(v => v.Prescriptions)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            var prescriptions = employee.Visits
                .SelectMany(v => v.Prescriptions)
                .OrderByDescending(p => p.PrescribedDate)
                .ToList();

            ViewBag.EmployeeName = employee.Name;
            return View(prescriptions);
        }

        // GET: EmployeePortal/HealthReminders
        public async Task<IActionResult> HealthReminders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            var reminders = await _db.HealthReminders
                .Where(r => r.EmployeeId == employee.Id)
                .OrderBy(r => r.DueDate)
                .ToListAsync();

            ViewBag.EmployeeName = employee.Name;
            return View(reminders);
        }

        // GET: EmployeePortal/AiAssistant
        public IActionResult AiAssistant()
        {
            return View();
        }

        // GET: EmployeePortal/Settings
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            return View(employee);
        }

        // POST: EmployeePortal/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Employee model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            // Update allowed fields
            employee.PhilHealthId = model.PhilHealthId;
            employee.SSSId = model.SSSId;
            employee.TINId = model.TINId;
            employee.PagIbigId = model.PagIbigId;
            employee.ContactNumber = model.ContactNumber;
            employee.Email = model.Email;
            employee.EmergencyContact = model.EmergencyContact;
            employee.Address = model.Address;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: EmployeePortal/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Settings));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Settings));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Settings));
        }
    }
}
