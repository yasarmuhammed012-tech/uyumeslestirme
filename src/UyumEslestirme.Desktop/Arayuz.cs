using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

internal static class Arayuz
{
    // Is katmaninin kullaniciya yonelik hatalarini tek yerde mesaja cevirir.
    public static bool Dene(Action islem)
    {
        try
        {
            islem();
            return true;
        }
        catch (IsKuraliHatasi hata)
        {
            MessageBox.Show(hata.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (YetkisizErisimHatasi hata)
        {
            MessageBox.Show(hata.Message, "Yetkisiz işlem", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return false;
    }

    public static string KategoriAdi(SoruKategorisi kategori) => kategori switch
    {
        SoruKategorisi.UykuDuzeni => "Gece düzeni",
        SoruKategorisi.Temizlik => "Düzen ve temizlik",
        SoruKategorisi.Sosyallik => "Sosyallik",
        SoruKategorisi.Gurultu => "Sessizlik isteği",
        SoruKategorisi.Ders => "Evde ders çalışma",
        _ => kategori.ToString(),
    };

    public static string RolAdi(Rol rol) => rol == Rol.Yonetici ? "Yönetici" : "Öğrenci";

    // Kisi listelerinde (Arkadas bul, Eslesmelerim) kullanilan ortak kart.
    public static KartPanel KisiKarti(string ad, IReadOnlyList<string> satirlar, double? uyum, params Control[] eylemler)
    {
        var bilgiler = Tema.Yigin();
        bilgiler.Controls.Add(Tema.Etiket(ad, Tema.AltBaslik, Tema.Metin));
        foreach (var satir in satirlar)
            bilgiler.Controls.Add(Tema.Etiket(satir, Tema.Kucuk, Tema.SolukMetin, 520));

        var sag = Tema.Yigin();
        if (uyum is not null)
        {
            sag.Controls.Add(Tema.Etiket($"%{uyum:0} uyum", Tema.AltBaslik, Tema.Birincil));
            sag.Controls.Add(new Cubuk(uyum.Value / 100) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) });
        }
        if (eylemler.Length > 0)
        {
            var dugmeler = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0) };
            foreach (var eylem in eylemler)
            {
                eylem.Margin = new Padding(0, 0, 6, 0);
                dugmeler.Controls.Add(eylem);
            }
            sag.Controls.Add(dugmeler);
        }

        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        tablo.Controls.Add(bilgiler);
        tablo.Controls.Add(sag);

        var kart = new KartPanel
        {
            Height = Math.Max(70 + satirlar.Sum(s => s.Length > 80 ? 40 : 22), eylemler.Length > 0 ? 130 : 100),
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 14, 18, 10),
        };
        kart.Controls.Add(tablo);
        return kart;
    }

    public static Label BolumBasligi(string metin)
    {
        var etiket = Tema.Etiket(metin, Tema.AltBaslik, Tema.Metin);
        etiket.Margin = new Padding(0, 12, 0, 8);
        return etiket;
    }

    public static Label DurumEtiketi(string metin, Color renk)
    {
        var etiket = Tema.Etiket(metin, Tema.KucukKalin, renk);
        etiket.Margin = new Padding(0, 10, 0, 0);
        return etiket;
    }

    public static FlowLayoutPanel KartListesi() => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
    };

    public static void ListeyiTemizle(FlowLayoutPanel liste)
    {
        while (liste.Controls.Count > 0)
            liste.Controls[0].Dispose();
    }

    public static void KartlariGenislet(FlowLayoutPanel liste)
    {
        var genislik = Math.Max(liste.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4, 560);
        foreach (Control kart in liste.Controls)
        {
            if (kart is KartPanel)
                kart.Width = genislik;
        }
    }
}
