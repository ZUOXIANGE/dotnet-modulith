using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.JobHost.Infrastructure.TickerQMigrations
{
    /// <inheritdoc />
    public partial class UpdateTickerQModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemPaused",
                schema: "ticker",
                table: "CronTickers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemPaused",
                schema: "ticker",
                table: "CronTickers");
        }
    }
}
