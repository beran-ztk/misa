using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ItemEntityRs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_entities_EntityId",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_EntityId",
                table: "items");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "items");

            migrationBuilder.AddForeignKey(
                name: "FK_items_entities_id",
                table: "items",
                column: "id",
                principalTable: "entities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_entities_id",
                table: "items");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_items_EntityId",
                table: "items",
                column: "EntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_items_entities_EntityId",
                table: "items",
                column: "EntityId",
                principalTable: "entities",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
