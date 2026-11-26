using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api.Kimlik;

public sealed class JwtAyarlari
{
    public const string Bolum = "Jwt";
    public const int AsgariAnahtarUzunlugu = 32;

    // Koda yazilmaz: gelistirmede user-secrets, yayinda Jwt__Anahtar ortam degiskeni.
    public string Anahtar { get; set; } = "";
    public string Yayinci { get; set; } = "UyumEslestirme.Api";
    public string Hedef { get; set; } = "UyumEslestirme.Web";

    // Dosya varsa icindeki anahtari okur; yoksa 48 baytlik rastgele anahtar uretip dosyaya yazar.
    public static string DosyadanVeyaYeni(string dosya)
    {
        if (File.Exists(dosya))
        {
            var mevcut = File.ReadAllText(dosya).Trim();
            if (Encoding.UTF8.GetByteCount(mevcut) >= AsgariAnahtarUzunlugu)
                return mevcut;
        }
        var yeni = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        File.WriteAllText(dosya, yeni);
        return yeni;
    }

    public SymmetricSecurityKey GuvenlikAnahtari()
    {
        if (Encoding.UTF8.GetByteCount(Anahtar) < AsgariAnahtarUzunlugu)
        {
            throw new InvalidOperationException(
                $"Jwt:Anahtar ayarı yok veya {AsgariAnahtarUzunlugu} bayttan kısa. " +
                "Geliştirmede: dotnet user-secrets set \"Jwt:Anahtar\" \"<rastgele uzun değer>\" --project src/UyumEslestirme.Api; " +
                "yayında Jwt__Anahtar ortam değişkenini tanımlayın.");
        }
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Anahtar));
    }
}

// Token'daki alan adlari. Kullanici kimligi istek govdesinden degil, yalnizca buradan okunur.
public static class Talepler
{
    public const string KullaniciId = JwtRegisteredClaimNames.Sub;
    public const string AdSoyad = "name";
    public const string Rol = "role";
    public const string TokenId = JwtRegisteredClaimNames.Jti;

    public static (int Id, string AdSoyad, Rol Rol)? Oku(ClaimsPrincipal kullanici)
    {
        if (kullanici.Identity?.IsAuthenticated != true
            || !int.TryParse(kullanici.FindFirstValue(KullaniciId), out var id)
            || !Enum.TryParse<Rol>(kullanici.FindFirstValue(Rol), out var rol))
        {
            return null;
        }
        return (id, kullanici.FindFirstValue(AdSoyad) ?? "", rol);
    }
}

public sealed class TokenServisi(JwtAyarlari ayarlar, TimeProvider saat)
{
    // Masaustundeki oturum suresiyle ayni: 30 dakika.
    public static TimeSpan Sure => OturumYoneticisi.OturumSuresi;

    private readonly SigningCredentials _imza = new(ayarlar.GuvenlikAnahtari(), SecurityAlgorithms.HmacSha256);

    // KimlikServisi.GirisYap basariyla oturumu actiktan sonra cagrilir.
    public GirisYaniti Uret(OturumYoneticisi oturum)
    {
        var simdi = saat.GetUtcNow();
        var bitis = simdi + Sure;
        var token = new JwtSecurityToken(
            issuer: ayarlar.Yayinci,
            audience: ayarlar.Hedef,
            claims:
            [
                new Claim(Talepler.KullaniciId, oturum.KullaniciId.ToString()),
                new Claim(Talepler.AdSoyad, oturum.AdSoyad),
                new Claim(Talepler.Rol, oturum.Rol.ToString()),
                new Claim(Talepler.TokenId, Guid.NewGuid().ToString("N")),
            ],
            notBefore: simdi.UtcDateTime,
            expires: bitis.UtcDateTime,
            signingCredentials: _imza);

        return new GirisYaniti(new JwtSecurityTokenHandler().WriteToken(token), bitis, oturum.AdSoyad, oturum.Rol);
    }
}

// JWT kendi basina gecerli oldugu icin cikis yapilan token'lar suresi dolana kadar burada tutulur.
// Bellekte tutulur: sunucu yeniden baslarsa liste bosalir (token en fazla 30 dakika gecerlidir).
public sealed class IptalListesi(TimeProvider saat)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _iptaller = new();

    public void Ekle(string tokenId, DateTimeOffset bitis)
    {
        var simdi = saat.GetUtcNow();
        foreach (var (id, b) in _iptaller)
        {
            if (b <= simdi)
                _iptaller.TryRemove(id, out _);
        }
        _iptaller[tokenId] = bitis;
    }

    public bool IptalMi(string? tokenId) => tokenId is not null && _iptaller.ContainsKey(tokenId);
}
