using Capstone.Data;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Controllers
{
    [Route("api/pharmacy")]
    [ApiController]
    public class PharmacyController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PharmacyController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET api/pharmacy/medicines
        [HttpGet("medicines")]
        public async Task<IActionResult> GetMedicines()
        {
            var medicines = await _db.Medicines
                .OrderBy(m => m.Name)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Category,
                    m.CurrentStock,
                    m.MinStock,
                    m.CriticalStock,
                    m.MaxStock,
                    m.Unit,
                    m.BatchNumber,
                    m.ExpiryDate,
                    m.LastRestockedAt,
                    m.LastDispensedAt,
                    StockStatus  = m.CurrentStock <= m.CriticalStock ? "Critical"
                                 : m.CurrentStock <= m.MinStock       ? "Low"
                                 : "Good",
                    StockPercent = m.MaxStock == 0 ? 0
                                 : (int)Math.Round((double)m.CurrentStock / m.MaxStock * 100)
                })
                .ToListAsync();

            return Ok(medicines);
        }

        // GET api/pharmacy/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var medicines = await _db.Medicines.ToListAsync();
            var now = DateTime.UtcNow;

            int total       = medicines.Sum(m => m.CurrentStock);
            int lowCount    = medicines.Count(m => m.CurrentStock <= m.MinStock);
            int expiring    = medicines.Count(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= now.AddDays(30));
            int varieties   = medicines.Count;

            return Ok(new { total, lowCount, expiring, varieties });
        }

        // GET api/pharmacy/dispensing-trends
        [HttpGet("dispensing-trends")]
        public async Task<IActionResult> GetDispensingTrends()
        {
            var today  = DateTime.UtcNow.Date;
            var colors = new[] { "#10b981", "#3b82f6", "#8b5cf6", "#f59e0b", "#ef4444" };

            // Build week labels W1..W8 (oldest first)
            var labels = new List<string>();
            for (int i = 7; i >= 0; i--)
                labels.Add($"W{8 - i}");

            // Step 1 – find top-5 most dispensed medicine IDs (two safe EF queries)
            var dispenseTotals = await _db.MedicineTransactions
                .Where(t => t.TransactionType == "Dispense")
                .GroupBy(t => t.MedicineId)
                .Select(g => new { MedicineId = g.Key, Total = g.Sum(t => t.Quantity) })
                .OrderByDescending(g => g.Total)
                .Take(5)
                .ToListAsync();

            var datasets = new List<object>();

            if (dispenseTotals.Any())
            {
                var medIds   = dispenseTotals.Select(d => d.MedicineId).ToList();
                var medNames = await _db.Medicines
                    .Where(m => medIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Name })
                    .ToListAsync();

                // Pull all relevant transactions in one query, group in-memory
                var eightWeeksAgo = today.AddDays(-56);
                var allTxns = await _db.MedicineTransactions
                    .Where(t => t.TransactionType == "Dispense"
                             && medIds.Contains(t.MedicineId)
                             && t.TransactionDate >= eightWeeksAgo)
                    .Select(t => new { t.MedicineId, t.Quantity, t.TransactionDate })
                    .ToListAsync();

                int ci = 0;
                foreach (var dt in dispenseTotals)
                {
                    var medName = medNames.FirstOrDefault(m => m.Id == dt.MedicineId)?.Name ?? $"ID {dt.MedicineId}";
                    var data    = new List<int>();

                    for (int i = 7; i >= 0; i--)
                    {
                        var weekStart = today.AddDays(-7 * i - 6);
                        var weekEnd   = today.AddDays(-7 * i + 1);

                        int qty = allTxns
                            .Where(t => t.MedicineId == dt.MedicineId
                                     && t.TransactionDate >= weekStart
                                     && t.TransactionDate < weekEnd)
                            .Sum(t => t.Quantity);

                        data.Add(qty);
                    }

                    datasets.Add(new
                    {
                        label           = medName,
                        data,
                        borderColor     = colors[ci % colors.Length],
                        backgroundColor = colors[ci % colors.Length] + "22",
                        borderWidth     = 2,
                        tension         = 0.4,
                        fill            = false,
                        pointRadius     = 4,
                        pointHoverRadius = 7
                    });
                    ci++;
                }

                return Ok(new { chartType = "line", labels, datasets, noDispenseData = false });
            }

            // No dispense history yet – show current stock as a bar chart so the user
            // always sees real DB data rather than placeholder zeros.
            var stockMeds = await _db.Medicines
                .OrderByDescending(m => m.CurrentStock)
                .Take(8)
                .Select(m => new { m.Name, m.CurrentStock })
                .ToListAsync();

            return Ok(new
            {
                chartType     = "bar",
                labels        = stockMeds.Select(m => m.Name).ToList(),
                datasets      = new object[]
                {
                    new
                    {
                        label           = "Current Stock",
                        data            = stockMeds.Select(m => m.CurrentStock).ToList(),
                        backgroundColor = stockMeds.Select((_, i) => colors[i % colors.Length]).ToList(),
                        borderRadius    = 6,
                        borderWidth     = 0
                    }
                },
                noDispenseData = true
            });
        }

        // POST api/pharmacy/medicines  – add new medicine
        [HttpPost("medicines")]
        public async Task<IActionResult> AddMedicine([FromBody] AddMedicineRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { success = false, message = "Name is required." });

            var med = new Medicine
            {
                Name         = req.Name,
                Category     = req.Category ?? "",
                CurrentStock = req.InitialStock,
                MinStock     = req.MinStock > 0 ? req.MinStock : 20,
                CriticalStock= req.CriticalStock > 0 ? req.CriticalStock : 10,
                MaxStock     = req.MaxStock > 0 ? req.MaxStock : 100,
                Unit         = req.Unit ?? "tablets",
                BatchNumber  = req.BatchNumber ?? "",
                ExpiryDate   = req.ExpiryDate,
                LastRestockedAt = DateTime.UtcNow
            };

            _db.Medicines.Add(med);
            await _db.SaveChangesAsync();

            if (req.InitialStock > 0)
            {
                _db.MedicineTransactions.Add(new MedicineTransaction
                {
                    MedicineId      = med.Id,
                    TransactionType = "Restock",
                    Quantity        = req.InitialStock,
                    Notes           = "Initial stock entry",
                    PerformedBy     = User.Identity?.Name ?? "Admin",
                    TransactionDate = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, id = med.Id });
        }

        // PATCH api/pharmacy/medicines/{id}/restock
        [HttpPatch("medicines/{id}/restock")]
        public async Task<IActionResult> Restock(int id, [FromBody] StockChangeRequest req)
        {
            var med = await _db.Medicines.FindAsync(id);
            if (med == null) return NotFound();

            med.CurrentStock    = Math.Min(med.MaxStock, med.CurrentStock + req.Quantity);
            med.LastRestockedAt = DateTime.UtcNow;

            _db.MedicineTransactions.Add(new MedicineTransaction
            {
                MedicineId      = med.Id,
                TransactionType = "Restock",
                Quantity        = req.Quantity,
                Notes           = req.Notes ?? "",
                PerformedBy     = User.Identity?.Name ?? "Admin",
                TransactionDate = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, currentStock = med.CurrentStock });
        }

        // PATCH api/pharmacy/medicines/{id}/dispense
        [HttpPatch("medicines/{id}/dispense")]
        public async Task<IActionResult> Dispense(int id, [FromBody] StockChangeRequest req)
        {
            var med = await _db.Medicines.FindAsync(id);
            if (med == null) return NotFound();

            if (med.CurrentStock < req.Quantity)
                return BadRequest(new { success = false, message = $"Insufficient stock. Available: {med.CurrentStock}" });

            med.CurrentStock    -= req.Quantity;
            med.LastDispensedAt = DateTime.UtcNow;

            _db.MedicineTransactions.Add(new MedicineTransaction
            {
                MedicineId      = med.Id,
                TransactionType = "Dispense",
                Quantity        = req.Quantity,
                Notes           = req.Notes ?? "",
                PerformedBy     = User.Identity?.Name ?? "Admin",
                TransactionDate = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(new { success = true, currentStock = med.CurrentStock });
        }

        // GET api/pharmacy/transactions
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var txns = await _db.MedicineTransactions
                .Include(t => t.Medicine)
                .OrderByDescending(t => t.TransactionDate)
                .Take(50)
                .Select(t => new
                {
                    t.Id,
                    MedicineName    = t.Medicine!.Name,
                    t.TransactionType,
                    t.Quantity,
                    t.Notes,
                    t.PerformedBy,
                    t.TransactionDate
                })
                .ToListAsync();

            return Ok(txns);
        }
    }

    // ── Request DTOs ────────────────────────────────────────────────────────
    public class AddMedicineRequest
    {
        public string  Name          { get; set; } = string.Empty;
        public string? Category      { get; set; }
        public int     InitialStock  { get; set; }
        public int     MinStock      { get; set; }
        public int     CriticalStock { get; set; }
        public int     MaxStock      { get; set; }
        public string? Unit          { get; set; }
        public string? BatchNumber   { get; set; }
        public DateTime? ExpiryDate  { get; set; }
    }

    public class StockChangeRequest
    {
        public int     Quantity { get; set; }
        public string? Notes    { get; set; }
    }
}
