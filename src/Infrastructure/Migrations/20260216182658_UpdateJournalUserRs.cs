using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJournalUserRs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deadline_Items_ItemId",
                table: "Deadline");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Outbox",
                table: "Outbox");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deadline",
                table: "Deadline");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Outbox",
                newName: "Outboxes");

            migrationBuilder.RenameTable(
                name: "Deadline",
                newName: "Deadlines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Outboxes",
                table: "Outboxes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deadlines",
                table: "Deadlines",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deadlines_Items_ItemId",
                table: "Deadlines",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_journals_Users_UserId",
                table: "journals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deadlines_Items_ItemId",
                table: "Deadlines");

            migrationBuilder.DropForeignKey(
                name: "FK_journals_Users_UserId",
                table: "journals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Outboxes",
                table: "Outboxes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deadlines",
                table: "Deadlines");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "Outboxes",
                newName: "Outbox");

            migrationBuilder.RenameTable(
                name: "Deadlines",
                newName: "Deadline");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Outbox",
                table: "Outbox",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deadline",
                table: "Deadline",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deadline_Items_ItemId",
                table: "Deadline",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
