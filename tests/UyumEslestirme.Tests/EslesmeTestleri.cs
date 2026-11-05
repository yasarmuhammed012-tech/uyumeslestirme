using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

public sealed class EslesmeTestleri : IDisposable
{
    private readonly TestOrtami _o = new();
    private readonly int _ali;
    private readonly int _ayse;

    public EslesmeTestleri()
    {
        _ali = _o.TamOgrenci("ali");
        _ayse = _o.TamOgrenci("ayse");
    }

    public void Dispose() => _o.Dispose();

    private int IstekGonder(int gonderen, int alici)
    {
        _o.GirisYap(gonderen);
        _o.Eslesme.IstekGonder(alici);
        return _o.Db.Eslesmeler.Single(e => e.GonderenId == gonderen && e.AliciId == alici).Id;
    }

    private AdayDurumu Durum(int bakan, int aday)
    {
        _o.GirisYap(bakan);
        return _o.Adaylar.Adaylar().Single(a => a.KullaniciId == aday).Durum;
    }

    [Fact]
    public void Istek_gonderilir_ve_iki_tarafta_dogru_durum_gorunur()
    {
        IstekGonder(_ali, _ayse);

        Assert.Equal(AdayDurumu.IstekGonderildi, Durum(_ali, _ayse));
        Assert.Equal(AdayDurumu.IstekGeldi, Durum(_ayse, _ali));
        _o.GirisYap(_ayse);
        Assert.Equal("ali", Assert.Single(_o.Eslesme.Listele().GelenIstekler).AdSoyad);
    }

    [Fact]
    public void Kabul_sonrasi_eslesir_ve_eposta_yalnizca_simdi_gorunur()
    {
        var id = IstekGonder(_ali, _ayse);
        _o.GirisYap(_ayse);
        Assert.Null(_o.Eslesme.Listele().GelenIstekler.Single().Eposta);

        _o.Eslesme.Yanitla(id, kabul: true);

        Assert.Equal("ali@ornek.test", Assert.Single(_o.Eslesme.Listele().Eslesmeler).Eposta);
        _o.GirisYap(_ali);
        Assert.Equal("ayse@ornek.test", Assert.Single(_o.Eslesme.Listele().Eslesmeler).Eposta);
        Assert.Equal(AdayDurumu.Eslesti, Durum(_ali, _ayse));
    }

    [Fact]
    public void Gonderilen_ve_bekleyen_isteklerde_eposta_gorunmez()
    {
        IstekGonder(_ali, _ayse);

        Assert.Null(_o.Eslesme.Listele().GonderilenIstekler.Single().Eposta);
    }

    [Fact]
    public void Reddedilince_iki_taraf_da_birbirini_listede_gormez_ve_tekrar_istek_gonderilemez()
    {
        var id = IstekGonder(_ali, _ayse);
        _o.GirisYap(_ayse);
        _o.Eslesme.Yanitla(id, kabul: false);

        Assert.Empty(_o.Adaylar.Adaylar());
        _o.GirisYap(_ali);
        Assert.Empty(_o.Adaylar.Adaylar());
        Assert.Equal(EslesmeDurumu.Red, _o.Eslesme.Listele().GonderilenIstekler.Single().Durum);
        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(_ayse));
    }

    [Fact]
    public void Kendine_istek_gonderilemez()
    {
        _o.GirisYap(_ali);

        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(_ali));
    }

    [Fact]
    public void Ayni_kisiye_iki_kez_veya_ters_yonde_istek_gonderilemez()
    {
        IstekGonder(_ali, _ayse);

        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(_ayse));
        _o.GirisYap(_ayse);
        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(_ali));
        Assert.Single(_o.Db.Eslesmeler.ToList());
    }

    [Fact]
    public void Uygun_olmayan_kisilere_ayni_mesajla_istek_gonderilemez()
    {
        var gizli = _o.TamOgrenci("gizli", gorunur: false);
        var anketsiz = _o.Ogrenci("anketsiz");
        var yonetici = _o.Ogrenci("yonetici", cevap: _ => (3, 3), rol: Rol.Yonetici);
        _o.GirisYap(_ali);

        var mesajlar = new[] { gizli, anketsiz, yonetici, 9999 }
            .Select(id => Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(id)).Message)
            .Distinct()
            .ToList();

        Assert.Single(mesajlar);
        Assert.Empty(_o.Db.Eslesmeler.ToList());
    }

    [Fact]
    public void Anketini_tamamlamayan_istek_gonderemez()
    {
        var anketsiz = _o.Ogrenci("anketsiz2");
        _o.GirisYap(anketsiz);

        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(_ali));
    }

    [Fact]
    public void Yonetici_istek_gonderemez()
    {
        _o.GirisYap(_ali, Rol.Yonetici);

        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.IstekGonder(_ayse));
    }

    [Fact]
    public void Bekleyen_istek_sayisi_sinirlidir()
    {
        var kisiler = Enumerable.Range(1, EslesmeServisi.AzamiBekleyenIstek + 1).Select(i => _o.TamOgrenci($"kisi{i}")).ToList();
        _o.GirisYap(_ali);
        foreach (var kisi in kisiler.Take(EslesmeServisi.AzamiBekleyenIstek))
            _o.Eslesme.IstekGonder(kisi);

        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.IstekGonder(kisiler[^1]));
    }

    [Fact]
    public void Istegi_yalnizca_alici_yanitlayabilir_ve_bir_kez_yanitlanir()
    {
        var id = IstekGonder(_ali, _ayse);
        var ucuncu = _o.TamOgrenci("ucuncu");

        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.Yanitla(id, kabul: true));
        _o.GirisYap(ucuncu);
        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.Yanitla(id, kabul: true));

        _o.GirisYap(_ayse);
        _o.Eslesme.Yanitla(id, kabul: true);
        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.Yanitla(id, kabul: false));
    }

    [Fact]
    public void Istegi_yalnizca_gonderen_ve_yalnizca_yanit_beklerken_geri_cekebilir()
    {
        var id = IstekGonder(_ali, _ayse);

        _o.GirisYap(_ayse);
        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.GeriCek(id));

        _o.GirisYap(_ali);
        _o.Eslesme.GeriCek(id);
        Assert.Empty(_o.Db.Eslesmeler.ToList());
        Assert.Equal(AdayDurumu.Yok, Durum(_ali, _ayse));

        var yeni = IstekGonder(_ali, _ayse);
        _o.GirisYap(_ayse);
        _o.Eslesme.Yanitla(yeni, kabul: true);
        _o.GirisYap(_ali);
        Assert.Throws<IsKuraliHatasi>(() => _o.Eslesme.GeriCek(yeni));
    }

    [Fact]
    public void Eslesme_listesi_uyum_skorunu_icerir()
    {
        IstekGonder(_ali, _ayse);

        Assert.Equal(100, _o.Eslesme.Listele().GonderilenIstekler.Single().UyumYuzdesi!.Value, 5);
    }

    [Fact]
    public void Girissiz_eslesme_islemleri_engellenir()
    {
        _o.Oturum.Kapat();

        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.IstekGonder(_ayse));
        Assert.Throws<YetkisizErisimHatasi>(() => _o.Eslesme.Listele());
    }
}
