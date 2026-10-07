using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeDiscoveryAndSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    PrepTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CookTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    PrimaryImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_AuthorId",
                table: "Recipes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CategoryId",
                table: "Recipes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedAt",
                table: "Recipes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Difficulty",
                table: "Recipes",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_PublishedAt",
                table: "Recipes",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Slug",
                table: "Recipes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Status",
                table: "Recipes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recipes");
        }
    }
}
