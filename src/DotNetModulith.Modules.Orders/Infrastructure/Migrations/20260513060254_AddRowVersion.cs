using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "orders",
                table: "orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE OR REPLACE FUNCTION orders.update_row_version()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.row_version = gen_random_bytes(8);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_update_row_version
                BEFORE UPDATE ON orders.orders
                FOR EACH ROW
                EXECUTE FUNCTION orders.update_row_version();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_update_row_version ON orders.orders;
                DROP FUNCTION IF EXISTS orders.update_row_version();
                """);

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "total_amount",
                schema: "orders",
                table: "orders");
        }
    }
}
