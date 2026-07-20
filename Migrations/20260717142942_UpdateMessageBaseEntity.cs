using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrendyolMiniApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "Messages",
                newName: "CreatedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Messages",
                newName: "SentAt");
        }
    }
}
