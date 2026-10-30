using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var saat = TimeProvider.System;
        UygulamaBaslatici.Baslat(saat);

        using var db = UygulamaBaslatici.ContextOlustur();
        var servisler = new Servisler(db, saat);

        // Cikis yapilinca veya oturum dolunca giris ekranina donulur.
        while (true)
        {
            using var giris = new GirisForm(servisler.Kimlik);
            if (giris.ShowDialog() != DialogResult.OK)
                return;

            using var ana = new AnaForm(servisler);
            ana.ShowDialog();
            servisler.Kimlik.CikisYap();
            if (!ana.CikisIstendi)
                return;
        }
    }
}
