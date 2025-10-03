using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RobentexService.Migrations
{
    /// <inheritdoc />
    public partial class notlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequestNote_ServiceRequests_ServiceRequestId",
                table: "ServiceRequestNote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceRequestNote",
                table: "ServiceRequestNote");

            migrationBuilder.RenameTable(
                name: "ServiceRequestNote",
                newName: "ServiceRequestNotes");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRequestNote_ServiceRequestId",
                table: "ServiceRequestNotes",
                newName: "IX_ServiceRequestNotes_ServiceRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceRequestNotes",
                table: "ServiceRequestNotes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequestNotes_ServiceRequests_ServiceRequestId",
                table: "ServiceRequestNotes",
                column: "ServiceRequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequestNotes_ServiceRequests_ServiceRequestId",
                table: "ServiceRequestNotes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceRequestNotes",
                table: "ServiceRequestNotes");

            migrationBuilder.RenameTable(
                name: "ServiceRequestNotes",
                newName: "ServiceRequestNote");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRequestNotes_ServiceRequestId",
                table: "ServiceRequestNote",
                newName: "IX_ServiceRequestNote_ServiceRequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceRequestNote",
                table: "ServiceRequestNote",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequestNote_ServiceRequests_ServiceRequestId",
                table: "ServiceRequestNote",
                column: "ServiceRequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
