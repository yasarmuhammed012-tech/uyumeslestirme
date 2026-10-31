using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class AdaylarSekmesi : UserControl
{
    private const string TumSehirler = "Tüm şehirler";

    private readonly AdayServisi _adaylar;
    private readonly AnketServisi _anket;
    private readonly ComboBox _sehir = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 220 };
    private readonly Label _bilgi = Tema.Etiket("", Tema.Govde, Tema.SolukMetin);
    private readonly FlowLayoutPanel _liste = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
    };
    private List<AdayOzeti> _tumu = [];

    public AdaylarSekmesi(AdayServisi adaylar, AnketServisi anket)
    {
        _adaylar = adaylar;
        _anket = anket;

        _bilgi.Dock = DockStyle.Right;
        _bilgi.Padding = new Padding(0, 6, 0, 0);
        var ust = new Panel { Dock = DockStyle.Top, Height = 44 };
        ust.Controls.Add(_sehir);
        ust.Controls.Add(_bilgi);

        Controls.Add(_liste);
        Controls.Add(ust);
        Controls.Add(Tema.SayfaBasligi("Arkadaş bul", "Diğer öğrencilerin profilleri, size uyumlarına göre sıralanır."));

        _sehir.SelectedIndexChanged += (_, _) => Listele();
        _liste.Resize += (_, _) => KartlariGenislet();
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
        // SelectedItem degisince Listele calisir; ayni kalsa da liste yenilensin diye asagida da cagrilir.
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

        Temizle();
        _bilgi.Text = $"{sonuc.Count} kişi";
        if (sonuc.Count == 0)
        {
            Mesaj("Bu seçime uyan, profilini ve anketini tamamlamış kimse henüz yok.");
            return;
        }
        foreach (var aday in sonuc)
            _liste.Controls.Add(AdayKarti(aday));
        KartlariGenislet();
    }

    private void Mesaj(string metin)
    {
        Temizle();
        _bilgi.Text = "";
        var etiket = Tema.Etiket(metin, Tema.Govde, Tema.SolukMetin);
        etiket.Margin = new Padding(0, 16, 0, 0);
        _liste.Controls.Add(etiket);
    }

    private void Temizle()
    {
        while (_liste.Controls.Count > 0)
            _liste.Controls[0].Dispose();
    }

    private static KartPanel AdayKarti(AdayOzeti aday)
    {
        var bilgiler = Tema.Yigin();
        bilgiler.Controls.Add(Tema.Etiket(aday.AdSoyad, Tema.AltBaslik, Tema.Metin));
        bilgiler.Controls.Add(Tema.Etiket($"{aday.Bolum}  ·  {aday.Sehir}  ·  {aday.Yas} yaş", Tema.Kucuk, Tema.SolukMetin));
        if (aday.Hakkinda is not null)
            bilgiler.Controls.Add(Tema.Etiket(aday.Hakkinda, Tema.Kucuk, Tema.Metin, 520));
        bilgiler.Controls.Add(Tema.Etiket(
            $"En uyumlu: {Arayuz.KategoriAdi(aday.EnUyumluKategori)}   |   En farklı: {Arayuz.KategoriAdi(aday.EnFarkliKategori)}",
            Tema.Kucuk, Tema.SolukMetin));

        var uyum = Tema.Yigin();
        var yuzde = Tema.Etiket($"%{aday.UyumYuzdesi:0} uyum", Tema.AltBaslik, Tema.Birincil);
        uyum.Controls.Add(yuzde);
        uyum.Controls.Add(new Cubuk(aday.UyumYuzdesi / 100) { Dock = DockStyle.Fill, Margin = new Padding(0) });

        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tablo.Controls.Add(bilgiler);
        tablo.Controls.Add(uyum);

        var kart = new KartPanel
        {
            Height = aday.Hakkinda is null ? 120 : 160,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 14, 18, 10),
        };
        kart.Controls.Add(tablo);
        return kart;
    }

    private void KartlariGenislet()
    {
        var genislik = _liste.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
        foreach (Control kart in _liste.Controls)
        {
            if (kart is KartPanel)
                kart.Width = Math.Max(genislik, 500);
        }
    }
}
