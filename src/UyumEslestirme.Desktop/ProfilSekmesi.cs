using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Desktop;

public sealed class ProfilSekmesi : UserControl
{
    private readonly ProfilServisi _profil;
    private readonly AnketServisi _anket;
    private readonly TextBox _bolum = new();
    private readonly TextBox _sehir = new();
    private readonly NumericUpDown _dogumYili = new() { Minimum = 1900, Maximum = 2100, Value = 2005 };
    private readonly TextBox _hakkinda = new() { Multiline = true, Height = 70 };
    private readonly Label _durum = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
    private readonly DataGridView _ozet = new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        Height = 170,
    };

    public ProfilSekmesi(ProfilServisi profil, AnketServisi anket)
    {
        _profil = profil;
        _anket = anket;
        Dock = DockStyle.Fill;

        var duzen = Arayuz.FormDuzeni(7);
        duzen.AutoScroll = true;
        Arayuz.AlanEkle(duzen, "Bölüm", _bolum);
        Arayuz.AlanEkle(duzen, "Şehir", _sehir);
        Arayuz.AlanEkle(duzen, "Doğum yılı", _dogumYili);
        Arayuz.AlanEkle(duzen, "Hakkında", _hakkinda);

        var kaydet = new Button { Text = "Profili kaydet", AutoSize = true };
        kaydet.Click += (_, _) => Kaydet();
        duzen.Controls.Add(new Label());
        duzen.Controls.Add(kaydet);

        duzen.Controls.Add(new Label { Text = "Anket durumu", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        duzen.Controls.Add(_durum);
        Arayuz.AlanEkle(duzen, "Tercih özeti", _ozet);

        Controls.Add(duzen);
        Yenile();
    }

    public void Yenile() => Arayuz.Dene(() =>
    {
        var profil = _profil.BenimProfilim();
        if (profil is not null)
        {
            _bolum.Text = profil.Bolum;
            _sehir.Text = profil.Sehir;
            _dogumYili.Value = Math.Clamp(profil.DogumYili, (int)_dogumYili.Minimum, (int)_dogumYili.Maximum);
            _hakkinda.Text = profil.Hakkinda;
        }

        _durum.Text = _anket.TamamlandiMi() ? "Tamamlandı" : "Eksik: Anket sekmesinden doldurun";
        _ozet.DataSource = _anket.TercihOzeti()
            .Select(o => new
            {
                Kategori = KategoriAdi(o.Kategori),
                Ortalama = Math.Round(o.Ortalama, 2),
                ÖnemYüzdesi = Math.Round(o.AgirlikYuzdesi, 1),
            })
            .ToList();
    });

    private static string KategoriAdi(SoruKategorisi kategori) => kategori switch
    {
        SoruKategorisi.UykuDuzeni => "Uyku düzeni",
        SoruKategorisi.Temizlik => "Temizlik",
        SoruKategorisi.Sosyallik => "Sosyallik",
        SoruKategorisi.Gurultu => "Gürültü",
        SoruKategorisi.Ders => "Ders",
        _ => kategori.ToString(),
    };

    private void Kaydet()
    {
        var girdi = new ProfilGirdisi(_bolum.Text, _sehir.Text, (int)_dogumYili.Value, _hakkinda.Text);
        if (Arayuz.Dene(() => _profil.Kaydet(girdi)))
            MessageBox.Show(this, "Profil kaydedildi.", "Profil");
    }
}
