using UyumEslestirme.Domain;
using Timer = System.Windows.Forms.Timer;

namespace UyumEslestirme.Desktop;

public sealed class AnaForm : Form
{
    private const int MenuGenisligi = 230;

    private readonly Timer _oturumDenetimi = new() { Interval = 30_000 };
    private readonly Panel _icerik = new() { Dock = DockStyle.Fill, BackColor = Tema.Zemin, Padding = new Padding(28, 24, 28, 24) };
    private readonly FlowLayoutPanel _menu = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
    private readonly List<Button> _menuDugmeleri = [];
    private readonly List<Action> _sayfaAcicilari = [];

    public bool CikisIstendi { get; private set; }

    public AnaForm(Servisler servisler)
    {
        Text = "Ev/Yurt Arkadaşı Uyum Eşleştirme";
        Font = Tema.Govde;
        ForeColor = Tema.Metin;
        ClientSize = new Size(1080, 700);
        MinimumSize = new Size(1000, 660);
        StartPosition = FormStartPosition.CenterScreen;

        var oturum = servisler.Oturum;
        var cikis = Tema.IkincilDugme("Çıkış yap");

        var kenar = new Panel { Dock = DockStyle.Left, Width = MenuGenisligi, BackColor = Tema.Koyu };
        kenar.Controls.Add(_menu);
        kenar.Controls.Add(new Label
        {
            Text = "Uyum Eşleştirme",
            Font = Tema.AltBaslik,
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 68,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
        });
        kenar.Controls.Add(KullaniciAlani(oturum.AdSoyad, oturum.Rol, cikis));

        Controls.Add(_icerik);
        Controls.Add(kenar);

        var profil = new ProfilSekmesi(servisler.Profil, servisler.Anket);
        SayfaEkle("Profil", profil, profil.Yenile);
        SayfaEkle("Tercih anketi", new AnketSekmesi(servisler.Anket));

        if (oturum.Rol == Rol.Ogrenci)
        {
            var adaylar = new AdaylarSekmesi(servisler.Adaylar, servisler.Anket);
            SayfaEkle("Arkadaş bul", adaylar, adaylar.Yenile);
        }

        // Menude gizlemek yalnizca kolayliktir; asil yetki kontrolu YonetimServisi icindedir.
        if (oturum.Rol == Rol.Yonetici)
            SayfaEkle("Yönetim", new YonetimSekmesi(servisler.Yonetim));

        _sayfaAcicilari[0]();

        cikis.Click += (_, _) =>
        {
            CikisIstendi = true;
            Close();
        };

        _oturumDenetimi.Tick += (_, _) =>
        {
            if (oturum.Acik)
                return;
            _oturumDenetimi.Stop();
            MessageBox.Show(this, "Oturum süreniz doldu. Lütfen tekrar giriş yapın.", "Oturum");
            CikisIstendi = true;
            Close();
        };
        _oturumDenetimi.Start();
    }

    private static Panel KullaniciAlani(string adSoyad, Rol rol, Button cikis)
    {
        var alan = new Panel { Dock = DockStyle.Bottom, Height = 124, BackColor = Tema.KoyuAcik, Padding = new Padding(20, 14, 20, 14) };
        cikis.Dock = DockStyle.Bottom;
        alan.Controls.Add(cikis);
        alan.Controls.Add(new Label { Text = Arayuz.RolAdi(rol), Font = Tema.Kucuk, ForeColor = Tema.BirincilSoluk, Dock = DockStyle.Top, Height = 22 });
        alan.Controls.Add(new Label { Text = adSoyad, Font = Tema.KucukKalin, ForeColor = Color.White, Dock = DockStyle.Top, Height = 24, AutoEllipsis = true });
        return alan;
    }

    private void SayfaEkle(string ad, Control sayfa, Action? acilinca = null)
    {
        sayfa.Dock = DockStyle.Fill;
        sayfa.Visible = false;
        _icerik.Controls.Add(sayfa);

        var dugme = new Button
        {
            Text = ad,
            Width = MenuGenisligi,
            Height = 46,
            Margin = new Padding(0),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0),
            ForeColor = Color.White,
            Font = Tema.Govde,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        dugme.FlatAppearance.BorderSize = 0;
        dugme.FlatAppearance.MouseOverBackColor = Tema.KoyuAcik;
        void Ac()
        {
            foreach (Control diger in _icerik.Controls)
                diger.Visible = diger == sayfa;
            foreach (var m in _menuDugmeleri)
                m.BackColor = m == dugme ? Tema.Birincil : Tema.Koyu;
            acilinca?.Invoke();
        }
        dugme.Click += (_, _) => Ac();
        _sayfaAcicilari.Add(Ac);
        _menuDugmeleri.Add(dugme);
        _menu.Controls.Add(dugme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _oturumDenetimi.Dispose();
        base.Dispose(disposing);
    }
}
