namespace UyumEslestirme.Domain;

public class Kullanici
{
    public int Id { get; set; }
    public string Eposta { get; set; } = "";
    public string ParolaHash { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public Rol Rol { get; set; } = Rol.Ogrenci;
    public int BasarisizGiris { get; set; }
    public DateTime? KilitBitis { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public Profil? Profil { get; set; }
    public List<AnketCevabi> Cevaplar { get; set; } = [];
    public List<Ilan> Ilanlar { get; set; } = [];
}

public class Profil
{
    public int KullaniciId { get; set; }
    public string Bolum { get; set; } = "";
    public int DogumYili { get; set; }
    public string Sehir { get; set; } = "";
    public string? Hakkinda { get; set; }

    public Kullanici? Kullanici { get; set; }
}

public class AnketSorusu
{
    public int Id { get; set; }
    public SoruKategorisi Kategori { get; set; }
    public string Metin { get; set; } = "";
    public int Sira { get; set; }
    public bool Aktif { get; set; } = true;

    public List<AnketCevabi> Cevaplar { get; set; } = [];
}

// Kullanici ile AnketSorusu arasindaki N-N iliskinin ara tablosu.
public class AnketCevabi
{
    public int KullaniciId { get; set; }
    public int SoruId { get; set; }
    public int Deger { get; set; }
    public int Agirlik { get; set; }

    public Kullanici? Kullanici { get; set; }
    public AnketSorusu? Soru { get; set; }
}

public class UyumSkoru
{
    public int Id { get; set; }
    public int KullaniciAId { get; set; }
    public int KullaniciBId { get; set; }
    public double Skor { get; set; }
    public DateTime HesaplamaTarihi { get; set; }

    public Kullanici? KullaniciA { get; set; }
    public Kullanici? KullaniciB { get; set; }
}

public class Eslesme
{
    public int Id { get; set; }
    public int GonderenId { get; set; }
    public int AliciId { get; set; }
    public EslesmeDurumu Durum { get; set; } = EslesmeDurumu.Beklemede;
    public DateTime OlusturmaTarihi { get; set; }

    public Kullanici? Gonderen { get; set; }
    public Kullanici? Alici { get; set; }
}

public class Ilan
{
    public int Id { get; set; }
    public int SahipId { get; set; }
    public string Baslik { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Sehir { get; set; } = "";
    public decimal AylikKira { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; }

    public Kullanici? Sahip { get; set; }
}

public class Mesaj
{
    public int Id { get; set; }
    public int GonderenId { get; set; }
    public int AliciId { get; set; }
    public string Icerik { get; set; } = "";
    public bool Okundu { get; set; }
    public DateTime GonderimTarihi { get; set; }

    public Kullanici? Gonderen { get; set; }
    public Kullanici? Alici { get; set; }
}

public class Bildirim
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public string Metin { get; set; } = "";
    public bool Okundu { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public Kullanici? Kullanici { get; set; }
}
