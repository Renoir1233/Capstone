using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Route("api/emr")]
    [ApiController]
    public class EmrController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public EmrController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/emr?search=&dept=
        [HttpGet]
        public async Task<IActionResult> GetEmployees([FromQuery] string? search, [FromQuery] string? dept)
        {
            var query = _db.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dept) && dept != "all")
                query = query.Where(e => e.Department == dept);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e =>
                    e.Name.Contains(search) ||
                    e.EmployeeNumber.Contains(search) ||
                    e.Department.Contains(search));

            var employees = await query
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeNumber,
                    e.Name,
                    e.Department,
                    e.Age,
                    e.Gender,
                    e.Status,
                    e.Occupation,
                    e.ContactNumber,
                    e.Address,
                    LastVisit = e.Visits
                        .Where(v => v.QueueStatus == "Cleared")
                        .OrderByDescending(v => v.DateOfConsultation)
                        .Select(v => new { v.ChiefComplain, v.DateOfConsultation, v.Outcome })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET api/emr/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.Visits.OrderByDescending(v => v.DateOfConsultation).Take(10))
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeNumber,
                    e.Name,
                    e.Department,
                    e.Age,
                    e.Gender,
                    e.Status,
                    e.Occupation,
                    e.ContactNumber,
                    e.Address,
                    Visits = e.Visits
                        .OrderByDescending(v => v.DateOfConsultation)
                        .Take(10)
                        .Select(v => new
                        {
                            v.Id,
                            v.ChiefComplain,
                            v.DateOfConsultation,
                            v.Outcome,
                            v.QueueStatus,
                            v.VitalBP,
                            v.VitalTemp,
                            Prescriptions = v.Prescriptions.Select(p => new
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
                        })
                })
                .FirstOrDefaultAsync();

            if (emp == null) return NotFound();
            return Ok(emp);
        }

        // GET api/emr/visit/{visitId}
        [HttpGet("visit/{visitId}")]
        public async Task<IActionResult> GetVisit(int visitId)
        {
            var visit = await _db.Visits
                .Include(v => v.Prescriptions)
                .Include(v => v.Employee)
                .Where(v => v.Id == visitId)
                .Select(v => new
                {
                    v.Id,
                    v.PatientName,
                    v.Department,
                    v.Age,
                    v.Sex,
                    v.ChiefComplain,
                    v.DateOfConsultation,
                    v.Outcome,
                    v.QueueStatus,
                    v.VitalBP,
                    v.VitalPR,
                    v.VitalRR,
                    v.VitalTemp,
                    v.VitalWT,
                    v.VitalHT,
                    v.VitalWC,
                    v.IsSmoker,
                    v.SticksPerDay,
                    v.IsAlcoholDrinker,
                    v.BottlesAlcohol,
                    v.IsBeverageDrinker,
                    v.BottlesBeverage,
                    v.FamilyHistory,
                    Employee = v.Employee != null ? new
                    {
                        v.Employee.Id,
                        v.Employee.EmployeeNumber,
                        v.Employee.Name
                    } : null,
                    Prescriptions = v.Prescriptions.Select(p => new
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
                })
                .FirstOrDefaultAsync();

            if (visit == null) return NotFound();
            return Ok(visit);
        }

        // POST api/emr/visit/{visitId}/prescriptions
        [HttpPost("visit/{visitId}/prescriptions")]
        public async Task<IActionResult> AddPrescription(int visitId, [FromBody] PrescriptionRequest request)
        {
            var visit = await _db.Visits.FindAsync(visitId);
            if (visit == null) return NotFound("Visit not found");

            var medicine = await _db.Medicines.FindAsync(request.MedicineId);
            if (medicine == null) return NotFound("Medicine not found");

            // Check if medicine has enough stock
            if (medicine.CurrentStock < request.Quantity)
            {
                return BadRequest($"Insufficient stock. Available: {medicine.CurrentStock} {medicine.Unit}");
            }

            var prescription = new Capstone.Models.Prescription
            {
                VisitId = visitId,
                MedicineId = request.MedicineId,
                MedicineName = medicine.Name,
                Quantity = request.Quantity,
                Dosage = request.Dosage,
                Frequency = request.Frequency,
                Duration = request.Duration,
                Instructions = request.Instructions,
                PrescribedBy = request.PrescribedBy,
                PrescribedDate = DateTime.UtcNow,
                IsDispensed = false
            };

            _db.Prescriptions.Add(prescription);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                prescription.Id,
                prescription.MedicineId,
                prescription.MedicineName,
                prescription.Quantity,
                prescription.Dosage,
                prescription.Frequency,
                prescription.Duration,
                prescription.Instructions,
                prescription.PrescribedDate,
                prescription.PrescribedBy,
                prescription.IsDispensed
            });
        }

        // PUT api/emr/prescriptions/{id}
        [HttpPut("prescriptions/{id}")]
        public async Task<IActionResult> UpdatePrescription(int id, [FromBody] PrescriptionRequest request)
        {
            var prescription = await _db.Prescriptions.FindAsync(id);
            if (prescription == null) return NotFound();

            if (prescription.IsDispensed)
            {
                return BadRequest("Cannot update a dispensed prescription");
            }

            var medicine = await _db.Medicines.FindAsync(request.MedicineId);
            if (medicine == null) return NotFound("Medicine not found");

            // Check stock availability
            if (medicine.CurrentStock < request.Quantity)
            {
                return BadRequest($"Insufficient stock. Available: {medicine.CurrentStock} {medicine.Unit}");
            }

            prescription.MedicineId = request.MedicineId;
            prescription.MedicineName = medicine.Name;
            prescription.Quantity = request.Quantity;
            prescription.Dosage = request.Dosage;
            prescription.Frequency = request.Frequency;
            prescription.Duration = request.Duration;
            prescription.Instructions = request.Instructions;

            await _db.SaveChangesAsync();
            return Ok(prescription);
        }

        // DELETE api/emr/prescriptions/{id}
        [HttpDelete("prescriptions/{id}")]
        public async Task<IActionResult> DeletePrescription(int id)
        {
            var prescription = await _db.Prescriptions.FindAsync(id);
            if (prescription == null) return NotFound();

            if (prescription.IsDispensed)
            {
                return BadRequest("Cannot delete a dispensed prescription");
            }

            _db.Prescriptions.Remove(prescription);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST api/emr/prescriptions/{id}/dispense
        [HttpPost("prescriptions/{id}/dispense")]
        public async Task<IActionResult> DispensePrescription(int id, [FromBody] DispenseRequest request)
        {
            var prescription = await _db.Prescriptions
                .Include(p => p.Medicine)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null) return NotFound();

            if (prescription.IsDispensed)
            {
                return BadRequest("Prescription already dispensed");
            }

            var medicine = prescription.Medicine;
            if (medicine == null) return NotFound("Medicine not found");

            // Check stock availability
            if (medicine.CurrentStock < prescription.Quantity)
            {
                return BadRequest($"Insufficient stock. Available: {medicine.CurrentStock} {medicine.Unit}");
            }

            // Update medicine stock
            medicine.CurrentStock -= prescription.Quantity;
            medicine.LastDispensedAt = DateTime.UtcNow;

            // Create medicine transaction
            var transaction = new MedicineTransaction
            {
                MedicineId = medicine.Id,
                TransactionType = "Dispense",
                Quantity = prescription.Quantity,
                Notes = $"Dispensed for prescription #{prescription.Id} - Visit #{prescription.VisitId}",
                PerformedBy = request.DispensedBy,
                TransactionDate = DateTime.UtcNow
            };

            _db.MedicineTransactions.Add(transaction);

            // Update prescription status
            prescription.IsDispensed = true;
            prescription.DispensedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                prescription.Id,
                prescription.IsDispensed,
                prescription.DispensedDate,
                medicine.CurrentStock,
                medicine.StockStatus
            });
        }

        // GET api/emr/medicines
        [HttpGet("medicines")]
        public async Task<IActionResult> GetMedicines([FromQuery] string? search)
        {
            var query = _db.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Name.Contains(search) || m.Category.Contains(search));
            }

            var medicines = await query
                .Where(m => m.CurrentStock > 0)
                .OrderBy(m => m.Name)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Category,
                    m.CurrentStock,
                    m.Unit,
                    m.StockStatus
                })
                .ToListAsync();

            return Ok(medicines);
        }
    }

    public class PrescriptionRequest
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string PrescribedBy { get; set; } = string.Empty;
    }

    public class DispenseRequest
    {
        public string DispensedBy { get; set; } = string.Empty;
    }
}
