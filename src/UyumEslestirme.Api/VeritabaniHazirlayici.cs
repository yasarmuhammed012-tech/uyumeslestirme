using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api;

// Acilista ortak veritabanini hazirlar: mevcut migration'lari uygular, anket sorularini ekler,
// istenirse ornek veriyi ve ilk yonetici hesabini olusturur. Her acilista guvenle tekrar calisir.
public static class VeritabaniHazirlayici
{
    public const string GelistirmeDemoParolasi = "Demo1234";

    public static void KlasoruOlustur(string baglantiDizesi)
    {
        var dosya = new SqliteConnectionStringBuilder(baglantiDizesi).DataSource;
        var klasor = Path.GetDirectoryName(Path.GetFullPath(dosya));
        if (!string.IsNullOrEmpty(klasor))
            Directory.CreateDirectory(klasor);
    }

    public static void Hazirla(IServiceProvider servisler)
    {
        using var kapsam = servisler.CreateScope();
        var sp = kapsam.ServiceProvider;
        var db = sp.GetRequiredService<UygulamaDbContext>();
        var ayar = sp.GetRequiredService<IConfiguration>();
        var ortam = sp.GetRequiredService<IHostEnvironment>();
        var simdi = sp.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var gunluk = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(VeritabaniHazirlayici));

        db.Database.Migrate();
        VeriTohumlayici.SorulariTohumla(db);

        if (ayar.GetValue<bool>("Tohum:DemoVerisi"))
        {
            // Gelistirmede demo hesaplar Demo1234 ile girilebilir. Yayinda ornek ogrenciler listede gorunur
            // ama giris yapilamaz: parolasi herkesce bilinen bir hesap gercek kullanicilarin e-postasini goremesin.
            var ogrenci = ortam.IsDevelopment() ? ParolaHasleyici.Hashle(GelistirmeDemoParolasi) : VeriTohumlayici.GirisKapaliHash;
            var yonetici = ortam.IsDevelopment() ? ogrenci : VeriTohumlayici.GirisKapaliHash;
            VeriTohumlayici.Tohumla(db, ogrenci, yonetici, simdi);
        }

        // Yayindaki ilk yonetici hesabi ortam degiskenlerinden gelir (Yonetici__Eposta, Yonetici__Parola).
        var eposta = ayar["Yonetici:Eposta"]?.Trim().ToLowerInvariant();
        var parola = ayar["Yonetici:Parola"];
        if (!string.IsNullOrEmpty(eposta) && !string.IsNullOrEmpty(parola) && !db.Kullanicilar.Any(k => k.Eposta == eposta))
        {
            if (parola.Length < 12)
                throw new InvalidOperationException("Yonetici:Parola en az 12 karakter olmalıdır.");
            db.Kullanicilar.Add(new Kullanici
            {
                Eposta = eposta,
                AdSoyad = ayar["Yonetici:AdSoyad"] ?? "Yönetici",
                ParolaHash = ParolaHasleyici.Hashle(parola),
                Rol = Rol.Yonetici,
                OlusturmaTarihi = simdi,
            });
            db.SaveChanges();
            gunluk.LogInformation("Yönetici hesabı oluşturuldu.");
        }
    }
}
