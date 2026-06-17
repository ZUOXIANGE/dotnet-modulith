using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Borrowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "borrowing");

            migrationBuilder.CreateTable(
                name: "borrowing_records",
                schema: "borrowing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BorrowDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RenewalCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_borrowing_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_borrowing_records_BookId",
                schema: "borrowing",
                table: "borrowing_records",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_borrowing_records_DueDate",
                schema: "borrowing",
                table: "borrowing_records",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_borrowing_records_MemberId",
                schema: "borrowing",
                table: "borrowing_records",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_borrowing_records_Status",
                schema: "borrowing",
                table: "borrowing_records",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "borrowing_records",
                schema: "borrowing");
        }
    }
}
