using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class ProfilSekmesi : UserControl
{
    private readonly ProfilServisi _profil;
    private readonly AnketServisi _anket;
    private readonly TextBox _bolum = new();
    private readonly TextBox _sehir = new();
    private readonly NumericUpDown _dogumYili = new() { Minimum = 1900, Maximum = 2100, Value = 2005 };
    private readonly TextBox _hakkinda = new() { Multiline = true, Height = 90, ScrollBars = ScrollBars.Vertical };
    private readonly Label _kayitDurumu = Tema.Etiket("", Tema.Kucuk, Tema.Basari);
    private readonly Label _anketDurumu = Tema.Etiket("", Tema.KucukKalin, Tema.Basari);
    private readonly TableLayoutPanel _ozet = Tema.Yigin();

    public ProfilSekmesi(ProfilServisi profil, AnketServisi anket)
    {
        _profil = profil;
        _anket = anket;

        var duzen = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        duzen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        duzen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        duzen.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        duzen.Controls.Add(ProfilKarti());
        duzen.Controls.Add(OzetKarti());

        Controls.Add(duzen);
        Controls.Add(Tema.SayfaBasligi("Profilim", "Bilgileriniz ve tercihlerinizin özeti."));
        Yenile();
    }

    private KartPanel ProfilKarti()
    {
        var form = Tema.Yigin();
        form.Controls.Add(Tema.Etiket("Kişisel bilgiler", Tema.AltBaslik, Tema.Metin));
        Tema.Alan(form, "Bölüm", _bolum);
        Tema.Alan(form, "Şehir", _sehir);
        Tema.Alan(form, "Doğum yılı", _dogumYili);
        Tema.Alan(form, "Hakkında (isteğe bağlı)", _hakkinda);

        var kaydet = Tema.BirincilDugme("Profili kaydet");
        kaydet.Margin = new Padding(0, 12, 0, 8);
        kaydet.Width = 150;
        kaydet.Click += (_, _) => Kaydet();
        form.Controls.Add(kaydet);
        form.Controls.Add(_kayitDurumu);

        var kart = new KartPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), AutoScroll = true };
        kart.Controls.Add(form);
        return kart;
    }

    private KartPanel OzetKarti()
    {
        var yigin = Tema.Yigin();
        yigin.Controls.Add(Tema.Etiket("Tercih özeti", Tema.AltBaslik, Tema.Metin));
        yigin.Controls.Add(_anketDurumu);
        _ozet.Margin = new Padding(0, 8, 0, 0);
        yigin.Controls.Add(_ozet);

        var kart = new KartPanel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), AutoScroll = true };
        kart.Controls.Add(yigin);
        return kart;
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

        var tamam = _anket.TamamlandiMi();
        _anketDurumu.Text = tamam ? "✓ Anket tamamlandı" : "Anket eksik: Tercih anketi sayfasından doldurun.";
        _anketDurumu.ForeColor = tamam ? Tema.Basari : Tema.Uyari;
        OzetiCiz();
    });

    private void OzetiCiz()
    {
        _ozet.SuspendLayout();
        while (_ozet.Controls.Count > 0)
            _ozet.Controls[0].Dispose();

        foreach (var kategori in _anket.TercihOzeti())
        {
            var etiket = $"{Arayuz.KategoriAdi(kategori.Kategori)}:  {kategori.Ortalama:0.0} / 5";
            var baslik = Tema.Etiket(etiket, Tema.KucukKalin, Tema.Metin);
            baslik.Margin = new Padding(0, 10, 0, 4);
            _ozet.Controls.Add(baslik);
            _ozet.Controls.Add(new Cubuk((kategori.Ortalama - AnketServisi.AsgariOlcek) / (AnketServisi.AzamiOlcek - AnketServisi.AsgariOlcek))
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 3),
            });
            _ozet.Controls.Add(Tema.Etiket($"Sizin için önem payı: %{kategori.AgirlikYuzdesi:0}", Tema.Kucuk, Tema.SolukMetin));
        }
        _ozet.ResumeLayout();
    }

    private void Kaydet()
    {
        var girdi = new ProfilGirdisi(_bolum.Text, _sehir.Text, (int)_dogumYili.Value, _hakkinda.Text);
        var basarili = Arayuz.Dene(() => _profil.Kaydet(girdi));
        _kayitDurumu.Text = basarili ? "✓ Profil kaydedildi." : "";
    }
}
