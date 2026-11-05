using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UyumEslestirme.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfilGorunurlugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut profiller gorunur kalir; gizlemek kullanicinin kendi secimidir.
            migrationBuilder.AddColumn<bool>(
                name: "Gorunur",
                table: "Profiller",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gorunur",
                table: "Profiller");
        }
    }
}
