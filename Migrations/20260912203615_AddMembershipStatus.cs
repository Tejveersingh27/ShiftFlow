using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrganizationMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrganizationMembers");
        }
    }
}
