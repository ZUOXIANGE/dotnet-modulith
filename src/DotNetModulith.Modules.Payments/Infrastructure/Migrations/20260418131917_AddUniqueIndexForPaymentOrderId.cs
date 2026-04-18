using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexForPaymentOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_order_id",
                schema: "payments",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_order_id",
                schema: "payments",
                table: "payments",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_order_id",
                schema: "payments",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_order_id",
                schema: "payments",
                table: "payments",
                column: "order_id");
        }
    }
}
