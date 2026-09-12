using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "OrganizationMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_DepartmentId",
                table: "OrganizationMembers",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMembers_Departments_DepartmentId",
                table: "OrganizationMembers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMembers_Departments_DepartmentId",
                table: "OrganizationMembers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMembers_DepartmentId",
                table: "OrganizationMembers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "OrganizationMembers");
        }
    }
}
