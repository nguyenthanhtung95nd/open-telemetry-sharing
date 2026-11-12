using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookmarks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                schema: "bookmarks",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookmark_tag",
                schema: "bookmarks",
                columns: table => new
                {
                    bookmark_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tag_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookmark_tag", x => new { x.bookmark_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_bookmark_tag_bookmarks_bookmark_id",
                        column: x => x.bookmark_id,
                        principalSchema: "bookmarks",
                        principalTable: "bookmarks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bookmark_tag_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "bookmarks",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookmark_tag_tag_id",
                schema: "bookmarks",
                table: "bookmark_tag",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_name",
                schema: "bookmarks",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookmark_tag",
                schema: "bookmarks");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "bookmarks");
        }
    }
}
