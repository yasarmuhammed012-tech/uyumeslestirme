using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Data;

public class Depo<T>(UygulamaDbContext db) where T : class
{
    protected readonly UygulamaDbContext Db = db;

    public T? Bul(params object[] anahtar) => Db.Set<T>().Find(anahtar);

    public List<T> Listele() => Db.Set<T>().AsNoTracking().ToList();

    public T Ekle(T varlik)
    {
        Db.Set<T>().Add(varlik);
        Db.SaveChanges();
        return varlik;
    }

    public void Guncelle(T varlik)
    {
        Db.Set<T>().Update(varlik);
        Db.SaveChanges();
    }

    public void Sil(T varlik)
    {
        Db.Set<T>().Remove(varlik);
        Db.SaveChanges();
    }
}

public class KullaniciDeposu(UygulamaDbContext db) : Depo<Kullanici>(db)
{
    public Kullanici? EpostaIleBul(string eposta) =>
        Db.Kullanicilar.FirstOrDefault(k => k.Eposta == eposta);

    public bool EpostaVarMi(string eposta) =>
        Db.Kullanicilar.Any(k => k.Eposta == eposta);

    public Kullanici? ProfilIleBul(int id) =>
        Db.Kullanicilar.AsNoTracking().Include(k => k.Profil).FirstOrDefault(k => k.Id == id);

    public List<Kullanici> ProfilliOgrenciler(int haricKullaniciId) =>
        Db.Kullanicilar.AsNoTracking()
            .Include(k => k.Profil)
            .Include(k => k.Cevaplar)
            .Where(k => k.Rol == Rol.Ogrenci && k.Id != haricKullaniciId && k.Profil != null)
            .ToList();
}

public class EslesmeDeposu(UygulamaDbContext db) : Depo<Eslesme>(db)
{
    // Iki kisi arasindaki istek, kim gonderdiyse.
    public Eslesme? Arasinda(int a, int b) =>
        Db.Eslesmeler.AsNoTracking().FirstOrDefault(e =>
            (e.GonderenId == a && e.AliciId == b) || (e.GonderenId == b && e.AliciId == a));

    public List<Eslesme> KullanicininEslesmeleri(int kullaniciId) =>
        Db.Eslesmeler.AsNoTracking()
            .Include(e => e.Gonderen).ThenInclude(k => k!.Profil)
            .Include(e => e.Alici).ThenInclude(k => k!.Profil)
            .Where(e => e.GonderenId == kullaniciId || e.AliciId == kullaniciId)
            .OrderByDescending(e => e.OlusturmaTarihi)
            .ToList();

    public int BekleyenGidenSayisi(int kullaniciId) =>
        Db.Eslesmeler.Count(e => e.GonderenId == kullaniciId && e.Durum == EslesmeDurumu.Beklemede);
}

public class UyumSkoruDeposu(UygulamaDbContext db) : Depo<UyumSkoru>(db)
{
    public UyumSkoru? Bul(int a, int b)
    {
        var (kucuk, buyuk) = Sirala(a, b);
        return Db.UyumSkorlari.AsNoTracking()
            .FirstOrDefault(s => s.KullaniciAId == kucuk && s.KullaniciBId == buyuk);
    }

    public void Kaydet(int a, int b, double skor, DateTime simdi)
    {
        var (kucuk, buyuk) = Sirala(a, b);
        Ekle(new UyumSkoru { KullaniciAId = kucuk, KullaniciBId = buyuk, Skor = skor, HesaplamaTarihi = simdi });
    }

    // Tabloda her cift tek satirdir ve kucuk kimlik her zaman A tarafindadir (CHECK kisiti).
    private static (int, int) Sirala(int a, int b) => a < b ? (a, b) : (b, a);
}

public class IlanDeposu(UygulamaDbContext db) : Depo<Ilan>(db)
{
    public List<Ilan> AktifIlanlar(string sehir) =>
        Db.Ilanlar.AsNoTracking()
            .Where(i => i.Sehir == sehir && i.Aktif)
            .OrderByDescending(i => i.OlusturmaTarihi)
            .ToList();
}

public class AnketDeposu(UygulamaDbContext db) : Depo<AnketSorusu>(db)
{
    public List<AnketSorusu> AktifSorular() =>
        Db.AnketSorulari.AsNoTracking()
            .Where(s => s.Aktif)
            .OrderBy(s => s.Sira)
            .ToList();

    public List<AnketCevabi> KullaniciCevaplari(int kullaniciId) =>
        Db.AnketCevaplari.AsNoTracking()
            .Include(c => c.Soru)
            .Where(c => c.KullaniciId == kullaniciId)
            .OrderBy(c => c.Soru!.Sira)
            .ToList();

    // Eski cevaplarin silinmesi ve yenilerin eklenmesi tek islemdir: yarida kalirsa eski cevaplar korunur.
    public void CevaplariDegistir(int kullaniciId, IReadOnlyList<AnketCevabi> cevaplar)
    {
        // ExecuteDelete takip edilen nesneleri bilmez; ayni anahtarli eski kayitlar cakismasin diye takip temizlenir.
        Db.ChangeTracker.Clear();
        using var islem = Db.Database.BeginTransaction();
        Db.AnketCevaplari.Where(c => c.KullaniciId == kullaniciId).ExecuteDelete();
        // Cevaplar degisince bu kullaniciya ait onbellekteki uyum skorlari gecersiz olur.
        Db.UyumSkorlari.Where(s => s.KullaniciAId == kullaniciId || s.KullaniciBId == kullaniciId).ExecuteDelete();
        Db.AnketCevaplari.AddRange(cevaplar);
        Db.SaveChanges();
        islem.Commit();
    }
}
