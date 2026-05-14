using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "inventory",
                table: "stocks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE OR REPLACE FUNCTION inventory.update_row_version()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.row_version = gen_random_bytes(8);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_update_row_version
                BEFORE UPDATE ON inventory.stocks
                FOR EACH ROW
                EXECUTE FUNCTION inventory.update_row_version();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_update_row_version ON inventory.stocks;
                DROP FUNCTION IF EXISTS inventory.update_row_version();
                """);

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "inventory",
                table: "stocks");
        }
    }
}
