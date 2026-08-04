using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrendyolMiniApi.Migrations
{
    /// <inheritdoc />
    public partial class SoftDelete2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Favorites");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Favorites",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
