using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LangCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAdmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_admissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_admissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exam_admissions_enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exam_admissions_EnrollmentId",
                table: "exam_admissions",
                column: "EnrollmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_admissions");
        }
    }
}
