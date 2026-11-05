using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

public sealed class EslesmelerSekmesi : UserControl
{
    private readonly EslesmeServisi _eslesme;
    private readonly FlowLayoutPanel _liste = Arayuz.KartListesi();

    public EslesmelerSekmesi(EslesmeServisi eslesme)
    {
        _eslesme = eslesme;

        Controls.Add(_liste);
        Controls.Add(Tema.SayfaBasligi(
            "Eşleşmelerim",
            "Gelen istekleri yanıtlayın. İletişim bilgileri yalnızca iki taraf da kabul edince görünür."));

        _liste.Resize += (_, _) => Arayuz.KartlariGenislet(_liste);
    }

    public void Yenile() => Arayuz.Dene(() =>
    {
        var listeler = _eslesme.Listele();

        _liste.SuspendLayout();
        Arayuz.ListeyiTemizle(_liste);

        Bolum("Gelen istekler", listeler.GelenIstekler, "Bekleyen isteğiniz yok.", GelenEylemleri);
        Bolum("Eşleştiklerim", listeler.Eslesmeler, "Henüz eşleşmeniz yok. Arkadaş bul sayfasından istek gönderebilirsiniz.", _ => []);
        Bolum("Gönderdiğim istekler", listeler.GonderilenIstekler, "Yanıt bekleyen isteğiniz yok.", GidenEylemleri);

        _liste.ResumeLayout();
        Arayuz.KartlariGenislet(_liste);
    });

    private void Bolum(string baslik, List<EslesmeOzeti> kayitlar, string bosMetin, Func<EslesmeOzeti, Control[]> eylemler)
    {
        _liste.Controls.Add(Arayuz.BolumBasligi($"{baslik} ({kayitlar.Count})"));
        if (kayitlar.Count == 0)
        {
            var bos = Tema.Etiket(bosMetin, Tema.Kucuk, Tema.SolukMetin);
            bos.Margin = new Padding(0, 0, 0, 8);
            _liste.Controls.Add(bos);
            return;
        }

        foreach (var kayit in kayitlar)
        {
            var satirlar = new List<string> { $"{kayit.Bolum}  ·  {kayit.Sehir}" };
            if (kayit.Eposta is not null)
                satirlar.Add($"İletişim: {kayit.Eposta}");
            _liste.Controls.Add(Arayuz.KisiKarti(kayit.AdSoyad, satirlar, kayit.UyumYuzdesi, eylemler(kayit)));
        }
    }

    private Control[] GelenEylemleri(EslesmeOzeti kayit)
    {
        var kabul = Tema.BirincilDugme("Kabul et");
        kabul.AccessibleName = $"Kabul et: {kayit.AdSoyad}";
        kabul.Click += (_, _) => Islem(() => _eslesme.Yanitla(kayit.EslesmeId, kabul: true));

        var red = Tema.IkincilDugme("Reddet");
        red.AccessibleName = $"Reddet: {kayit.AdSoyad}";
        red.Click += (_, _) =>
        {
            var onay = MessageBox.Show(this, $"{kayit.AdSoyad} kişisinin isteği reddedilsin mi? İkiniz de birbirinizi Arkadaş bul listesinde artık görmeyeceksiniz.",
                "İsteği reddet", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (onay == DialogResult.Yes)
                Islem(() => _eslesme.Yanitla(kayit.EslesmeId, kabul: false));
        };
        return [kabul, red];
    }

    private Control[] GidenEylemleri(EslesmeOzeti kayit)
    {
        if (kayit.Durum == EslesmeDurumu.Red)
            return [Arayuz.DurumEtiketi("Kabul edilmedi", Tema.SolukMetin)];

        var geriCek = Tema.IkincilDugme("Geri çek");
        geriCek.AccessibleName = $"Geri çek: {kayit.AdSoyad}";
        geriCek.Click += (_, _) => Islem(() => _eslesme.GeriCek(kayit.EslesmeId));
        return [Arayuz.DurumEtiketi("Yanıt bekleniyor", Tema.SolukMetin), geriCek];
    }

    // Liste, tiklanan dugmenin kendi olayi bittikten sonra yeniden kurulur.
    private void Islem(Action islem)
    {
        if (Arayuz.Dene(islem))
            BeginInvoke(Yenile);
    }
}
