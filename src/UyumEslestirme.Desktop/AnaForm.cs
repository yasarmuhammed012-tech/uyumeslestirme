using UyumEslestirme.Domain;
using Timer = System.Windows.Forms.Timer;

namespace UyumEslestirme.Desktop;

public sealed class AnaForm : Form
{
    private readonly Timer _oturumDenetimi = new() { Interval = 30_000 };

    public bool CikisIstendi { get; private set; }

    public AnaForm(Servisler servisler)
    {
        Text = "Ev/Yurt Arkadaşı Uyum Eşleştirme";
        ClientSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        var ust = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
        var cikis = new Button { Text = "Çıkış", AutoSize = true };
        ust.Controls.Add(cikis);
        ust.Controls.Add(new Label
        {
            Text = $"{servisler.Oturum.AdSoyad} ({servisler.Oturum.Rol})",
            AutoSize = true,
            Margin = new Padding(3, 8, 12, 3),
        });

        var sekmeler = new TabControl { Dock = DockStyle.Fill };
        sekmeler.TabPages.Add(new TabPage("Ana sayfa")
        {
            Controls = { new Label { Text = $"Hoş geldiniz, {servisler.Oturum.AdSoyad}.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter } },
        });

        // Arayuzde gizlemek yalnizca kolayliktir; asil yetki kontrolu YonetimServisi icindedir.
        if (servisler.Oturum.Rol == Rol.Yonetici)
            sekmeler.TabPages.Add(new TabPage("Yönetim") { Controls = { new YonetimSekmesi(servisler.Yonetim) } });

        Controls.Add(sekmeler);
        Controls.Add(ust);

        cikis.Click += (_, _) =>
        {
            CikisIstendi = true;
            Close();
        };

        _oturumDenetimi.Tick += (_, _) =>
        {
            if (servisler.Oturum.Acik)
                return;
            _oturumDenetimi.Stop();
            MessageBox.Show(this, "Oturum süreniz doldu. Lütfen tekrar giriş yapın.", "Oturum");
            CikisIstendi = true;
            Close();
        };
        _oturumDenetimi.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _oturumDenetimi.Dispose();
        base.Dispose(disposing);
    }
}
