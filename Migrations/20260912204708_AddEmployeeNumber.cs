using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeNumber",
                table: "OrganizationMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "OrganizationMembers");
        }
    }
}
