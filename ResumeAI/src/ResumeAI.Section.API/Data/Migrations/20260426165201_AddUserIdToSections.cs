using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeAI.Section.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ResumeSections",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ResumeSections");
        }
    }
}
