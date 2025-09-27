using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inventoryreporttable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryReport",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ShelfId = table.Column<long>(type: "bigint", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityOnShelf = table.Column<int>(type: "int", nullable: false),
                    QuantityRestocked = table.Column<int>(type: "int", nullable: true),
                    AlertTriggered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReport", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_InventoryReport_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReport_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "ShelfId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReport_ProductId_ShelfId_ReportDate",
                table: "InventoryReport",
                columns: new[] { "ProductId", "ShelfId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReport_ShelfId",
                table: "InventoryReport",
                column: "ShelfId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryReport");
        }
    }
}
