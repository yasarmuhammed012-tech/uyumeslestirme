using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public class VeriKatmaniTestleri
{
    private static Kullanici YeniKullanici(string eposta = "a@ornek.test") => new()
    {
        Eposta = eposta,
        AdSoyad = "Test",
        ParolaHash = VeriTohumlayici.GirisKapaliHash,
        OlusturmaTarihi = DateTime.UtcNow,
    };

    [Fact]
    public void Crud_ekle_oku_guncelle_sil_calisir()
    {
        using var t = new TestVeritabani();
        var depo = new KullaniciDeposu(t.Db);

        var k = depo.Ekle(YeniKullanici());
        Assert.NotNull(depo.EpostaIleBul("a@ornek.test"));

        k.AdSoyad = "Yeni Ad";
        depo.Guncelle(k);
        Assert.Equal("Yeni Ad", depo.Bul(k.Id)!.AdSoyad);

        depo.Sil(k);
        Assert.Null(depo.Bul(k.Id));
    }

    [Fact]
    public void Ayni_eposta_ikinci_kez_eklenemez()
    {
        using var t = new TestVeritabani();
        var depo = new KullaniciDeposu(t.Db);
        depo.Ekle(YeniKullanici());

        Assert.Throws<DbUpdateException>(() => depo.Ekle(YeniKullanici()));
    }

    [Fact]
    public void Yabanci_anahtar_olmayan_kullaniciya_ilani_engeller()
    {
        using var t = new TestVeritabani();
        var depo = new IlanDeposu(t.Db);

        Assert.Throws<DbUpdateException>(() => depo.Ekle(new Ilan
        {
            SahipId = 9999,
            Baslik = "x",
            Aciklama = "x",
            Sehir = "Bingöl",
            OlusturmaTarihi = DateTime.UtcNow,
        }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Kontrol_kisiti_aralik_disi_cevabi_engeller(int deger)
    {
        using var t = new TestVeritabani();
        VeriTohumlayici.Tohumla(t.Db, VeriTohumlayici.GirisKapaliHash, VeriTohumlayici.GirisKapaliHash, DateTime.UtcNow);
        var kullanici = t.Db.Kullanicilar.First(k => k.Rol == Rol.Yonetici);
        var soru = t.Db.AnketSorulari.First();

        t.Db.AnketCevaplari.Add(new AnketCevabi { KullaniciId = kullanici.Id, SoruId = soru.Id, Deger = deger, Agirlik = 3 });

        Assert.Throws<DbUpdateException>(() => t.Db.SaveChanges());
    }

    [Fact]
    public void Tohumlama_ornek_veriyi_olusturur_ve_tekrar_calisinca_cogaltmaz()
    {
        using var t = new TestVeritabani();
        VeriTohumlayici.Tohumla(t.Db, "h", "h", DateTime.UtcNow);
        VeriTohumlayici.Tohumla(t.Db, "h", "h", DateTime.UtcNow);

        Assert.Equal(21, t.Db.Kullanicilar.Count());
        Assert.Equal(10, t.Db.AnketSorulari.Count());
        Assert.Equal(200, t.Db.AnketCevaplari.Count());
        Assert.Equal(5, t.Db.Ilanlar.Count());
        Assert.All(t.Db.Kullanicilar.ToList(), k => Assert.EndsWith("@ornek.test", k.Eposta));
    }

    [Fact]
    public void Kullanici_silinince_profil_ve_cevaplari_da_silinir()
    {
        using var t = new TestVeritabani();
        VeriTohumlayici.Tohumla(t.Db, "h", "h", DateTime.UtcNow);
        var ogrenci = t.Db.Kullanicilar.First(k => k.Rol == Rol.Ogrenci);
        var id = ogrenci.Id;

        t.Db.Kullanicilar.Remove(ogrenci);
        t.Db.SaveChanges();

        Assert.False(t.Db.Profiller.Any(p => p.KullaniciId == id));
        Assert.False(t.Db.AnketCevaplari.Any(c => c.KullaniciId == id));
    }
}
