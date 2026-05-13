using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRHelper.Data.Migrations
{
    /// <inheritdoc />
    public partial class LearningMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LearningMaterial",
                table: "JobPositions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningMaterial",
                table: "JobPositions");
        }
    }
}
