using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeNator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDistractionEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistractionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WindowTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistractionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistractionEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DistractionEvents_StudySessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistractionEvents_SessionId",
                table: "DistractionEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DistractionEvents_UserId_OccurredAt",
                table: "DistractionEvents",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistractionEvents");
        }
    }
}
