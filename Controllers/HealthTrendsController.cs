using Capstone.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Route("api/health-trends")]
    [ApiController]
    public class HealthTrendsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public HealthTrendsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/health-trends/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var today = DateTime.Today;
            var lastQuarter = today.AddMonths(-3);

            // Average consultation time (mock for now - would need actual consultation duration tracking)
            var avgConsultationTime = 14.2;

            // Top consultation day
            var visitsByDay = await _db.Visits
                .Where(v => v.DateOfConsultation >= lastQuarter && v.QueueStatus == "Cleared")
                .GroupBy(v => v.DateOfConsultation.DayOfWeek)
                .Select(g => new
                {
                    Day = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var topDay = visitsByDay.OrderByDescending(d => d.Count).FirstOrDefault();
            var totalVisits = visitsByDay.Sum(d => d.Count);
            var topDayPercentage = topDay != null && totalVisits > 0 
                ? Math.Round((double)topDay.Count / totalVisits * 100, 1) 
                : 0;

            // Referral rate (visits with outcome "Referred to Specialist")
            var totalClearedVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= lastQuarter && v.QueueStatus == "Cleared")
                .CountAsync();

            var referredVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= lastQuarter && 
                           v.QueueStatus == "Cleared" && 
                           v.Outcome.Contains("Referred"))
                .CountAsync();

            var referralRate = totalClearedVisits > 0 
                ? Math.Round((double)referredVisits / totalClearedVisits * 100, 1) 
                : 0;

            // Preventive checkups rate (visits that are not urgent complaints)
            var preventiveVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= lastQuarter && 
                           v.QueueStatus == "Cleared" &&
                           !v.ChiefComplain.ToLower().Contains("pain") &&
                           !v.ChiefComplain.ToLower().Contains("fever") &&
                           !v.ChiefComplain.ToLower().Contains("emergency"))
                .CountAsync();

            var preventiveRate = totalClearedVisits > 0 
                ? Math.Round((double)preventiveVisits / totalClearedVisits * 100, 0) 
                : 0;

            return Ok(new
            {
                AvgConsultationTime = avgConsultationTime,
                AvgConsultationTimeChange = -1.4,
                TopConsultationDay = topDay?.Day.ToString() ?? "Monday",
                TopConsultationDayPercentage = topDayPercentage,
                ReferralRate = referralRate,
                ReferralRateChange = -0.6,
                PreventiveCheckupsRate = preventiveRate,
                PreventiveCheckupsChange = 8.0
            });
        }

        // GET api/health-trends/consultation-analysis
        [HttpGet("consultation-analysis")]
        public async Task<IActionResult> GetConsultationAnalysis([FromQuery] string period = "quarter")
        {
            var startDate = period switch
            {
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                "quarter" => DateTime.Today.AddMonths(-3),
                "year" => DateTime.Today.AddYears(-1),
                _ => DateTime.Today.AddMonths(-3)
            };

            // Common illnesses analysis
            var commonIllnesses = await _db.Visits
                .Where(v => v.DateOfConsultation >= startDate && v.QueueStatus == "Cleared")
                .GroupBy(v => v.ChiefComplain)
                .Select(g => new
                {
                    Complaint = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(i => i.Count)
                .Take(10)
                .ToListAsync();

            // Department-wise visits
            var departmentVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= startDate && v.QueueStatus == "Cleared")
                .GroupBy(v => v.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToListAsync();

            // Outcome distribution
            var outcomes = await _db.Visits
                .Where(v => v.DateOfConsultation >= startDate && v.QueueStatus == "Cleared")
                .GroupBy(v => v.Outcome)
                .Select(g => new
                {
                    Outcome = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(o => o.Count)
                .ToListAsync();

            // Monthly trend
            var monthlyTrend = await _db.Visits
                .Where(v => v.DateOfConsultation >= startDate && v.QueueStatus == "Cleared")
                .GroupBy(v => new { v.DateOfConsultation.Year, v.DateOfConsultation.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToListAsync();

            return Ok(new
            {
                CommonIllnesses = commonIllnesses,
                DepartmentBreakdown = departmentVisits,
                OutcomeDistribution = outcomes,
                MonthlyTrend = monthlyTrend,
                TotalConsultations = await _db.Visits
                    .Where(v => v.DateOfConsultation >= startDate && v.QueueStatus == "Cleared")
                    .CountAsync()
            });
        }

        // GET api/health-trends/medicine-usage
        [HttpGet("medicine-usage")]
        public async Task<IActionResult> GetMedicineUsage([FromQuery] string period = "month")
        {
            var startDate = period switch
            {
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                "quarter" => DateTime.Today.AddMonths(-3),
                _ => DateTime.Today.AddMonths(-1)
            };

            // Most prescribed medicines
            var prescribedMedicines = await _db.Prescriptions
                .Where(p => p.PrescribedDate >= startDate)
                .GroupBy(p => p.MedicineName)
                .Select(g => new
                {
                    Medicine = g.Key,
                    TotalQuantity = g.Sum(p => p.Quantity),
                    PrescriptionCount = g.Count()
                })
                .OrderByDescending(m => m.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // Dispensing trends by week
            var weeklyDispensing = await _db.MedicineTransactions
                .Where(t => t.TransactionType == "Dispense" && t.TransactionDate >= startDate)
                .GroupBy(t => new { 
                    Year = t.TransactionDate.Year,
                    Week = EF.Functions.DateDiffWeek(new DateTime(t.TransactionDate.Year, 1, 1), t.TransactionDate)
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Week = g.Key.Week,
                    TotalDispensed = g.Sum(t => t.Quantity)
                })
                .OrderBy(w => w.Year).ThenBy(w => w.Week)
                .ToListAsync();

            return Ok(new
            {
                TopPrescribedMedicines = prescribedMedicines,
                WeeklyDispensingTrend = weeklyDispensing,
                TotalPrescriptions = await _db.Prescriptions
                    .Where(p => p.PrescribedDate >= startDate)
                    .CountAsync(),
                TotalDispensed = await _db.Prescriptions
                    .Where(p => p.PrescribedDate >= startDate && p.IsDispensed)
                    .CountAsync()
            });
        }

        // GET api/health-trends/wellness-participation
        [HttpGet("wellness-participation")]
        public async Task<IActionResult> GetWellnessParticipation()
        {
            var programs = await _db.WellnessPrograms
                .Include(w => w.Registrations)
                .Where(w => w.Status == "Completed" || w.Status == "Ongoing")
                .Select(w => new
                {
                    w.Title,
                    w.Category,
                    w.ProgramDate,
                    w.Status,
                    TotalRegistered = w.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended"),
                    TotalAttended = w.Registrations.Count(r => r.Status == "Attended"),
                    AttendanceRate = w.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended") > 0
                        ? Math.Round((double)w.Registrations.Count(r => r.Status == "Attended") / w.Registrations.Count(r => r.Status == "Registered" || r.Status == "Attended") * 100, 1)
                        : 0
                })
                .OrderByDescending(p => p.ProgramDate)
                .Take(10)
                .ToListAsync();

            var categoryBreakdown = await _db.WellnessProgramRegistrations
                .Include(r => r.WellnessProgram)
                .Where(r => r.Status == "Attended")
                .GroupBy(r => r.WellnessProgram!.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    ParticipantCount = g.Count()
                })
                .OrderByDescending(c => c.ParticipantCount)
                .ToListAsync();

            return Ok(new
            {
                RecentPrograms = programs,
                CategoryParticipation = categoryBreakdown,
                TotalProgramsCompleted = await _db.WellnessPrograms
                    .Where(w => w.Status == "Completed")
                    .CountAsync(),
                TotalParticipants = await _db.WellnessProgramRegistrations
                    .Where(r => r.Status == "Attended")
                    .CountAsync()
            });
        }
    }
}
