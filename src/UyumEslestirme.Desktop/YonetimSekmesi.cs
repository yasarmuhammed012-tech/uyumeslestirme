using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class YonetimSekmesi : UserControl
{
    public YonetimSekmesi(YonetimServisi yonetim)
    {
        Dock = DockStyle.Fill;

        var tablo = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        Controls.Add(tablo);

        // Tarihler veritabaninda UTC tutulur, ekranda yerel saat gosterilir.
        Arayuz.Dene(() => tablo.DataSource = yonetim.KullanicilariListele()
            .Select(k => k with { OlusturmaTarihi = DateTime.SpecifyKind(k.OlusturmaTarihi, DateTimeKind.Utc).ToLocalTime() })
            .ToList());
    }
}
