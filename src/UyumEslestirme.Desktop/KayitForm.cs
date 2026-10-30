using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class KayitForm : Form
{
    public KayitForm(KimlikServisi kimlik)
    {
        Text = "Kayıt ol";
        ClientSize = new Size(440, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var duzen = Arayuz.FormDuzeni(5);
        var ad = Arayuz.AlanEkle(duzen, "Ad soyad", new TextBox());
        var eposta = Arayuz.AlanEkle(duzen, "E-posta", new TextBox());
        var parola = Arayuz.AlanEkle(duzen, "Parola", new TextBox { UseSystemPasswordChar = true });
        var tekrar = Arayuz.AlanEkle(duzen, "Parola (tekrar)", new TextBox { UseSystemPasswordChar = true });

        var kaydet = new Button { Text = "Kaydol", AutoSize = true };
        duzen.Controls.Add(new Label());
        duzen.Controls.Add(kaydet);
        Controls.Add(duzen);
        AcceptButton = kaydet;

        kaydet.Click += (_, _) =>
        {
            if (Arayuz.Dene(() => kimlik.KayitOl(ad.Text, eposta.Text, parola.Text, tekrar.Text)))
                DialogResult = DialogResult.OK;
        };
    }
}
