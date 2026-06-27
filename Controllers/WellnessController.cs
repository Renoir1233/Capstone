using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Route("api/wellness")]
    [ApiController]
    public class WellnessController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public WellnessController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/wellness/programs
        [HttpGet("programs")]
        public async Task<IActionResult> GetPrograms([FromQuery] string? status)
        {
            var query = _db.WellnessPrograms
                .Include(w => w.Registrations)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(w => w.Status == status);
            }

            var programs = await query
                .OrderByDescending(w => w.ProgramDate)
                .Select(w => new
                {
                    w.Id,
                    w.Title,
                    w.Category,
                    w.Venue,
                    w.ProgramDate,
                    w.TargetDepartment,
                    w.ParticipantCapacity,
                    w.Description,
                    w.Status,
                    w.CreatedBy,
                    w.CreatedAt,
                    w.CompletedAt,
                    RegisteredCount = w.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended"),
                    AttendedCount = w.Registrations.Count(r => r.Status == "Attended"),
                    AvailableSlots = w.ParticipantCapacity - w.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended")
                })
                .ToListAsync();

            return Ok(programs);
        }

        // GET api/wellness/programs/{id}
        [HttpGet("programs/{id}")]
        public async Task<IActionResult> GetProgram(int id)
        {
            var program = await _db.WellnessPrograms
                .Include(w => w.Registrations)
                    .ThenInclude(r => r.Employee)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (program == null) return NotFound();

            return Ok(new
            {
                program.Id,
                program.Title,
                program.Category,
                program.Venue,
                program.ProgramDate,
                program.TargetDepartment,
                program.ParticipantCapacity,
                program.Description,
                program.Status,
                program.CreatedBy,
                program.CreatedAt,
                program.CompletedAt,
                Registrations = program.Registrations.Select(r => new
                {
                    r.Id,
                    r.EmployeeId,
                    r.EmployeeName,
                    r.Department,
                    r.Status,
                    r.RegisteredAt,
                    r.AttendedAt,
                    r.Notes,
                    Employee = r.Employee != null ? new
                    {
                        r.Employee.EmployeeNumber,
                        r.Employee.ContactNumber
                    } : null
                }).ToList()
            });
        }

        // POST api/wellness/programs
        [HttpPost("programs")]
        public async Task<IActionResult> CreateProgram([FromBody] WellnessProgramRequest request)
        {
            var program = new WellnessProgram
            {
                Title = request.Title,
                Category = request.Category,
                Venue = request.Venue,
                ProgramDate = request.ProgramDate,
                TargetDepartment = request.TargetDepartment ?? "All Departments",
                ParticipantCapacity = request.ParticipantCapacity,
                Description = request.Description ?? string.Empty,
                Status = "Open",
                CreatedBy = request.CreatedBy ?? "Admin",
                CreatedAt = DateTime.UtcNow
            };

            _db.WellnessPrograms.Add(program);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, programId = program.Id });
        }

        // PUT api/wellness/programs/{id}
        [HttpPut("programs/{id}")]
        public async Task<IActionResult> UpdateProgram(int id, [FromBody] WellnessProgramRequest request)
        {
            var program = await _db.WellnessPrograms.FindAsync(id);
            if (program == null) return NotFound();

            program.Title = request.Title;
            program.Category = request.Category;
            program.Venue = request.Venue;
            program.ProgramDate = request.ProgramDate;
            program.TargetDepartment = request.TargetDepartment ?? "All Departments";
            program.ParticipantCapacity = request.ParticipantCapacity;
            program.Description = request.Description ?? string.Empty;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DELETE api/wellness/programs/{id}
        [HttpDelete("programs/{id}")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            var program = await _db.WellnessPrograms.FindAsync(id);
            if (program == null) return NotFound();

            _db.WellnessPrograms.Remove(program);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST api/wellness/programs/{id}/register
        [HttpPost("programs/{id}/register")]
        public async Task<IActionResult> RegisterEmployee(int id, [FromBody] RegistrationRequest request)
        {
            var program = await _db.WellnessPrograms
                .Include(w => w.Registrations)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (program == null) return NotFound("Program not found");

            // Check if program is open for registration
            if (program.Status != "Open" && program.Status != "Scheduled")
            {
                return BadRequest("Program is not open for registration");
            }

            // Check if slots are available
            var currentRegistrations = program.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended");
            if (currentRegistrations >= program.ParticipantCapacity)
            {
                return BadRequest("Program is fully booked");
            }

            // Check if employee already registered
            var existingRegistration = await _db.WellnessProgramRegistrations
                .FirstOrDefaultAsync(r => r.WellnessProgramId == id && r.EmployeeId == request.EmployeeId);

            if (existingRegistration != null)
            {
                return BadRequest("Employee is already registered");
            }

            var employee = await _db.Employees.FindAsync(request.EmployeeId);
            if (employee == null) return NotFound("Employee not found");

            var registration = new WellnessProgramRegistration
            {
                WellnessProgramId = id,
                EmployeeId = request.EmployeeId,
                EmployeeName = employee.Name,
                Department = employee.Department,
                Status = "Registered",
                RegisteredAt = DateTime.UtcNow
            };

            _db.WellnessProgramRegistrations.Add(registration);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, registrationId = registration.Id });
        }

        // PATCH api/wellness/registrations/{id}/attendance
        [HttpPatch("registrations/{id}/attendance")]
        public async Task<IActionResult> MarkAttendance(int id, [FromBody] AttendanceRequest request)
        {
            var registration = await _db.WellnessProgramRegistrations.FindAsync(id);
            if (registration == null) return NotFound();

            registration.Status = request.Attended ? "Attended" : "Absent";
            registration.AttendedAt = request.Attended ? DateTime.UtcNow : null;
            registration.Notes = request.Notes ?? string.Empty;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST api/wellness/programs/{id}/complete
        [HttpPost("programs/{id}/complete")]
        public async Task<IActionResult> CompleteProgram(int id)
        {
            var program = await _db.WellnessPrograms
                .Include(w => w.Registrations)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (program == null) return NotFound();

            program.Status = "Completed";
            program.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // GET api/wellness/programs/{id}/report
        [HttpGet("programs/{id}/report")]
        public async Task<IActionResult> GetProgramReport(int id)
        {
            var program = await _db.WellnessPrograms
                .Include(w => w.Registrations)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (program == null) return NotFound();

            var totalRegistered = program.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended");
            var totalAttended = program.Registrations.Count(r => r.Status == "Attended");
            var totalAbsent = program.Registrations.Count(r => r.Status == "Absent");
            var attendanceRate = totalRegistered > 0 ? (double)totalAttended / totalRegistered * 100 : 0;

            // Participation by department
            var departmentStats = program.Registrations
                .Where(r => r.Status == "Attended")
                .GroupBy(r => r.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            return Ok(new
            {
                ProgramInfo = new
                {
                    program.Id,
                    program.Title,
                    program.Category,
                    program.Venue,
                    program.ProgramDate,
                    program.TargetDepartment,
                    program.Status,
                    program.CompletedAt
                },
                Statistics = new
                {
                    TotalRegistered = totalRegistered,
                    TotalAttended = totalAttended,
                    TotalAbsent = totalAbsent,
                    AttendanceRate = Math.Round(attendanceRate, 2),
                    Capacity = program.ParticipantCapacity,
                    CapacityUtilization = Math.Round((double)totalRegistered / program.ParticipantCapacity * 100, 2)
                },
                DepartmentBreakdown = departmentStats,
                Participants = program.Registrations.Select(r => new
                {
                    r.EmployeeName,
                    r.Department,
                    r.Status,
                    r.RegisteredAt,
                    r.AttendedAt
                }).ToList()
            });
        }

        // GET api/wellness/employees/search
        [HttpGet("employees/search")]
        public async Task<IActionResult> SearchEmployees([FromQuery] string? search, [FromQuery] string? dept)
        {
            var query = _db.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dept) && dept != "all")
            {
                query = query.Where(e => e.Department == dept);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Name.Contains(search) || e.EmployeeNumber.Contains(search));
            }

            var employees = await query
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeNumber,
                    e.Name,
                    e.Department
                })
                .Take(20)
                .ToListAsync();

            return Ok(employees);
        }
    }

    // Request DTOs
    public class WellnessProgramRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public DateTime ProgramDate { get; set; }
        public string? TargetDepartment { get; set; }
        public int ParticipantCapacity { get; set; }
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class RegistrationRequest
    {
        public int EmployeeId { get; set; }
    }

    public class AttendanceRequest
    {
        public bool Attended { get; set; }
        public string? Notes { get; set; }
    }
}
