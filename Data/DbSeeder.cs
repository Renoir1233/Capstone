using Capstone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply any pending migrations
            await db.Database.MigrateAsync();

            // ── Seed Admin User ────────────────────────────────────────────
            const string adminUsername = "Admin";
            const string adminPassword = "Admin123!";

            if (await userManager.FindByNameAsync(adminUsername) == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminUsername,
                    Email = "admin@unihealth.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to seed Admin user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // ── Seed Medicines ─────────────────────────────────────────────
            if (!await db.Medicines.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var medicines = new List<Medicine>
                {
                    new() { Name = "Mefenamic Acid 500mg",  Category = "Analgesic",     CurrentStock = 15,  MinStock = 20, CriticalStock = 10, MaxStock = 100, Unit = "tablets",  BatchNumber = "BA-001", ExpiryDate = now.AddMonths(8),  LastRestockedAt = now.AddMonths(-3) },
                    new() { Name = "Paracetamol 500mg",     Category = "Antipyretic",   CurrentStock = 48,  MinStock = 50, CriticalStock = 20, MaxStock = 200, Unit = "tablets",  BatchNumber = "BA-002", ExpiryDate = now.AddMonths(14), LastRestockedAt = now.AddMonths(-1) },
                    new() { Name = "Amoxicillin 500mg",     Category = "Antibiotic",    CurrentStock = 240, MinStock = 60, CriticalStock = 30, MaxStock = 300, Unit = "capsules", BatchNumber = "BA-003", ExpiryDate = now.AddMonths(10), LastRestockedAt = now.AddMonths(-2) },
                    new() { Name = "Ibuprofen 200mg",       Category = "NSAID",         CurrentStock = 150, MinStock = 50, CriticalStock = 20, MaxStock = 200, Unit = "tablets",  BatchNumber = "BA-004", ExpiryDate = now.AddMonths(25), LastRestockedAt = now.AddMonths(-4) },
                    new() { Name = "Cetirizine 10mg",       Category = "Antihistamine", CurrentStock = 80,  MinStock = 30, CriticalStock = 15, MaxStock = 150, Unit = "tablets",  BatchNumber = "BA-005", ExpiryDate = now.AddMonths(20), LastRestockedAt = now.AddMonths(-2) },
                    new() { Name = "Omeprazole 20mg",       Category = "Antacid",       CurrentStock = 8,   MinStock = 20, CriticalStock = 10, MaxStock = 100, Unit = "capsules", BatchNumber = "BA-006", ExpiryDate = now.AddDays(25),   LastRestockedAt = now.AddMonths(-5) },
                    new() { Name = "Hydrocortisone Cream",  Category = "Topical",       CurrentStock = 25,  MinStock = 10, CriticalStock = 5,  MaxStock = 60,  Unit = "tubes",    BatchNumber = "BA-007", ExpiryDate = now.AddMonths(18), LastRestockedAt = now.AddMonths(-3) },
                    new() { Name = "Salbutamol Inhaler",    Category = "Bronchodilator",CurrentStock = 6,   MinStock = 10, CriticalStock = 5,  MaxStock = 30,  Unit = "pcs",      BatchNumber = "BA-008", ExpiryDate = now.AddDays(28),   LastRestockedAt = now.AddMonths(-6) },
                };
                db.Medicines.AddRange(medicines);
                await db.SaveChangesAsync();
            }
        }
    }
}
