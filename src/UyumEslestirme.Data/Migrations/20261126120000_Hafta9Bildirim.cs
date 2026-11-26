using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UyumEslestirme.Data.Migrations
{
    /// <inheritdoc />
    public partial class Hafta9Bildirim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut bildirimler uygulama ici kalir: tur 0, harici gonderim kapali (0).
            migrationBuilder.AddColumn<int>(
                name: "Tur",
                table: "Bildirimler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HariciDurum",
                table: "Bildirimler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HariciHata",
                table: "Bildirimler",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BildirimTercihleri",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    EslesmeBildirimi = table.Column<bool>(type: "INTEGER", nullable: false),
                    MesajBildirimi = table.Column<bool>(type: "INTEGER", nullable: false),
                    TelefonKonusu = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BildirimTercihleri", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_BildirimTercihleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BildirimTercihleri");
            migrationBuilder.DropColumn(name: "Tur", table: "Bildirimler");
            migrationBuilder.DropColumn(name: "HariciDurum", table: "Bildirimler");
            migrationBuilder.DropColumn(name: "HariciHata", table: "Bildirimler");
        }
    }
}
