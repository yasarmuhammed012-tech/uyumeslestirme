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

public class BildirimDeposu(UygulamaDbContext db) : Depo<Bildirim>(db)
{
    public List<Bildirim> KullanicininBildirimleri(int kullaniciId, int adet) =>
        Db.Bildirimler.AsNoTracking()
            .Where(b => b.KullaniciId == kullaniciId)
            .OrderByDescending(b => b.OlusturmaTarihi).ThenByDescending(b => b.Id)
            .Take(adet)
            .ToList();

    public int OkunmamisSayisi(int kullaniciId) =>
        Db.Bildirimler.Count(b => b.KullaniciId == kullaniciId && !b.Okundu);

    public void TumunuOkundu(int kullaniciId)
    {
        foreach (var b in Db.Bildirimler.Where(b => b.KullaniciId == kullaniciId && !b.Okundu).ToList())
            b.Okundu = true;
        Db.SaveChanges();
    }

    public BildirimTercihi? Tercih(int kullaniciId) => Db.BildirimTercihleri.Find(kullaniciId);

    public void TercihKaydet(BildirimTercihi tercih)
    {
        if (Db.Entry(tercih).State == EntityState.Detached)
            Db.BildirimTercihleri.Add(tercih);
        Db.SaveChanges();
    }
}

public class MesajDeposu(UygulamaDbContext db) : Depo<Mesaj>(db)
{
    // Iki kisi arasindaki son mesajlar, eskiden yeniye.
    public List<Mesaj> Arasinda(int a, int b, int adet) =>
        Db.Mesajlar.AsNoTracking()
            .Where(m => (m.GonderenId == a && m.AliciId == b) || (m.GonderenId == b && m.AliciId == a))
            .OrderByDescending(m => m.GonderimTarihi).ThenByDescending(m => m.Id)
            .Take(adet)
            .AsEnumerable()
            .Reverse()
            .ToList();

    public int GonderilenSayisi(int gonderenId, DateTime baslangic) =>
        Db.Mesajlar.Count(m => m.GonderenId == gonderenId && m.GonderimTarihi > baslangic);

    public void OkunduIsaretle(int aliciId, int gonderenId)
    {
        foreach (var m in Db.Mesajlar.Where(m => m.AliciId == aliciId && m.GonderenId == gonderenId && !m.Okundu).ToList())
            m.Okundu = true;
        Db.SaveChanges();
    }
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

    // Tum degisiklikler tek SaveChanges ile yazilir; EF bunu tek transaction icinde yapar,
    // yarida kalirsa eski cevaplar korunur. ExecuteDelete kullanilmaz: ayni kod web surumundeki
    // bellek ici veritabaninda da calisir.
    public void CevaplariDegistir(int kullaniciId, IReadOnlyList<AnketCevabi> cevaplar)
    {
        var yeniler = cevaplar.ToDictionary(c => c.SoruId);
        foreach (var eski in Db.AnketCevaplari.Where(c => c.KullaniciId == kullaniciId).ToList())
        {
            if (yeniler.Remove(eski.SoruId, out var yeni))
            {
                eski.Deger = yeni.Deger;
                eski.Agirlik = yeni.Agirlik;
            }
            else
            {
                Db.AnketCevaplari.Remove(eski);
            }
        }
        Db.AnketCevaplari.AddRange(yeniler.Values);
        // Cevaplar degisince bu kullaniciya ait onbellekteki uyum skorlari gecersiz olur.
        Db.UyumSkorlari.RemoveRange(Db.UyumSkorlari.Where(s => s.KullaniciAId == kullaniciId || s.KullaniciBId == kullaniciId));
        Db.SaveChanges();
    }
}
