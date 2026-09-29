using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFtsAndGinIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D18 / Task B3 (TV2): PostgreSQL FTS unaccent, pg_trgm và GIN index
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IDX_Recipes_Title_Description_Trgm"
                ON "Recipes" USING gin (("Title" || ' ' || "Description") gin_trgm_ops);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IDX_Recipes_Title_Description_Trgm";""");
        }
    }
}
