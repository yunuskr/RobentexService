using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RobentexService.Migrations
{
    /// <inheritdoc />
    public partial class DeleteCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ServiceRequests",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ServiceRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ServiceRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ServiceRequestNotes",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ServiceRequestNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ServiceRequestNotes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ServiceRequestNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ServiceRequestNotes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ServiceRequestNotes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ServiceRequestNotes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ServiceRequestNotes");
        }
    }
}
