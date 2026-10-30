using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UyumEslestirme.Data.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnketSorulari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kategori = table.Column<int>(type: "INTEGER", nullable: false),
                    Metin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sira = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnketSorulari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ParolaHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AdSoyad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    BasarisizGiris = table.Column<int>(type: "INTEGER", nullable: false),
                    KilitBitis = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnketCevaplari",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoruId = table.Column<int>(type: "INTEGER", nullable: false),
                    Deger = table.Column<int>(type: "INTEGER", nullable: false),
                    Agirlik = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnketCevaplari", x => new { x.KullaniciId, x.SoruId });
                    table.CheckConstraint("CK_AnketCevabi_Agirlik", "\"Agirlik\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_AnketCevabi_Deger", "\"Deger\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AnketCevaplari_AnketSorulari_SoruId",
                        column: x => x.SoruId,
                        principalTable: "AnketSorulari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnketCevaplari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Metin = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Okundu = table.Column<bool>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eslesmeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GonderenId = table.Column<int>(type: "INTEGER", nullable: false),
                    AliciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eslesmeler", x => x.Id);
                    table.CheckConstraint("CK_Eslesme_Farkli", "\"GonderenId\" <> \"AliciId\"");
                    table.ForeignKey(
                        name: "FK_Eslesmeler_Kullanicilar_AliciId",
                        column: x => x.AliciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Eslesmeler_Kullanicilar_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ilanlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SahipId = table.Column<int>(type: "INTEGER", nullable: false),
                    Baslik = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Sehir = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AylikKira = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Aktif = table.Column<bool>(type: "INTEGER", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ilanlar", x => x.Id);
                    table.CheckConstraint("CK_Ilan_Kira", "\"AylikKira\" >= 0");
                    table.ForeignKey(
                        name: "FK_Ilanlar_Kullanicilar_SahipId",
                        column: x => x.SahipId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesajlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GonderenId = table.Column<int>(type: "INTEGER", nullable: false),
                    AliciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Icerik = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Okundu = table.Column<bool>(type: "INTEGER", nullable: false),
                    GonderimTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesajlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Kullanicilar_AliciId",
                        column: x => x.AliciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mesajlar_Kullanicilar_GonderenId",
                        column: x => x.GonderenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiller",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false),
                    Bolum = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DogumYili = table.Column<int>(type: "INTEGER", nullable: false),
                    Sehir = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Hakkinda = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiller", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_Profiller_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UyumSkorlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KullaniciAId = table.Column<int>(type: "INTEGER", nullable: false),
                    KullaniciBId = table.Column<int>(type: "INTEGER", nullable: false),
                    Skor = table.Column<double>(type: "REAL", nullable: false),
                    HesaplamaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UyumSkorlari", x => x.Id);
                    table.CheckConstraint("CK_UyumSkoru_Aralik", "\"Skor\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_UyumSkoru_Sira", "\"KullaniciAId\" < \"KullaniciBId\"");
                    table.ForeignKey(
                        name: "FK_UyumSkorlari_Kullanicilar_KullaniciAId",
                        column: x => x.KullaniciAId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UyumSkorlari_Kullanicilar_KullaniciBId",
                        column: x => x.KullaniciBId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnketCevaplari_SoruId",
                table: "AnketCevaplari",
                column: "SoruId");

            migrationBuilder.CreateIndex(
                name: "IX_AnketSorulari_Sira",
                table: "AnketSorulari",
                column: "Sira",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId_Okundu",
                table: "Bildirimler",
                columns: new[] { "KullaniciId", "Okundu" });

            migrationBuilder.CreateIndex(
                name: "IX_Eslesmeler_AliciId",
                table: "Eslesmeler",
                column: "AliciId");

            migrationBuilder.CreateIndex(
                name: "IX_Eslesmeler_GonderenId_AliciId",
                table: "Eslesmeler",
                columns: new[] { "GonderenId", "AliciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ilanlar_SahipId",
                table: "Ilanlar",
                column: "SahipId");

            migrationBuilder.CreateIndex(
                name: "IX_Ilanlar_Sehir_Aktif",
                table: "Ilanlar",
                columns: new[] { "Sehir", "Aktif" });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_AliciId_GonderimTarihi",
                table: "Mesajlar",
                columns: new[] { "AliciId", "GonderimTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Mesajlar_GonderenId",
                table: "Mesajlar",
                column: "GonderenId");

            migrationBuilder.CreateIndex(
                name: "IX_UyumSkorlari_KullaniciAId_KullaniciBId",
                table: "UyumSkorlari",
                columns: new[] { "KullaniciAId", "KullaniciBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UyumSkorlari_KullaniciBId",
                table: "UyumSkorlari",
                column: "KullaniciBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnketCevaplari");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "Eslesmeler");

            migrationBuilder.DropTable(
                name: "Ilanlar");

            migrationBuilder.DropTable(
                name: "Mesajlar");

            migrationBuilder.DropTable(
                name: "Profiller");

            migrationBuilder.DropTable(
                name: "UyumSkorlari");

            migrationBuilder.DropTable(
                name: "AnketSorulari");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
