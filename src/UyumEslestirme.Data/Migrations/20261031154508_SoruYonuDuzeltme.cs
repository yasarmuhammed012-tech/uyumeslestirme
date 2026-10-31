using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UyumEslestirme.Data.Migrations
{
    /// <summary>
    /// Ayni kategorideki sorulari ayni yone cevirir; ters yonlu sorularin ortalamasi anlamsiz olur.
    /// Yeni kurulumda tablo bos oldugu icin etkisizdir, sorular VeriTohumlayici'dan gelir.
    /// </summary>
    public partial class SoruYonuDuzeltme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AnketSorulari SET Metin = 'Sabahları geç kalkarım.' WHERE Sira = 2;");
            migrationBuilder.Sql("UPDATE AnketSorulari SET Metin = 'Evde sessiz bir ortam tercih ederim.' WHERE Sira = 8;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AnketSorulari SET Metin = 'Sabahları erken kalkarım.' WHERE Sira = 2;");
            migrationBuilder.Sql("UPDATE AnketSorulari SET Metin = 'Evde yüksek sesle müzik dinlemekten hoşlanırım.' WHERE Sira = 8;");
        }
    }
}
