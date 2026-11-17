using UyumEslestirme.Domain;

namespace UyumEslestirme.Business;

// API ile web istemcisi arasinda tasinan veriler. Varliklar (Kullanici, Profil...) disariya
// dogrudan verilmez; parola ozeti, kilit bilgisi ve gezinme ozellikleri boylece hic tasinmaz.
// Not: istek govdelerinde kullanici kimligi yoktur; sunucu kimligi token'dan okur.

public sealed record KayitIstegi(string AdSoyad, string Eposta, string Parola, string ParolaTekrar);

public sealed record GirisIstegi(string Eposta, string Parola);

public sealed record GirisYaniti(string Token, DateTimeOffset Bitis, string AdSoyad, Rol Rol);

public sealed record ProfilBilgisi(string Bolum, string Sehir, int DogumYili, string? Hakkinda, bool Gorunur)
{
    public static ProfilBilgisi Olustur(Profil p) => new(p.Bolum, p.Sehir, p.DogumYili, p.Hakkinda, p.Gorunur);
}

public sealed record GorunurlukIstegi(bool Gorunur);

public sealed record SoruBilgisi(int Id, SoruKategorisi Kategori, string Metin, int Sira)
{
    public static SoruBilgisi Olustur(AnketSorusu s) => new(s.Id, s.Kategori, s.Metin, s.Sira);
}

public sealed record IstekGonderIstegi(int AliciId);

public sealed record HataYaniti(string Mesaj);
