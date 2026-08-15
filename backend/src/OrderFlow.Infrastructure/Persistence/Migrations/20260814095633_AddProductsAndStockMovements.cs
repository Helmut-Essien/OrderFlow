using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsAndStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_LowStockThresholdRange", "\"LowStockThreshold\" >= 0 AND \"LowStockThreshold\" <= 99999999");
                    table.CheckConstraint("CK_Products_NameNotEmpty", "char_length(btrim(\"Name\")) > 0");
                    table.CheckConstraint("CK_Products_PriceNonNegative", "\"Price\" >= 0 AND \"Price\" <= 999999999.99");
                    table.CheckConstraint("CK_Products_SkuNotEmpty", "char_length(btrim(\"Sku\")) > 0");
                    table.CheckConstraint("CK_Products_StockRange", "\"Stock\" >= 0 AND \"Stock\" <= 99999999");
                    table.CheckConstraint("CK_Products_VersionPositive", "\"Version\" >= 1");
                    table.ForeignKey(
                        name: "FK_Products_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                    ResultingStock = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovements_ResultingStockRange", "\"ResultingStock\" >= 0 AND \"ResultingStock\" <= 99999999");
                    table.CheckConstraint("CK_StockMovements_Type", "\"Type\" IN ('Adjustment', 'Reserve', 'Deduct', 'Release')");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ShopId",
                table: "Products",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ShopId_Category",
                table: "Products",
                columns: new[] { "ShopId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ShopId_Sku",
                table: "Products",
                columns: new[] { "ShopId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ShopId",
                table: "StockMovements",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ShopId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "ShopId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
