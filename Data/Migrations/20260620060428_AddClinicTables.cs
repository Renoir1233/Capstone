using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medicines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    MinStock = table.Column<int>(type: "int", nullable: false),
                    CriticalStock = table.Column<int>(type: "int", nullable: false),
                    MaxStock = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRestockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDispensedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChiefComplain = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateOfConsultation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VitalBP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalPR = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalRR = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalTemp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalWT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalHT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VitalWC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsSmoker = table.Column<bool>(type: "bit", nullable: false),
                    SticksPerDay = table.Column<int>(type: "int", nullable: true),
                    IsAlcoholDrinker = table.Column<bool>(type: "bit", nullable: false),
                    BottlesAlcohol = table.Column<int>(type: "int", nullable: true),
                    IsBeverageDrinker = table.Column<bool>(type: "bit", nullable: false),
                    BottlesBeverage = table.Column<int>(type: "int", nullable: true),
                    QueueStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MedicineTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicineId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineTransactions_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineTransactions_MedicineId",
                table: "MedicineTransactions",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_EmployeeId",
                table: "Visits",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineTransactions");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "Medicines");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
