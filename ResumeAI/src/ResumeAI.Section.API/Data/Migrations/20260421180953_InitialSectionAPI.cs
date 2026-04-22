using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ResumeAI.Section.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSectionAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResumeSections",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResumeId = table.Column<int>(type: "integer", nullable: false),
                    SectionType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    AiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeSections", x => x.SectionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResumeSections_ResumeId",
                table: "ResumeSections",
                column: "ResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumeSections_ResumeId_DisplayOrder",
                table: "ResumeSections",
                columns: new[] { "ResumeId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResumeSections");
        }
    }
}
