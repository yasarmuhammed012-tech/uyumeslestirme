using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class AdaylarSekmesi : UserControl
{
    private const string TumSehirler = "Tüm şehirler";

    private readonly AdayServisi _adaylar;
    private readonly AnketServisi _anket;
    private readonly EslesmeServisi _eslesme;
    private readonly ComboBox _sehir = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 220 };
    private readonly Label _bilgi = Tema.Etiket("", Tema.Govde, Tema.SolukMetin);
    private readonly FlowLayoutPanel _liste = Arayuz.KartListesi();
    private List<AdayOzeti> _tumu = [];

    public AdaylarSekmesi(AdayServisi adaylar, AnketServisi anket, EslesmeServisi eslesme)
    {
        _adaylar = adaylar;
        _anket = anket;
        _eslesme = eslesme;

        _bilgi.Dock = DockStyle.Right;
        _bilgi.Padding = new Padding(0, 6, 0, 0);
        var ust = new Panel { Dock = DockStyle.Top, Height = 44 };
        ust.Controls.Add(_sehir);
        ust.Controls.Add(_bilgi);

        Controls.Add(_liste);
        Controls.Add(ust);
        Controls.Add(Tema.SayfaBasligi("Arkadaş bul", "Diğer öğrencilerin profilleri, size uyumlarına göre sıralanır."));

        _sehir.SelectedIndexChanged += (_, _) => Listele();
        _liste.Resize += (_, _) => Arayuz.KartlariGenislet(_liste);
    }

    public void Yenile() => Arayuz.Dene(() =>
    {
        _tumu = [];
        var tamam = _anket.TamamlandiMi();
        if (tamam)
            _tumu = _adaylar.Adaylar();

        var secili = _sehir.SelectedItem as string;
        var sehirler = _tumu.Select(a => a.Sehir).Distinct().Order().ToList();
        _sehir.Items.Clear();
        _sehir.Items.Add(TumSehirler);
        foreach (var sehir in sehirler)
            _sehir.Items.Add(sehir);
        _sehir.Enabled = tamam;
        _sehir.SelectedItem = secili is not null && sehirler.Contains(secili) ? secili : TumSehirler;

        if (tamam)
            Listele();
        else
            Mesaj("Uyumlu kişileri görmek için önce Tercih anketi sayfasındaki anketi tamamlayın.");
    });

    private void Listele()
    {
        var sehir = _sehir.SelectedItem as string;
        var sonuc = _tumu.Where(a => sehir is null or TumSehirler || a.Sehir == sehir).ToList();

        Arayuz.ListeyiTemizle(_liste);
        _bilgi.Text = $"{sonuc.Count} kişi";
        if (sonuc.Count == 0)
        {
            Mesaj("Bu seçime uyan, profilini ve anketini tamamlamış kimse henüz yok.");
            return;
        }
        foreach (var aday in sonuc)
            _liste.Controls.Add(AdayKarti(aday));
        Arayuz.KartlariGenislet(_liste);
    }

    private KartPanel AdayKarti(AdayOzeti aday)
    {
        var satirlar = new List<string> { $"{aday.Bolum}  ·  {aday.Sehir}  ·  {aday.Yas} yaş" };
        if (aday.Hakkinda is not null)
            satirlar.Add(aday.Hakkinda);
        satirlar.Add($"En uyumlu: {Arayuz.KategoriAdi(aday.EnUyumluKategori)}   |   En farklı: {Arayuz.KategoriAdi(aday.EnFarkliKategori)}");

        return Arayuz.KisiKarti(aday.AdSoyad, satirlar, aday.UyumYuzdesi, Eylem(aday));
    }

    private Control Eylem(AdayOzeti aday)
    {
        switch (aday.Durum)
        {
            case AdayDurumu.Yok:
                var gonder = Tema.BirincilDugme("İstek gönder");
                gonder.AccessibleName = $"İstek gönder: {aday.AdSoyad}";
                // Liste, tiklanan dugmenin kendi olayi bittikten sonra yeniden kurulur.
                gonder.Click += (_, _) =>
                {
                    if (Arayuz.Dene(() => _eslesme.IstekGonder(aday.KullaniciId)))
                        BeginInvoke(Yenile);
                };
                return gonder;
            case AdayDurumu.IstekGonderildi:
                return Arayuz.DurumEtiketi("İstek gönderildi, yanıt bekleniyor", Tema.SolukMetin);
            case AdayDurumu.IstekGeldi:
                return Arayuz.DurumEtiketi("Size istek gönderdi: Eşleşmelerim'den yanıtlayın", Tema.Uyari);
            default:
                return Arayuz.DurumEtiketi("✓ Eşleştiniz", Tema.Basari);
        }
    }

    private void Mesaj(string metin)
    {
        Arayuz.ListeyiTemizle(_liste);
        _bilgi.Text = "";
        var etiket = Tema.Etiket(metin, Tema.Govde, Tema.SolukMetin);
        etiket.Margin = new Padding(0, 16, 0, 0);
        _liste.Controls.Add(etiket);
    }
}
