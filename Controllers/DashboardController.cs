using Capstone.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/dashboard/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = startOfMonth.AddMonths(-1);

            int todayVisits = await _db.Visits
                .Where(v => v.DateOfConsultation == today)
                .CountAsync();

            int lowStockCount = await _db.Medicines
                .CountAsync(m => m.CurrentStock <= m.MinStock);

            int sickLeavesThisMonth = await _db.Visits
                .Where(v => v.DateOfConsultation >= startOfMonth
                         && v.DateOfConsultation <  startOfMonth.AddMonths(1)
                         && v.Outcome.Contains("Sent Home"))
                .CountAsync();

            int sickLeavesLastMonth = await _db.Visits
                .Where(v => v.DateOfConsultation >= lastMonth
                         && v.DateOfConsultation <  startOfMonth
                         && v.Outcome.Contains("Sent Home"))
                .CountAsync();

            // Health score: 100 minus penalty for sick-leave rate
            int totalMonthVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= startOfMonth)
                .CountAsync();

            int healthScore = totalMonthVisits == 0
                ? 90
                : Math.Max(50, 100 - (int)Math.Round((double)sickLeavesThisMonth / Math.Max(totalMonthVisits, 1) * 100));

            return Ok(new
            {
                todayVisits,
                lowStockCount,
                sickLeavesThisMonth,
                sickLeavesLastMonth,
                healthScore
            });
        }

        // GET api/dashboard/monthly-visits
        [HttpGet("monthly-visits")]
        public async Task<IActionResult> GetMonthlyVisits()
        {
            var today = DateTime.Today;
            var labels = new List<string>();
            var data   = new List<int>();

            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var monthEnd   = monthStart.AddMonths(1);

                int count = await _db.Visits
                    .Where(v => v.DateOfConsultation >= monthStart && v.DateOfConsultation < monthEnd)
                    .CountAsync();

                labels.Add(monthStart.ToString("MMM"));
                data.Add(count);
            }

            return Ok(new { labels, data });
        }

        // GET api/dashboard/top-illnesses
        [HttpGet("top-illnesses")]
        public async Task<IActionResult> GetTopIllnesses()
        {
            var startOfYear = new DateTime(DateTime.Today.Year, 1, 1);

            // Categorize chief complaints into illness groups
            var groups = new Dictionary<string, string[]>
            {
                { "Upper Respiratory", new[] { "Cough", "Sore Throat", "Flu", "Cold", "Upper Respiratory" } },
                { "Hypertension",       new[] { "Hypertension", "High Blood", "BP" } },
                { "Gastroenteritis",    new[] { "Gastro", "Diarrhea", "Vomiting", "Nausea", "Indigestion", "Stomach" } },
                { "Back Pain",          new[] { "Back Pain", "Back", "Muscle Strain", "Muscle" } },
                { "Headache",           new[] { "Headache", "Migraine", "Head" } },
            };

            var allVisits = await _db.Visits
                .Where(v => v.DateOfConsultation >= startOfYear)
                .Select(v => v.ChiefComplain)
                .ToListAsync();

            var counts = new Dictionary<string, int>();
            int othersCount = 0;

            foreach (var complaint in allVisits)
            {
                bool matched = false;
                foreach (var (label, keywords) in groups)
                {
                    if (keywords.Any(k => complaint.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        counts.TryAdd(label, 0);
                        counts[label]++;
                        matched = true;
                        break;
                    }
                }
                if (!matched) othersCount++;
            }

            var labels = groups.Keys.ToList();
            labels.Add("Others");
            var data = labels.Select(l => l == "Others" ? othersCount : counts.GetValueOrDefault(l, 0)).ToList();

            return Ok(new { labels, data });
        }

        // GET api/dashboard/dept-visits
        [HttpGet("dept-visits")]
        public async Task<IActionResult> GetDeptVisits()
        {
            var startOfYear = new DateTime(DateTime.Today.Year, 1, 1);

            var deptGroups = await _db.Visits
                .Where(v => v.DateOfConsultation >= startOfYear && !string.IsNullOrEmpty(v.Department))
                .GroupBy(v => v.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderBy(g => g.Count)
                .ToListAsync();

            return Ok(new
            {
                labels = deptGroups.Select(g => g.Department).ToList(),
                data   = deptGroups.Select(g => g.Count).ToList()
            });
        }
    }
}
