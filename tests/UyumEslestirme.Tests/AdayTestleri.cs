using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public sealed class AdayTestleri : IDisposable
{
    private readonly TestOrtami _o = new();

    public void Dispose() => _o.Dispose();

    [Fact]
    public void Ayni_cevaplar_yuzde_yuz_zit_cevaplar_yuzde_sifir_verir()
    {
        var ben = _o.Ogrenci("ben", cevap: _ => (1, 3));
        _o.Ogrenci("ayni", cevap: _ => (1, 3));
        _o.Ogrenci("zit", cevap: _ => (5, 3));
        _o.GirisYap(ben);

        var sonuc = _o.Adaylar.Adaylar();

        Assert.Equal(100, sonuc.Single(a => a.AdSoyad == "ayni").UyumYuzdesi, 5);
        Assert.Equal(0, sonuc.Single(a => a.AdSoyad == "zit").UyumYuzdesi, 5);
    }

    [Fact]
    public void Skor_iki_yonde_de_aynidir()
    {
        var a = _o.Ogrenci("a", cevap: s => (1 + s.Sira % 5, 1 + s.Sira % 3));
        var b = _o.Ogrenci("b", cevap: s => (1 + s.Sira * 2 % 5, 5 - s.Sira % 4));

        _o.GirisYap(a);
        var ab = _o.Adaylar.Adaylar().Single().UyumYuzdesi;
        _o.Db.UyumSkorlari.RemoveRange(_o.Db.UyumSkorlari);
        _o.Db.SaveChanges();
        _o.GirisYap(b);
        var ba = _o.Adaylar.Adaylar().Single().UyumYuzdesi;

        Assert.Equal(ab, ba, 8);
    }

    [Fact]
    public void Onem_derecesi_skoru_etkiler()
    {
        // Ilk soruda tam uyusmazlik, digerlerinde tam uyum.
        var ben = _o.Ogrenci("ben", cevap: s => (3, s.Sira == 1 ? 5 : 1));
        _o.Ogrenci("onemli", cevap: s => (s.Sira == 1 ? 5 : 3, s.Sira == 1 ? 5 : 1));
        _o.GirisYap(ben);
        var ilkSoruOnemli = _o.Adaylar.Adaylar().Single().UyumYuzdesi;

        var ben2 = _o.Ogrenci("ben2", cevap: s => (3, s.Sira == 1 ? 1 : 5));
        _o.GirisYap(ben2);
        var ilkSoruOnemsiz = _o.Adaylar.Adaylar().Single(a => a.AdSoyad == "onemli").UyumYuzdesi;

        Assert.True(ilkSoruOnemli < ilkSoruOnemsiz);
    }

    [Fact]
    public void En_uyumlu_aday_basta_gelir_ve_kategoriler_belirlenir()
    {
        var ben = _o.Ogrenci("ben", cevap: _ => (3, 3));
        _o.Ogrenci("uzak", cevap: _ => (5, 3));
        _o.Ogrenci("yakin", cevap: s => (s.Kategori == SoruKategorisi.Temizlik ? 5 : 3, 3));
        _o.GirisYap(ben);

        var sonuc = _o.Adaylar.Adaylar();

        Assert.Equal("yakin", sonuc[0].AdSoyad);
        Assert.Equal(SoruKategorisi.Temizlik, sonuc[0].EnFarkliKategori);
        Assert.NotEqual(SoruKategorisi.Temizlik, sonuc[0].EnUyumluKategori);
    }

    [Fact]
    public void Kendisi_yonetici_eksik_ve_gizli_kullanicilar_listelenmez()
    {
        var ben = _o.TamOgrenci("ben");
        _o.Ogrenci("yonetici", cevap: _ => (3, 3), rol: Rol.Yonetici);
        _o.Ogrenci("anketsiz");
        _o.Ogrenci("profilsiz", profil: false, cevap: _ => (3, 3));
        _o.TamOgrenci("gizli", gorunur: false);
        _o.TamOgrenci("tam");
        _o.GirisYap(ben);

        var sonuc = _o.Adaylar.Adaylar();

        Assert.Equal("tam", Assert.Single(sonuc).AdSoyad);
    }

    [Fact]
    public void Profilini_gizleyen_kullanici_listeden_cikar()
    {
        var ben = _o.TamOgrenci("ben");
        var diger = _o.TamOgrenci("diger");

        _o.GirisYap(diger);
        _o.Profil.Kaydet(new ProfilGirdisi("Muhasebe", "Bingöl", 2005, null, Gorunur: false));

        _o.GirisYap(ben);
        Assert.Empty(_o.Adaylar.Adaylar());
    }

    [Fact]
    public void Anketini_tamamlamayan_adaylari_goremez()
    {
        var ben = _o.Ogrenci("ben");
        _o.TamOgrenci("tam");
        _o.GirisYap(ben);

        Assert.Throws<IsKuraliHatasi>(() => _o.Adaylar.Adaylar());
    }

    [Fact]
    public void Girissiz_erisim_engellenir()
    {
        Assert.Throws<YetkisizErisimHatasi>(() => _o.Adaylar.Adaylar());
    }

    [Fact]
    public void Yas_dogum_yilindan_hesaplanir_ve_hassas_alanlar_sonucta_yer_almaz()
    {
        var ben = _o.TamOgrenci("ben");
        _o.TamOgrenci("tam");
        _o.GirisYap(ben);

        var aday = _o.Adaylar.Adaylar().Single();

        Assert.Equal(2026 - 2005, aday.Yas);
        Assert.DoesNotContain(typeof(AdayOzeti).GetProperties(), p => p.Name is "Eposta" or "DogumYili" or "ParolaHash");
    }

    [Fact]
    public void Skor_onbellege_yazilir_ve_anket_degisince_gecersiz_olur()
    {
        var ben = _o.Ogrenci("ben", cevap: _ => (1, 3));
        var diger = _o.Ogrenci("diger", cevap: _ => (1, 3));
        _o.GirisYap(ben);

        Assert.Equal(100, _o.Adaylar.Adaylar().Single().UyumYuzdesi, 5);
        var kayit = Assert.Single(_o.Db.UyumSkorlari.ToList());
        Assert.True(kayit.KullaniciAId < kayit.KullaniciBId);

        _o.GirisYap(diger);
        _o.CevaplariKaydet(_ => (5, 3));
        Assert.Empty(_o.Db.UyumSkorlari.ToList());

        _o.GirisYap(ben);
        Assert.Equal(0, _o.Adaylar.Adaylar().Single().UyumYuzdesi, 5);
    }
}
