using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class AnketSekmesi : UserControl
{
    private readonly AnketServisi _anket;
    private readonly FlowLayoutPanel _liste;
    private readonly List<(int SoruId, NumericUpDown Deger, NumericUpDown Agirlik)> _satirlar = [];

    public AnketSekmesi(AnketServisi anket)
    {
        _anket = anket;
        Dock = DockStyle.Fill;

        var aciklama = new Label
        {
            Text = "Cevap: 1 = hiç katılmıyorum, 5 = tamamen katılıyorum.  Önem: 1 = benim için önemsiz, 5 = çok önemli.",
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8),
        };
        _liste = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8),
        };
        var kaydet = new Button { Text = "Anketi kaydet", Dock = DockStyle.Bottom, Height = 34 };
        kaydet.Click += (_, _) => Kaydet();

        Controls.Add(_liste);
        Controls.Add(kaydet);
        Controls.Add(aciklama);

        Arayuz.Dene(Yukle);
    }

    private void Yukle()
    {
        var cevaplar = _anket.BenimCevaplarim().ToDictionary(c => c.SoruId);
        foreach (var soru in _anket.Sorular())
        {
            cevaplar.TryGetValue(soru.Id, out var cevap);
            var deger = OlcekKutusu(cevap?.Deger);
            var agirlik = OlcekKutusu(cevap?.Agirlik);
            _satirlar.Add((soru.Id, deger, agirlik));

            var satir = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            satir.Controls.Add(new Label { Text = soru.Metin, Width = 400, Margin = new Padding(3, 6, 3, 3) });
            satir.Controls.Add(new Label { Text = "Cevap", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
            satir.Controls.Add(deger);
            satir.Controls.Add(new Label { Text = "Önem", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
            satir.Controls.Add(agirlik);
            _liste.Controls.Add(satir);
        }
    }

    private void Kaydet()
    {
        var girdiler = _satirlar
            .Select(s => new CevapGirdisi(s.SoruId, (int)s.Deger.Value, (int)s.Agirlik.Value))
            .ToList();
        if (Arayuz.Dene(() => _anket.CevaplariKaydet(girdiler)))
            MessageBox.Show(this, "Anket kaydedildi.", "Anket");
    }

    private static NumericUpDown OlcekKutusu(int? deger) => new()
    {
        Minimum = AnketServisi.AsgariOlcek,
        Maximum = AnketServisi.AzamiOlcek,
        Value = deger ?? 3,
        Width = 50,
    };
}
