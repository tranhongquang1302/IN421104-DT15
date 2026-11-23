using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LangCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    TuitionDue = table.Column<decimal>(type: "numeric(14,0)", precision: 14, scale: 0, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.Id);
                    table.CheckConstraint("CK_enrollments_TuitionDue", "\"TuitionDue\" >= 0");
                    table.ForeignKey(
                        name: "FK_enrollments_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_training_classes_TrainingClassId",
                        column: x => x.TrainingClassId,
                        principalTable: "training_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_StudentId_TrainingClassId",
                table: "enrollments",
                columns: new[] { "StudentId", "TrainingClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_TrainingClassId",
                table: "enrollments",
                column: "TrainingClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrollments");
        }
    }
}
