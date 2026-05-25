using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolationToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_customer_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_status",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_order_id",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                schema: "orders",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                schema: "orders",
                table: "order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_tenant_id_customer_id",
                schema: "orders",
                table: "orders",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_tenant_id_status",
                schema: "orders",
                table: "orders",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_order_id",
                schema: "orders",
                table: "order_lines",
                columns: new[] { "order_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_tenant_id_product_id",
                schema: "orders",
                table: "order_lines",
                columns: new[] { "tenant_id", "product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_tenant_id_customer_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_tenant_id_status",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_order_id",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "IX_order_lines_tenant_id_product_id",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "orders",
                table: "order_lines");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id",
                schema: "orders",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                schema: "orders",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_order_id",
                schema: "orders",
                table: "order_lines",
                column: "order_id");
        }
    }
}
