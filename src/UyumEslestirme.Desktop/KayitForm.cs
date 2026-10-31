using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class KayitForm : Form
{
    public KayitForm(KimlikServisi kimlik)
    {
        Text = "Kayıt ol";
        Font = Tema.Govde;
        BackColor = Color.White;
        ClientSize = new Size(460, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var form = Tema.Yigin();
        form.Controls.Add(Tema.Etiket("Hesap oluşturun", Tema.Baslik, Tema.Metin));
        var ad = Tema.Alan(form, "Ad soyad", new TextBox());
        var eposta = Tema.Alan(form, "E-posta", new TextBox());
        var parola = Tema.Alan(form, "Parola", new TextBox { UseSystemPasswordChar = true });
        var tekrar = Tema.Alan(form, "Parola (tekrar)", new TextBox { UseSystemPasswordChar = true });
        var ipucu = Tema.Etiket(
            $"En az {KimlikServisi.AsgariParolaUzunlugu} karakter; harf ve rakam içermelidir.",
            Tema.Kucuk, Tema.SolukMetin);
        ipucu.Margin = new Padding(0, 0, 0, 16);
        form.Controls.Add(ipucu);

        var kaydet = Tema.BirincilDugme("Kayıt ol");
        kaydet.Dock = DockStyle.Fill;
        kaydet.Margin = new Padding(0);
        form.Controls.Add(kaydet);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(36, 28, 36, 20) };
        panel.Controls.Add(form);
        Controls.Add(panel);
        AcceptButton = kaydet;

        kaydet.Click += (_, _) =>
        {
            if (Arayuz.Dene(() => kimlik.KayitOl(ad.Text, eposta.Text, parola.Text, tekrar.Text)))
                DialogResult = DialogResult.OK;
        };
    }
}
