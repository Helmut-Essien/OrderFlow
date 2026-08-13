using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenShopUserConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_DisplayNameNotEmpty",
                table: "Users",
                sql: "char_length(btrim(\"DisplayName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_EmailNotEmpty",
                table: "Users",
                sql: "char_length(btrim(\"Email\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordHashNotEmpty",
                table: "Users",
                sql: "char_length(btrim(\"PasswordHash\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "\"Role\" IN ('Owner', 'Assistant')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shops_LicenseLookupHashLength",
                table: "Shops",
                sql: "char_length(\"LicenseLookupHash\") = 64");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shops_NameNotEmpty",
                table: "Shops",
                sql: "char_length(btrim(\"Name\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shops_PlanNameNotEmpty",
                table: "Shops",
                sql: "char_length(btrim(\"PlanName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shops_WhatsAppConnectionStatus",
                table: "Shops",
                sql: "\"WhatsAppConnectionStatus\" IN ('Disconnected', 'Connected', 'Error')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_DisplayNameNotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_EmailNotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordHashNotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shops_LicenseLookupHashLength",
                table: "Shops");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shops_NameNotEmpty",
                table: "Shops");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shops_PlanNameNotEmpty",
                table: "Shops");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shops_WhatsAppConnectionStatus",
                table: "Shops");
        }
    }
}
