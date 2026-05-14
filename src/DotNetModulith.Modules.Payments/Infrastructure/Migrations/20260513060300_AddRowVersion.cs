using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "payments",
                table: "payments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE OR REPLACE FUNCTION payments.update_row_version()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.row_version = gen_random_bytes(8);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_update_row_version
                BEFORE UPDATE ON payments.payments
                FOR EACH ROW
                EXECUTE FUNCTION payments.update_row_version();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_update_row_version ON payments.payments;
                DROP FUNCTION IF EXISTS payments.update_row_version();
                """);

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "payments",
                table: "payments");
        }
    }
}
