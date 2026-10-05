using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipeImageOnePrimaryIgnoresSoftDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_recipe_images_one_primary",
                table: "RecipeImages");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_images_one_primary",
                table: "RecipeImages",
                column: "RecipeId",
                unique: true,
                filter: "\"IsPrimary\" = true AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_recipe_images_one_primary",
                table: "RecipeImages");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_images_one_primary",
                table: "RecipeImages",
                column: "RecipeId",
                unique: true,
                filter: "\"IsPrimary\" = true");
        }
    }
}
