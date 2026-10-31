using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

public sealed class AnketSekmesi : UserControl
{
    private const int BaslikSatiri = 40;
    private const int SoruSatiri = 48;

    private readonly AnketServisi _anket;
    private readonly FlowLayoutPanel _liste = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
    };
    private readonly Label _durum = Tema.Etiket("", Tema.Kucuk, Tema.Basari);
    private readonly List<(int SoruId, OlcekSecici Deger, OlcekSecici Agirlik)> _satirlar = [];

    public AnketSekmesi(AnketServisi anket)
    {
        _anket = anket;

        var kaydet = Tema.BirincilDugme("Anketi kaydet");
        kaydet.Width = 150;
        kaydet.Dock = DockStyle.Right;
        kaydet.Click += (_, _) => Kaydet();

        _durum.Margin = new Padding(0);
        _durum.Dock = DockStyle.Left;
        _durum.AutoSize = true;
        _durum.Padding = new Padding(0, 10, 0, 0);

        var alt = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(0, 12, 0, 0) };
        alt.Controls.Add(_durum);
        alt.Controls.Add(kaydet);

        Controls.Add(_liste);
        Controls.Add(alt);
        Controls.Add(Tema.SayfaBasligi(
            "Tercih anketi",
            "Cevap: 1 = hiç katılmıyorum, 5 = tamamen katılıyorum.   Önem: 1 = benim için önemsiz, 5 = çok önemli."));

        _liste.Resize += (_, _) => KartlariGenislet();
        Arayuz.Dene(Yukle);
    }

    private void Yukle()
    {
        var cevaplar = _anket.BenimCevaplarim().ToDictionary(c => c.SoruId);
        foreach (var grup in _anket.Sorular().GroupBy(s => s.Kategori))
            _liste.Controls.Add(KategoriKarti(grup.Key, grup.ToList(), cevaplar));
        KartlariGenislet();
    }

    private KartPanel KategoriKarti(SoruKategorisi kategori, List<AnketSorusu> sorular, Dictionary<int, AnketCevabi> cevaplar)
    {
        var tablo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = sorular.Count + 1 };
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        tablo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, BaslikSatiri));

        tablo.Controls.Add(Tema.Etiket(Arayuz.KategoriAdi(kategori), Tema.AltBaslik, Tema.Metin));
        tablo.Controls.Add(Tema.Etiket("Cevap", Tema.KucukKalin, Tema.SolukMetin));
        tablo.Controls.Add(Tema.Etiket("Önem", Tema.KucukKalin, Tema.SolukMetin));

        foreach (var soru in sorular)
        {
            tablo.RowStyles.Add(new RowStyle(SizeType.Absolute, SoruSatiri));
            cevaplar.TryGetValue(soru.Id, out var cevap);
            var deger = new OlcekSecici($"Soru {soru.Sira} cevap", cevap?.Deger);
            var agirlik = new OlcekSecici($"Soru {soru.Sira} önem", cevap?.Agirlik);
            _satirlar.Add((soru.Id, deger, agirlik));

            tablo.Controls.Add(new Label { Text = soru.Metin, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 12, 0) });
            tablo.Controls.Add(deger);
            tablo.Controls.Add(agirlik);
        }

        var kart = new KartPanel
        {
            Height = BaslikSatiri + sorular.Count * SoruSatiri + 40,
            Margin = new Padding(0, 0, 0, 12),
        };
        kart.Controls.Add(tablo);
        return kart;
    }

    private void KartlariGenislet()
    {
        var genislik = _liste.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;
        foreach (Control kart in _liste.Controls)
            kart.Width = Math.Max(genislik, 500);
    }

    private void Kaydet()
    {
        if (_satirlar.Any(s => s.Deger.Secili == 0 || s.Agirlik.Secili == 0))
        {
            _durum.Text = "";
            MessageBox.Show(this, "Lütfen tüm sorular için cevap ve önem seçin.", "Anket eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var girdiler = _satirlar
            .Select(s => new CevapGirdisi(s.SoruId, s.Deger.Secili, s.Agirlik.Secili))
            .ToList();
        var basarili = Arayuz.Dene(() => _anket.CevaplariKaydet(girdiler));
        _durum.Text = basarili ? "✓ Anket kaydedildi." : "";
    }
}
