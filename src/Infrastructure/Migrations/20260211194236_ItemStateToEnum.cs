using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Misa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ItemStateToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_States_StateId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropIndex(
                name: "IX_Items_StateId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Synopsis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "States",
                columns: new[] { "Id", "Name", "Synopsis" },
                values: new object[,]
                {
                    { 1, "Draft", "Entwurf; noch nie daran gearbeitet" },
                    { 2, "Undefined", "Unklar; muss präzisiert werden" },
                    { 3, "Scheduled", "Geplant für einen zukünftigen Zeitpunkt" },
                    { 4, "InProgress", "Bereits bearbeitet, aktuell keine aktive Session" },
                    { 5, "Active", "Aktive Session läuft" },
                    { 6, "Paused", "Session pausiert (max. 6h, danach Auto-Fortsetzung)" },
                    { 7, "Pending", "Zurückgestellt; lange nicht bearbeitet" },
                    { 8, "WaitForResponse", "Wartet auf Rückmeldung einer Person oder Stelle" },
                    { 9, "BlockedByRelationship", "Blockiert durch Relation oder Abhängigkeit" },
                    { 10, "Done", "Erfolgreich abgeschlossen" },
                    { 11, "Canceled", "Abgebrochen; nicht weiter erforderlich" },
                    { 12, "Failed", "Gescheitert; Ziel nicht erreicht" },
                    { 13, "Expired", "Automatisch abgelaufen (Deadline überschritten)" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_StateId",
                table: "Items",
                column: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_States_StateId",
                table: "Items",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
