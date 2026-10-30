namespace UyumEslestirme.Desktop;

public sealed class AnaForm : Form
{
    public AnaForm()
    {
        Text = "Ev/Yurt Arkadaşı Uyum Eşleştirme";
        ClientSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label
        {
            Text = "Proje iskeleti çalışıyor.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        });
    }
}
