using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PriorityHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "item_priorities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                table: "item_priorities",
                columns: new[] { "id", "name", "sort_order", "synopsis" },
                values: new object[,]
                {
                    { 1, "None", 1, "Keine Priorität vergeben" },
                    { 2, "Low", 2, "Geringe Wichtigkeit" },
                    { 3, "Medium", 3, "Normale Priorität" },
                    { 4, "High", 4, "Wichtig; zeitnah bearbeiten" },
                    { 5, "Urgent", 5, "Dringend; sofortige Aktion" },
                    { 6, "Critical", 6, "Kritische Eskalation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "item_priorities",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "item_priorities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
