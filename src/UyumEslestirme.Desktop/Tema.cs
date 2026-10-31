namespace UyumEslestirme.Desktop;

// Tum ekranlarin renk, yazi tipi ve ortak denetim tanimlari.
internal static class Tema
{
    public static readonly Color Koyu = Color.FromArgb(30, 27, 75);
    public static readonly Color KoyuAcik = Color.FromArgb(49, 46, 104);
    public static readonly Color Birincil = Color.FromArgb(79, 70, 229);
    public static readonly Color BirincilKoyu = Color.FromArgb(67, 56, 202);
    public static readonly Color BirincilSoluk = Color.FromArgb(224, 231, 255);
    public static readonly Color Zemin = Color.FromArgb(245, 246, 250);
    public static readonly Color Metin = Color.FromArgb(31, 41, 55);
    public static readonly Color SolukMetin = Color.FromArgb(107, 114, 128);
    public static readonly Color Kenar = Color.FromArgb(229, 231, 235);
    public static readonly Color Basari = Color.FromArgb(21, 128, 61);
    public static readonly Color Uyari = Color.FromArgb(180, 83, 9);

    public static readonly Font Govde = new("Segoe UI", 10F);
    public static readonly Font Kucuk = new("Segoe UI", 9F);
    public static readonly Font KucukKalin = new("Segoe UI Semibold", 9F);
    public static readonly Font AltBaslik = new("Segoe UI Semibold", 12F);
    public static readonly Font Baslik = new("Segoe UI Semibold", 18F);

    public static Button BirincilDugme(string metin)
    {
        var dugme = TemelDugme(metin);
        dugme.BackColor = Birincil;
        dugme.ForeColor = Color.White;
        dugme.FlatAppearance.BorderSize = 0;
        dugme.FlatAppearance.MouseOverBackColor = BirincilKoyu;
        return dugme;
    }

    public static Button IkincilDugme(string metin)
    {
        var dugme = TemelDugme(metin);
        dugme.BackColor = Color.White;
        dugme.ForeColor = Metin;
        dugme.FlatAppearance.BorderColor = Kenar;
        dugme.FlatAppearance.MouseOverBackColor = Zemin;
        return dugme;
    }

    private static Button TemelDugme(string metin) => new()
    {
        Text = metin,
        FlatStyle = FlatStyle.Flat,
        Font = KucukKalin,
        Height = 38,
        MinimumSize = new Size(110, 38),
        Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false,
    };

    public static Label Etiket(string metin, Font yazi, Color renk, int azamiGenislik = 0) => new()
    {
        Text = metin,
        Font = yazi,
        ForeColor = renk,
        AutoSize = true,
        MaximumSize = new Size(azamiGenislik, 0),
        Margin = new Padding(0, 0, 0, 6),
    };

    // Denetimleri alt alta dizen, yuksekligi icerige gore ayarlanan duzen.
    public static TableLayoutPanel Yigin()
    {
        var yigin = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
        };
        yigin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return yigin;
    }

    public static T Alan<T>(TableLayoutPanel yigin, string etiket, T alan) where T : Control
    {
        yigin.Controls.Add(new Label
        {
            Text = etiket,
            Font = KucukKalin,
            ForeColor = SolukMetin,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 3),
        });
        alan.Dock = DockStyle.Fill;
        alan.Margin = new Padding(0, 0, 0, 4);
        yigin.Controls.Add(alan);
        return alan;
    }

    public static Control SayfaBasligi(string baslik, string aciklama)
    {
        var yigin = Yigin();
        yigin.Padding = new Padding(0, 0, 0, 12);
        yigin.Controls.Add(Etiket(baslik, Baslik, Metin));
        yigin.Controls.Add(Etiket(aciklama, Govde, SolukMetin));
        return yigin;
    }
}

internal sealed class KartPanel : Panel
{
    public KartPanel()
    {
        BackColor = Color.White;
        Padding = new Padding(20);
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var kalem = new Pen(Tema.Kenar);
        e.Graphics.DrawRectangle(kalem, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class Cubuk : Control
{
    private readonly double _oran;

    public Cubuk(double oran)
    {
        _oran = Math.Clamp(oran, 0, 1);
        Height = 10;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Tema.BirincilSoluk);
        using var firca = new SolidBrush(Tema.Birincil);
        e.Graphics.FillRectangle(firca, 0, 0, (int)(Width * _oran), Height);
    }
}

// Anket icin 1-5 arasi tek secimli dugme grubu. Secili = 0: henuz secilmedi.
internal sealed class OlcekSecici : FlowLayoutPanel
{
    private readonly List<Button> _dugmeler = [];

    public int Secili { get; private set; }

    public OlcekSecici(string ad, int? deger)
    {
        WrapContents = false;
        Width = 180;
        Height = 34;
        Margin = new Padding(0, 6, 0, 0);

        for (var i = 1; i <= 5; i++)
        {
            var sayi = i;
            var dugme = new Button
            {
                Text = sayi.ToString(),
                AccessibleName = $"{ad} {sayi}",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Margin = new Padding(0, 0, 2, 0),
                Font = Tema.Kucuk,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            dugme.FlatAppearance.BorderColor = Tema.Kenar;
            dugme.Click += (_, _) => Sec(sayi);
            _dugmeler.Add(dugme);
            Controls.Add(dugme);
        }
        Sec(deger ?? 0);
    }

    private void Sec(int deger)
    {
        Secili = deger;
        for (var i = 0; i < _dugmeler.Count; i++)
        {
            var secili = i + 1 == deger;
            _dugmeler[i].BackColor = secili ? Tema.Birincil : Color.White;
            _dugmeler[i].ForeColor = secili ? Color.White : Tema.Metin;
        }
    }
}
