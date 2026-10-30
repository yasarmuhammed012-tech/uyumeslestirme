using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

internal static class Arayuz
{
    // Is katmaninin kullaniciya yonelik hatalarini tek yerde mesaja cevirir.
    public static bool Dene(Action islem)
    {
        try
        {
            islem();
            return true;
        }
        catch (IsKuraliHatasi hata)
        {
            MessageBox.Show(hata.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (YetkisizErisimHatasi hata)
        {
            MessageBox.Show(hata.Message, "Yetkisiz işlem", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return false;
    }

    public static TableLayoutPanel FormDuzeni(int satirSayisi)
    {
        var duzen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = satirSayisi,
            Padding = new Padding(12),
        };
        duzen.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        duzen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return duzen;
    }

    public static T AlanEkle<T>(TableLayoutPanel duzen, string etiket, T alan) where T : Control
    {
        duzen.Controls.Add(new Label { Text = etiket, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
        alan.Dock = DockStyle.Fill;
        duzen.Controls.Add(alan);
        return alan;
    }
}
