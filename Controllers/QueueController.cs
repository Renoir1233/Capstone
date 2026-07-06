using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Authorize]
    [Route("api/queue")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public QueueController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<bool> IsEmployeeUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            return employee != null;
        }

        // GET api/queue  – today's queue grouped by status
        [HttpGet]
        public async Task<IActionResult> GetQueue()
        {
            if (await IsEmployeeUser())
            {
                return Forbid();
            }
            var today = DateTime.Today;

            var visits = await _db.Visits
                .Where(v => v.DateOfConsultation == today)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    v.Id,
                    v.PatientName,
                    v.Department,
                    v.ChiefComplain,
                    v.QueueStatus,
                    v.Outcome,
                    v.CreatedAt,
                    Time = v.CreatedAt.ToLocalTime().ToString("hh:mm tt")
                })
                .ToListAsync();

            return Ok(new
            {
                waiting    = visits.Where(v => v.QueueStatus == "Waiting").ToList(),
                withDoctor = visits.Where(v => v.QueueStatus == "WithDoctor").ToList(),
                cleared    = visits.Where(v => v.QueueStatus == "Cleared").ToList()
            });
        }

        // POST api/queue – add new visit (also adds/updates employee)
        [HttpPost]
        public async Task<IActionResult> AddToQueue([FromBody] NewVisitRequest req)
        {
            if (await IsEmployeeUser())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(req.PatientName) || string.IsNullOrWhiteSpace(req.ChiefComplain))
                return BadRequest(new { success = false, message = "Patient name and chief complaint are required." });

            // Look up or create employee record
            var emp = await _db.Employees
                .FirstOrDefaultAsync(e => e.Name.ToLower() == req.PatientName.ToLower());

            if (emp == null && !string.IsNullOrWhiteSpace(req.PatientName))
            {
                // Auto-generate employee number
                int empCount = await _db.Employees.CountAsync();
                emp = new Employee
                {
                    EmployeeNumber = $"EMP-{(empCount + 1):D3}",
                    Name           = req.PatientName,
                    Department     = req.Department ?? "",
                    Age            = req.Age,
                    Gender         = req.Sex ?? "",
                    ContactNumber  = req.ContactNumber ?? "",
                    Address        = req.Address ?? "",
                    Occupation     = req.Occupation ?? "",
                    Birthday       = req.Birthday.HasValue ? req.Birthday.Value : null
                };
                _db.Employees.Add(emp);
                await _db.SaveChangesAsync();
            }

            // Check for duplicate active queue entry today
            var today = DateTime.Today;
            bool alreadyQueued = await _db.Visits.AnyAsync(v =>
                v.PatientName.ToLower() == req.PatientName.ToLower() &&
                v.DateOfConsultation == today &&
                (v.QueueStatus == "Waiting" || v.QueueStatus == "WithDoctor"));

            if (alreadyQueued)
                return Conflict(new { success = false, message = $"{req.PatientName} is already in the active queue." });

            var visit = new Visit
            {
                EmployeeId          = emp?.Id,
                PatientName         = req.PatientName,
                Department          = req.Department ?? "",
                Age                 = req.Age,
                Sex                 = req.Sex ?? "",
                ContactNumber       = req.ContactNumber ?? "",
                Address             = req.Address ?? "",
                Occupation          = req.Occupation ?? "",
                FamilyHistory       = req.FamilyHistory ?? "",
                ChiefComplain       = req.ChiefComplain,
                DateOfConsultation  = today,
                Birthday            = req.Birthday,
                VitalBP             = req.VitalBP,
                VitalPR             = req.VitalPR,
                VitalRR             = req.VitalRR,
                VitalTemp           = req.VitalTemp,
                VitalWT             = req.VitalWT,
                VitalHT             = req.VitalHT,
                VitalWC             = req.VitalWC,
                IsSmoker            = req.IsSmoker,
                SticksPerDay        = req.SticksPerDay,
                IsAlcoholDrinker    = req.IsAlcoholDrinker,
                BottlesAlcohol      = req.BottlesAlcohol,
                IsBeverageDrinker   = req.IsBeverageDrinker,
                BottlesBeverage     = req.BottlesBeverage,
                QueueStatus         = "Waiting",
                CreatedAt           = DateTime.UtcNow
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, visitId = visit.Id });
        }

        // PATCH api/queue/{id}/callin  – Waiting → WithDoctor
        [HttpPatch("{id}/callin")]
        public async Task<IActionResult> CallIn(int id)
        {
            if (await IsEmployeeUser())
            {
                return Forbid();
            }

            var visit = await _db.Visits.FindAsync(id);
            if (visit == null) return NotFound();

            visit.QueueStatus = "WithDoctor";
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // PATCH api/queue/{id}/logvisit  – WithDoctor → Cleared
        [HttpPatch("{id}/logvisit")]
        public async Task<IActionResult> LogVisit(int id, [FromBody] LogVisitRequest req)
        {
            if (await IsEmployeeUser())
            {
                return Forbid();
            }

            var visit = await _db.Visits.FindAsync(id);
            if (visit == null) return NotFound();

            visit.QueueStatus = "Cleared";
            visit.Outcome     = req.Outcome ?? "Fit to Work";

            // Add prescriptions if provided
            if (req.Prescriptions != null && req.Prescriptions.Any())
            {
                foreach (var prescriptionReq in req.Prescriptions)
                {
                    var medicine = await _db.Medicines.FindAsync(prescriptionReq.MedicineId);
                    if (medicine == null) continue;

                    // Check stock availability
                    if (medicine.CurrentStock < prescriptionReq.Quantity)
                    {
                        return BadRequest(new 
                        { 
                            success = false, 
                            message = $"Insufficient stock for {medicine.Name}. Available: {medicine.CurrentStock} {medicine.Unit}" 
                        });
                    }

                    var prescription = new Capstone.Models.Prescription
                    {
                        VisitId = visit.Id,
                        MedicineId = prescriptionReq.MedicineId,
                        MedicineName = medicine.Name,
                        Quantity = prescriptionReq.Quantity,
                        Dosage = prescriptionReq.Dosage,
                        Frequency = prescriptionReq.Frequency,
                        Duration = prescriptionReq.Duration,
                        Instructions = prescriptionReq.Instructions,
                        PrescribedBy = prescriptionReq.PrescribedBy,
                        PrescribedDate = DateTime.UtcNow,
                        IsDispensed = false
                    };

                    _db.Prescriptions.Add(prescription);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // GET api/queue/{id} – Get visit details for consultation
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisitDetails(int id)
        {
            if (await IsEmployeeUser())
            {
                return Forbid();
            }

            var visit = await _db.Visits
                .Include(v => v.Employee)
                .Include(v => v.Prescriptions)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visit == null) return NotFound();

            return Ok(new
            {
                visit.Id,
                visit.PatientName,
                visit.Department,
                visit.Age,
                visit.Sex,
                visit.ContactNumber,
                visit.Address,
                visit.Occupation,
                visit.Birthday,
                visit.FamilyHistory,
                visit.ChiefComplain,
                visit.DateOfConsultation,
                visit.QueueStatus,
                visit.Outcome,
                // Vitals
                visit.VitalBP,
                visit.VitalPR,
                visit.VitalRR,
                visit.VitalTemp,
                visit.VitalWT,
                visit.VitalHT,
                visit.VitalWC,
                // Social History
                visit.IsSmoker,
                visit.SticksPerDay,
                visit.IsAlcoholDrinker,
                visit.BottlesAlcohol,
                visit.IsBeverageDrinker,
                visit.BottlesBeverage,
                // Employee Info
                Employee = visit.Employee != null ? new
                {
                    visit.Employee.Id,
                    visit.Employee.EmployeeNumber,
                    visit.Employee.Name
                } : null,
                // Prescriptions
                Prescriptions = visit.Prescriptions.Select(p => new
                {
                    p.Id,
                    p.MedicineId,
                    p.MedicineName,
                    p.Quantity,
                    p.Dosage,
                    p.Frequency,
                    p.Duration,
                    p.Instructions,
                    p.PrescribedDate,
                    p.PrescribedBy,
                    p.IsDispensed,
                    p.DispensedDate
                }).ToList()
            });
        }
    }

    // ── Request DTOs ────────────────────────────────────────────────────────
    public class NewVisitRequest
    {
        public string PatientName  { get; set; } = string.Empty;
        public string? Department  { get; set; }
        public int     Age         { get; set; }
        public string? Sex         { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address     { get; set; }
        public string? Occupation  { get; set; }
        public DateTime? Birthday  { get; set; }
        public string? FamilyHistory { get; set; }
        public string  ChiefComplain { get; set; } = string.Empty;
        // Vitals
        public string? VitalBP    { get; set; }
        public string? VitalPR    { get; set; }
        public string? VitalRR    { get; set; }
        public string? VitalTemp  { get; set; }
        public string? VitalWT    { get; set; }
        public string? VitalHT    { get; set; }
        public string? VitalWC    { get; set; }
        // Social History
        public bool IsSmoker          { get; set; }
        public int? SticksPerDay      { get; set; }
        public bool IsAlcoholDrinker  { get; set; }
        public int? BottlesAlcohol    { get; set; }
        public bool IsBeverageDrinker { get; set; }
        public int? BottlesBeverage   { get; set; }
    }

    public class LogVisitRequest
    {
        public string? Outcome { get; set; }
        public List<PrescriptionItem>? Prescriptions { get; set; }
    }

    public class PrescriptionItem
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string PrescribedBy { get; set; } = string.Empty;
    }
}
