using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeAI.JobMatch.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "JobMatches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobMatches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "JobMatches");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobMatches");
        }
    }
}
