using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderedComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClosedReplenishmentAlerts",
                columns: table => new
                {
                    ClosedAlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalAlertId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ShelfId = table.Column<long>(type: "bigint", nullable: false),
                    PredictedDepletionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UrgencyLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FulfillmentNote = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosedReplenishmentAlerts", x => x.ClosedAlertId);
                    table.ForeignKey(
                        name: "FK_ClosedReplenishmentAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClosedReplenishmentAlerts_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "ShelfId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClosedReplenishmentAlerts_ProductId",
                table: "ClosedReplenishmentAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ClosedReplenishmentAlerts_ShelfId",
                table: "ClosedReplenishmentAlerts",
                column: "ShelfId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClosedReplenishmentAlerts");
        }
    }
}
