using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeAI.Export.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialExportAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "text", nullable: false),
                    ResumeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: true),
                    FileSizeKb = table.Column<long>(type: "bigint", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    Customizations = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.JobId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExpiresAt",
                table: "ExportJobs",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ResumeId",
                table: "ExportJobs",
                column: "ResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_UserId",
                table: "ExportJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportJobs");
        }
    }
}
