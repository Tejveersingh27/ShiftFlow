using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxWeeklyHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxWeeklyHours",
                table: "OrganizationMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxWeeklyHours",
                table: "OrganizationMembers");
        }
    }
}
