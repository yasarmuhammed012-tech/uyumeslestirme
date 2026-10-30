using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UyumEslestirme.Business;

// Saklanan bicim: pbkdf2-sha256$tekrar$tuz$ozet
// Tekrar sayisi kayitla birlikte tutulur; ileride artirilsa da eski parolalar dogrulanabilir.
public static class ParolaHasleyici
{
    private const string Sema = "pbkdf2-sha256";
    private const int Tekrar = 600_000;
    private const int TuzBoyutu = 16;
    private const int OzetBoyutu = 32;

    public static string Hashle(string parola)
    {
        var tuz = RandomNumberGenerator.GetBytes(TuzBoyutu);
        var ozet = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(parola), tuz, Tekrar, HashAlgorithmName.SHA256, OzetBoyutu);
        return string.Join('$', Sema, Tekrar.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(tuz), Convert.ToBase64String(ozet));
    }

    public static bool Dogrula(string parola, string kayit)
    {
        var parcalar = kayit.Split('$');
        if (parcalar.Length != 4 || parcalar[0] != Sema
            || !int.TryParse(parcalar[1], NumberStyles.None, CultureInfo.InvariantCulture, out var tekrar) || tekrar <= 0)
        {
            return false;
        }

        byte[] tuz, beklenen;
        try
        {
            tuz = Convert.FromBase64String(parcalar[2]);
            beklenen = Convert.FromBase64String(parcalar[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var ozet = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(parola), tuz, tekrar, HashAlgorithmName.SHA256, beklenen.Length);
        return CryptographicOperations.FixedTimeEquals(ozet, beklenen);
    }
}
