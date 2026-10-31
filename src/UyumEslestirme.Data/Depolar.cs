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

    public List<Kullanici> ProfilliOgrenciler(int haricKullaniciId) =>
        Db.Kullanicilar.AsNoTracking()
            .Include(k => k.Profil)
            .Include(k => k.Cevaplar)
            .Where(k => k.Rol == Rol.Ogrenci && k.Id != haricKullaniciId && k.Profil != null)
            .ToList();
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
        Db.AnketCevaplari.AddRange(cevaplar);
        Db.SaveChanges();
        islem.Commit();
    }
}
