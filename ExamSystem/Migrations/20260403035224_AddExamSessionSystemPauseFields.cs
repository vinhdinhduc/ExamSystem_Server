using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSessionSystemPauseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeatAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionResponseSnapshotJson",
                table: "ExamSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPauseReason",
                table: "ExamSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SystemPausedAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHeartbeatAt",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "SessionResponseSnapshotJson",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "SystemPauseReason",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "SystemPausedAt",
                table: "ExamSessions");
        }
    }
}
