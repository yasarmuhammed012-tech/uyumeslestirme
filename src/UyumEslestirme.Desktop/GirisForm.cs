using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class GirisForm : Form
{
    public GirisForm(KimlikServisi kimlik)
    {
        Text = "Giriş";
        Font = Tema.Govde;
        BackColor = Color.White;
        ClientSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var form = Tema.Yigin();
        form.Controls.Add(Tema.Etiket("Hoş geldiniz", Tema.Baslik, Tema.Metin));
        form.Controls.Add(Tema.Etiket("Devam etmek için hesabınıza giriş yapın.", Tema.Govde, Tema.SolukMetin));
        var eposta = Tema.Alan(form, "E-posta", new TextBox { PlaceholderText = "ornek@ornek.test" });
        var parola = Tema.Alan(form, "Parola", new TextBox { UseSystemPasswordChar = true });

        var goster = new CheckBox { Text = "Parolayı göster", AutoSize = true, Font = Tema.Kucuk, ForeColor = Tema.SolukMetin, Margin = new Padding(0, 2, 0, 12) };
        goster.CheckedChanged += (_, _) => parola.UseSystemPasswordChar = !goster.Checked;
        form.Controls.Add(goster);

        var giris = Tema.BirincilDugme("Giriş yap");
        giris.Dock = DockStyle.Fill;
        giris.Margin = new Padding(0, 0, 0, 14);
        form.Controls.Add(giris);

        var kayit = new LinkLabel
        {
            Text = "Hesabınız yok mu? Kayıt olun",
            AutoSize = true,
            LinkColor = Tema.Birincil,
            ActiveLinkColor = Tema.BirincilKoyu,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Font = Tema.Kucuk,
            Margin = new Padding(0),
        };
        form.Controls.Add(kayit);

        var sag = new Panel { Dock = DockStyle.Fill, Padding = new Padding(52, 56, 52, 24) };
        sag.Controls.Add(form);
        Controls.Add(sag);
        Controls.Add(TanitimPaneli());
        AcceptButton = giris;

        giris.Click += (_, _) =>
        {
            if (Arayuz.Dene(() => kimlik.GirisYap(eposta.Text, parola.Text)))
                DialogResult = DialogResult.OK;
            parola.Clear();
        };
        // Modal pencere olay bittikten sonra acilir; erisilebilirlik araclari olay icinde kilitlenmesin.
        kayit.LinkClicked += (_, _) => BeginInvoke(() =>
        {
            using var kayitFormu = new KayitForm(kimlik);
            if (kayitFormu.ShowDialog(this) == DialogResult.OK)
                MessageBox.Show(this, "Kayıt tamamlandı. Giriş yapabilirsiniz.", "Kayıt");
        });
    }

    private static Panel TanitimPaneli()
    {
        const int genislik = 260;
        var icerik = Tema.Yigin();
        icerik.Controls.Add(Tema.Etiket("Uyum Eşleştirme", Tema.Baslik, Color.White, genislik));
        var aciklama = Tema.Etiket("Yaşam tarzınıza uygun ev veya yurt arkadaşını bulun.", Tema.Govde, Tema.BirincilSoluk, genislik);
        aciklama.Margin = new Padding(0, 0, 0, 28);
        icerik.Controls.Add(aciklama);
        foreach (var madde in new[] { "Profilinizi oluşturun", "Tercih anketini doldurun", "Size uyan kişileri görün" })
            icerik.Controls.Add(Tema.Etiket("✓  " + madde, Tema.Govde, Color.White, genislik));

        var panel = new Panel { Dock = DockStyle.Left, Width = 320, BackColor = Tema.Koyu, Padding = new Padding(30, 56, 30, 24) };
        panel.Controls.Add(icerik);
        return panel;
    }
}
