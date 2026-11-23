using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LangCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MinimumEnrollment = table.Column<int>(type: "integer", nullable: false),
                    TuitionFee = table.Column<decimal>(type: "numeric(14,0)", precision: 14, scale: 0, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_classes", x => x.Id);
                    table.CheckConstraint("CK_training_classes_MinimumEnrollment", "\"MinimumEnrollment\" > 0");
                    table.CheckConstraint("CK_training_classes_TuitionFee", "\"TuitionFee\" >= 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_classes");
        }
    }
}
