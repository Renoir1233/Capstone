using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWellnessPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WellnessPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProgramDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParticipantCapacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WellnessProgramRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WellnessProgramId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessProgramRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WellnessProgramRegistrations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WellnessProgramRegistrations_WellnessPrograms_WellnessProgramId",
                        column: x => x.WellnessProgramId,
                        principalTable: "WellnessPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WellnessProgramRegistrations_EmployeeId",
                table: "WellnessProgramRegistrations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WellnessProgramRegistrations_WellnessProgramId",
                table: "WellnessProgramRegistrations",
                column: "WellnessProgramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WellnessProgramRegistrations");

            migrationBuilder.DropTable(
                name: "WellnessPrograms");
        }
    }
}
