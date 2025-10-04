using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddKappiModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anonymous_galleries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    short_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    access_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anonymous_galleries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "galleries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    short_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_galleries", x => x.id);
                    table.ForeignKey(
                        name: "fk_galleries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anonymous_gallery_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gallery_id = table.Column<Guid>(type: "uuid", nullable: true),
                    s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    media_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_anonymous_galleries_anonymous_gallery_id",
                        column: x => x.anonymous_gallery_id,
                        principalTable: "anonymous_galleries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_media_galleries_gallery_id",
                        column: x => x.gallery_id,
                        principalTable: "galleries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_anonymous_galleries_access_key",
                table: "anonymous_galleries",
                column: "access_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_anonymous_galleries_expires_at",
                table: "anonymous_galleries",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_anonymous_galleries_short_code",
                table: "anonymous_galleries",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_galleries_short_code",
                table: "galleries",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_galleries_user_id",
                table: "galleries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_anonymous_gallery_id",
                table: "media",
                column: "anonymous_gallery_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_gallery_id",
                table: "media",
                column: "gallery_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_s3key",
                table: "media",
                column: "s3key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media");

            migrationBuilder.DropTable(
                name: "anonymous_galleries");

            migrationBuilder.DropTable(
                name: "galleries");
        }
    }
}
