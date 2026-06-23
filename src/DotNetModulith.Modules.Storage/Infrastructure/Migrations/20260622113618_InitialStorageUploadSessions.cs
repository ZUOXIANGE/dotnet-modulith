using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetModulith.Modules.Storage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialStorageUploadSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.CreateTable(
                name: "upload_sessions",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    declared_content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_expires_at",
                schema: "storage",
                table: "upload_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_object_key",
                schema: "storage",
                table: "upload_sessions",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_owner_user_id",
                schema: "storage",
                table: "upload_sessions",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_upload_sessions_purpose_status",
                schema: "storage",
                table: "upload_sessions",
                columns: new[] { "purpose", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "upload_sessions",
                schema: "storage");
        }
    }
}
