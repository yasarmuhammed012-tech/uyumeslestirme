using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Data;

namespace UyumEslestirme.Business;

public static class UygulamaBaslatici
{
#if DEBUG
    // Yalnizca gelistirme derlemesinde; yayin derlemesinde demo hesap olusmaz.
    public const string DemoParola = "Demo1234";
#endif

    public static string VeritabaniYolu()
    {
        var klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UyumEslestirme");
        Directory.CreateDirectory(klasor);
        return Path.Combine(klasor, "uyum.db");
    }

    public static UygulamaDbContext ContextOlustur()
    {
        var options = new DbContextOptionsBuilder<UygulamaDbContext>()
            .UseSqlite($"Data Source={VeritabaniYolu()}")
            .Options;
        return new UygulamaDbContext(options);
    }

    public static void Baslat(TimeProvider saat)
    {
        using var db = ContextOlustur();
        db.Database.Migrate();
        VeriTohumlayici.SorulariTohumla(db);
#if DEBUG
        var hash = ParolaHasleyici.Hashle(DemoParola);
        VeriTohumlayici.Tohumla(db, hash, hash, saat.GetUtcNow().UtcDateTime);
#endif
    }
}
