using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPricePrecisionAndAddStockMovementFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(11,2)",
                precision: 11,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedByUserId",
                table: "StockMovements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShopId_Status_CreatedAt",
                table: "Orders",
                columns: new[] { "ShopId", "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Users_CreatedByUserId",
                table: "StockMovements",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Users_CreatedByUserId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CreatedByUserId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShopId_Status_CreatedAt",
                table: "Orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,2)",
                oldPrecision: 11,
                oldScale: 2);
        }
    }
}
