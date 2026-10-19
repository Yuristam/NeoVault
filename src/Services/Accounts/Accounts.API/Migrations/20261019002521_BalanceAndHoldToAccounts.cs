using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.API.Migrations
{
    /// <inheritdoc />
    public partial class BalanceAndHoldToAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "balance",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "hold",
                table: "accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "balance",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "hold",
                table: "accounts");
        }
    }
}
