using Capstone.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Visit> Visits { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineTransaction> MedicineTransactions { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<WellnessProgram> WellnessPrograms { get; set; } = null!;
        public DbSet<WellnessProgramRegistration> WellnessProgramRegistrations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee: unique employee number
            builder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            // Visit -> Employee (optional FK, restrict delete)
            builder.Entity<Visit>()
                .HasOne(v => v.Employee)
                .WithMany(e => e.Visits)
                .HasForeignKey(v => v.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // MedicineTransaction -> Medicine (cascade delete)
            builder.Entity<MedicineTransaction>()
                .HasOne(t => t.Medicine)
                .WithMany(m => m.Transactions)
                .HasForeignKey(t => t.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties on Medicine
            builder.Entity<Medicine>()
                .Ignore(m => m.StockStatus)
                .Ignore(m => m.StockPercent);

            // Prescription -> Visit (cascade delete)
            builder.Entity<Prescription>()
                .HasOne(p => p.Visit)
                .WithMany(v => v.Prescriptions)
                .HasForeignKey(p => p.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prescription -> Medicine (restrict delete)
            builder.Entity<Prescription>()
                .HasOne(p => p.Medicine)
                .WithMany()
                .HasForeignKey(p => p.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // WellnessProgramRegistration -> WellnessProgram (cascade delete)
            builder.Entity<WellnessProgramRegistration>()
                .HasOne(r => r.WellnessProgram)
                .WithMany(w => w.Registrations)
                .HasForeignKey(r => r.WellnessProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // WellnessProgramRegistration -> Employee (restrict delete)
            builder.Entity<WellnessProgramRegistration>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
