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

            // ── Seed Employee User ─────────────────────────────────────────
            const string employeeUsername = "Employee";
            const string employeePassword = "Employee123!";

            var employeeUser = await userManager.FindByNameAsync(employeeUsername);
            if (employeeUser == null)
            {
                employeeUser = new IdentityUser
                {
                    UserName = employeeUsername,
                    Email = "employee@unihealth.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(employeeUser, employeePassword);
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to seed Employee user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // ── Seed Employee Record ───────────────────────────────────────
            var existingEmployee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == "EMP-001");
            
            if (existingEmployee == null)
            {
                // Create new employee
                var employee = new Employee
                {
                    EmployeeNumber = "EMP-001",
                    Name = "John Doe",
                    Department = "IT Department",
                    Position = "Software Developer",
                    Age = 30,
                    Gender = "Male",
                    Occupation = "Software Developer",
                    Address = "123 Main St, Quezon City",
                    ContactNumber = "09171234567",
                    Email = "employee@unihealth.com",
                    EmergencyContact = "Jane Doe - 09187654321",
                    PhilHealthId = "12-345678901-2",
                    SSSId = "34-1234567-8",
                    TINId = "123-456-789-000",
                    PagIbigId = "1234-5678-9012",
                    Birthday = new DateTime(1994, 5, 15),
                    Status = "Active",
                    UserId = employeeUser.Id
                };
                db.Employees.Add(employee);
                await db.SaveChangesAsync();

                // Add some health reminders for the employee
                var reminders = new List<HealthReminder>
                {
                    new() { EmployeeId = employee.Id, ReminderType = "Annual Physical Exam", Description = "Annual physical examination required", DueDate = DateTime.Now.AddDays(30), IsCompleted = false },
                    new() { EmployeeId = employee.Id, ReminderType = "Flu Vaccination", Description = "Annual flu shot", DueDate = DateTime.Now.AddDays(15), IsCompleted = false },
                    new() { EmployeeId = employee.Id, ReminderType = "Blood Pressure Check", Description = "Quarterly BP monitoring", DueDate = DateTime.Now.AddDays(7), IsCompleted = false },
                    new() { EmployeeId = employee.Id, ReminderType = "Follow-up Consultation", Description = "Follow-up for previous consultation", DueDate = DateTime.Now.AddDays(45), IsCompleted = false }
                };
                db.HealthReminders.AddRange(reminders);
                await db.SaveChangesAsync();
            }
            else if (existingEmployee.UserId == null)
            {
                // Update existing employee to link with user account
                existingEmployee.UserId = employeeUser.Id;
                existingEmployee.Email = "employee@unihealth.com";
                existingEmployee.Position = existingEmployee.Position ?? "Employee";
                existingEmployee.EmergencyContact = existingEmployee.EmergencyContact ?? "Emergency Contact - 09187654321";
                existingEmployee.PhilHealthId = existingEmployee.PhilHealthId ?? "";
                existingEmployee.SSSId = existingEmployee.SSSId ?? "";
                existingEmployee.TINId = existingEmployee.TINId ?? "";
                existingEmployee.PagIbigId = existingEmployee.PagIbigId ?? "";
                await db.SaveChangesAsync();

                // Add health reminders if none exist
                if (!await db.HealthReminders.AnyAsync(r => r.EmployeeId == existingEmployee.Id))
                {
                    var reminders = new List<HealthReminder>
                    {
                        new() { EmployeeId = existingEmployee.Id, ReminderType = "Annual Physical Exam", Description = "Annual physical examination required", DueDate = DateTime.Now.AddDays(30), IsCompleted = false },
                        new() { EmployeeId = existingEmployee.Id, ReminderType = "Flu Vaccination", Description = "Annual flu shot", DueDate = DateTime.Now.AddDays(15), IsCompleted = false },
                        new() { EmployeeId = existingEmployee.Id, ReminderType = "Blood Pressure Check", Description = "Quarterly BP monitoring", DueDate = DateTime.Now.AddDays(7), IsCompleted = false },
                        new() { EmployeeId = existingEmployee.Id, ReminderType = "Follow-up Consultation", Description = "Follow-up for previous consultation", DueDate = DateTime.Now.AddDays(45), IsCompleted = false }
                    };
                    db.HealthReminders.AddRange(reminders);
                    await db.SaveChangesAsync();
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
