using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLowStockAlertAndTickerQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_alerted_available_quantity",
                schema: "inventory",
                table: "stocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "low_stock_alert_sent_at",
                schema: "inventory",
                table: "stocks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_alerted_available_quantity",
                schema: "inventory",
                table: "stocks");

            migrationBuilder.DropColumn(
                name: "low_stock_alert_sent_at",
                schema: "inventory",
                table: "stocks");
        }
    }
}
