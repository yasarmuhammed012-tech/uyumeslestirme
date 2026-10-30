using UyumEslestirme.Business;

namespace UyumEslestirme.Desktop;

public sealed class GirisForm : Form
{
    public GirisForm(KimlikServisi kimlik)
    {
        Text = "Giriş";
        ClientSize = new Size(420, 190);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var duzen = Arayuz.FormDuzeni(3);
        var eposta = Arayuz.AlanEkle(duzen, "E-posta", new TextBox());
        var parola = Arayuz.AlanEkle(duzen, "Parola", new TextBox { UseSystemPasswordChar = true });

        var giris = new Button { Text = "Giriş yap", AutoSize = true };
        var kayit = new Button { Text = "Kayıt ol", AutoSize = true };
        var dugmeler = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        dugmeler.Controls.AddRange([giris, kayit]);
        duzen.Controls.Add(new Label());
        duzen.Controls.Add(dugmeler);
        Controls.Add(duzen);
        AcceptButton = giris;

        giris.Click += (_, _) =>
        {
            if (Arayuz.Dene(() => kimlik.GirisYap(eposta.Text, parola.Text)))
                DialogResult = DialogResult.OK;
            parola.Clear();
        };
        kayit.Click += (_, _) =>
        {
            using var form = new KayitForm(kimlik);
            if (form.ShowDialog(this) == DialogResult.OK)
                MessageBox.Show(this, "Kayıt tamamlandı. Giriş yapabilirsiniz.", "Kayıt");
        };
    }
}
