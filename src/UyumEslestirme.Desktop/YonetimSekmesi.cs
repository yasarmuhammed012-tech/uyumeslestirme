using System.Globalization;
using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class YonetimSekmesi : UserControl
{
    private static readonly CompareInfo Turkce = new CultureInfo("tr-TR").CompareInfo;

    private readonly DataGridView _tablo = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        GridColor = Tema.Kenar,
        EnableHeadersVisualStyles = false,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 40,
    };
    private readonly TextBox _arama = new() { PlaceholderText = "Ad veya e-posta ile ara", Dock = DockStyle.Left, Width = 320 };
    private readonly Label _sayi = Tema.Etiket("", Tema.Kucuk, Tema.SolukMetin);
    private List<KullaniciOzeti> _kullanicilar = [];

    public YonetimSekmesi(YonetimServisi yonetim)
    {
        _tablo.RowTemplate.Height = 36;
        _tablo.DefaultCellStyle.SelectionBackColor = Tema.BirincilSoluk;
        _tablo.DefaultCellStyle.SelectionForeColor = Tema.Metin;
        _tablo.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        _tablo.ColumnHeadersDefaultCellStyle.BackColor = Tema.Zemin;
        _tablo.ColumnHeadersDefaultCellStyle.ForeColor = Tema.SolukMetin;
        _tablo.ColumnHeadersDefaultCellStyle.SelectionBackColor = Tema.Zemin;
        _tablo.ColumnHeadersDefaultCellStyle.Font = Tema.KucukKalin;
        _tablo.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

        _tablo.Columns.Add("no", "No");
        _tablo.Columns.Add("ad", "Ad soyad");
        _tablo.Columns.Add("eposta", "E-posta");
        _tablo.Columns.Add("rol", "Rol");
        _tablo.Columns.Add("tarih", "Kayıt tarihi");
        _tablo.Columns["no"]!.FillWeight = 30;
        _tablo.Columns["rol"]!.FillWeight = 50;

        _sayi.Dock = DockStyle.Right;
        _sayi.Padding = new Padding(0, 6, 0, 0);
        var ust = new Panel { Dock = DockStyle.Top, Height = 44 };
        ust.Controls.Add(_arama);
        ust.Controls.Add(_sayi);

        var kart = new KartPanel { Dock = DockStyle.Fill };
        kart.Controls.Add(_tablo);
        kart.Controls.Add(ust);

        Controls.Add(kart);
        Controls.Add(Tema.SayfaBasligi("Yönetim", "Sistemde kayıtlı kullanıcılar."));

        _arama.TextChanged += (_, _) => Listele();
        Arayuz.Dene(() =>
        {
            _kullanicilar = yonetim.KullanicilariListele();
            Listele();
        });
    }

    private void Listele()
    {
        var aranan = _arama.Text.Trim();
        var sonuc = _kullanicilar.Where(k => aranan.Length == 0 || IceriyorMu(k.AdSoyad, aranan) || IceriyorMu(k.Eposta, aranan)).ToList();

        _tablo.Rows.Clear();
        foreach (var k in sonuc)
        {
            // Tarihler veritabaninda UTC tutulur, ekranda yerel saat gosterilir.
            var yerel = DateTime.SpecifyKind(k.OlusturmaTarihi, DateTimeKind.Utc).ToLocalTime();
            _tablo.Rows.Add(k.Id, k.AdSoyad, k.Eposta, Arayuz.RolAdi(k.Rol), yerel.ToString("dd.MM.yyyy HH:mm"));
        }
        _sayi.Text = $"{sonuc.Count} kullanıcı";
    }

    private static bool IceriyorMu(string metin, string aranan) =>
        Turkce.IndexOf(metin, aranan, CompareOptions.IgnoreCase) >= 0;
}
