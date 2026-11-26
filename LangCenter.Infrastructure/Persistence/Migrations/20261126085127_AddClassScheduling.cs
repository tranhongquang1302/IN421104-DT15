using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LangCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "instructors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instructors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_sessions", x => x.Id);
                    table.CheckConstraint("CK_class_sessions_TimeRange", "\"EndsAt\" > \"StartsAt\"");
                    table.ForeignKey(
                        name: "FK_class_sessions_instructors_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sessions_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sessions_training_classes_TrainingClassId",
                        column: x => x.TrainingClassId,
                        principalTable: "training_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_InstructorId_StartsAt",
                table: "class_sessions",
                columns: new[] { "InstructorId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_RoomId_StartsAt",
                table: "class_sessions",
                columns: new[] { "RoomId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_StartsAt",
                table: "class_sessions",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_TrainingClassId_StartsAt",
                table: "class_sessions",
                columns: new[] { "TrainingClassId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_instructors_Code",
                table: "instructors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_Code",
                table: "rooms",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "class_sessions");

            migrationBuilder.DropTable(
                name: "instructors");

            migrationBuilder.DropTable(
                name: "rooms");
        }
    }
}
