using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.API.Migrations
{
    /// <inheritdoc />
    public partial class CreateOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "accounts",
                newName: "created_at");

            migrationBuilder.CreateTable(
                name: "operations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("id", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operations");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "accounts",
                newName: "CreatedAt");
        }
    }
}
