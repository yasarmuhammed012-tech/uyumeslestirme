using UyumEslestirme.Business;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Tests;

// Hafta 9: bildirim tetikleyicileri, tercihler ve mesajlasma kurallari.
public sealed class BildirimVeMesajTestleri : IDisposable
{
    private const string Konu = "uyum-test-konusu-123";

    private readonly TestOrtami _o = new();
    private readonly int _ali;
    private readonly int _ayse;

    public BildirimVeMesajTestleri()
    {
        _ali = _o.TamOgrenci("ali");
        _ayse = _o.TamOgrenci("ayse");
    }

    public void Dispose() => _o.Dispose();

    private void Tercih(int kim, bool eslesme = true, bool mesaj = true, string? konu = Konu)
    {
        _o.GirisYap(kim);
        _o.Bildirim.TercihleriKaydet(new BildirimTercihleri(eslesme, mesaj, konu));
    }

    private int Eslestir()
    {
        _o.GirisYap(_ali);
        _o.Eslesme.IstekGonder(_ayse);
        var id = _o.Db.Eslesmeler.Single().Id;
        _o.GirisYap(_ayse);
        _o.Eslesme.Yanitla(id, kabul: true);
        return id;
    }

    private List<BildirimOzeti> Bildirimleri(int kim)
    {
        _o.GirisYap(kim);
        return _o.Bildirim.Listele();
    }

    [Fact]
    public void Istek_gonderilince_aliciya_bildirim_olusur_ve_telefona_kuyruklanir()
    {
        Tercih(_ayse);
        _o.GirisYap(_ali);
        _o.Eslesme.IstekGonder(_ayse);

        var b = Assert.Single(Bildirimleri(_ayse));
        Assert.Equal(BildirimTuru.EslesmeIstegi, b.Tur);
        Assert.False(b.Okundu);
        Assert.Equal(HariciGonderimDurumu.Bekliyor, b.HariciDurum);

        var is_ = Assert.Single(_o.Kuyruk.Isler);
        Assert.Equal(Konu, is_.Konu);
        Assert.Equal(b.Id, is_.BildirimId);
        // Telefona giden metinde gonderenin adi yoktur.
        Assert.DoesNotContain("Test", is_.Metin);
        Assert.Empty(Bildirimleri(_ali));
    }

    [Fact]
    public void Kabulde_gonderene_bildirim_gider_retde_gitmez()
    {
        Eslestir();
        Assert.Equal(BildirimTuru.IstekKabul, Assert.Single(Bildirimleri(_ali)).Tur);

        var veli = _o.TamOgrenci("veli");
        _o.GirisYap(veli);
        _o.Eslesme.IstekGonder(_ali);
        var id = _o.Db.Eslesmeler.Single(e => e.GonderenId == veli).Id;
        _o.GirisYap(_ali);
        _o.Eslesme.Yanitla(id, kabul: false);

        Assert.Empty(Bildirimleri(veli));
    }

    [Fact]
    public void Tercih_kapaliysa_uygulama_ici_bildirim_yazilir_ama_telefona_gitmez()
    {
        Tercih(_ayse, eslesme: false);
        _o.GirisYap(_ali);
        _o.Eslesme.IstekGonder(_ayse);

        Assert.Equal(HariciGonderimDurumu.Kapali, Assert.Single(Bildirimleri(_ayse)).HariciDurum);
        Assert.Empty(_o.Kuyruk.Isler);
    }

    [Fact]
    public void Konu_tanimli_degilse_telefona_gitmez()
    {
        _o.GirisYap(_ali);
        _o.Eslesme.IstekGonder(_ayse);

        Assert.Equal(HariciGonderimDurumu.Kapali, Assert.Single(Bildirimleri(_ayse)).HariciDurum);
        Assert.Empty(_o.Kuyruk.Isler);
    }

    [Fact]
    public void Kuyruk_doluysa_bildirim_basarisiz_isaretlenir_ama_istek_yine_gonderilir()
    {
        Tercih(_ayse);
        _o.Kuyruk.Dolu = true;
        _o.GirisYap(_ali);
        _o.Eslesme.IstekGonder(_ayse);

        Assert.Single(_o.Db.Eslesmeler);
        var b = Assert.Single(Bildirimleri(_ayse));
        Assert.Equal(HariciGonderimDurumu.Basarisiz, b.HariciDurum);
        Assert.NotNull(b.HariciHata);
    }

    [Theory]
    [InlineData("kisa")]
    [InlineData("bosluk iceren konu adi")]
    [InlineData("türkçe-karakterli-konu")]
    public void Gecersiz_konu_adi_reddedilir(string konu)
    {
        _o.GirisYap(_ali);
        Assert.Throws<IsKuraliHatasi>(() => _o.Bildirim.TercihleriKaydet(new BildirimTercihleri(true, true, konu)));
    }

    [Fact]
    public void Tercihler_kaydedilir_ve_okunur()
    {
        Tercih(_ali, eslesme: false, mesaj: true);
        Assert.Equal(new BildirimTercihleri(false, true, Konu), _o.Bildirim.Tercihlerim());

        Tercih(_ali, konu: "  ");
        Assert.Null(_o.Bildirim.Tercihlerim().TelefonKonusu);
    }

    [Fact]
    public void Tumunu_okundu_yalnizca_kendi_bildirimlerini_etkiler()
    {
        Eslestir();
        _o.GirisYap(_ayse);
        _o.Mesaj.Gonder(_ali, "Merhaba");
        _o.GirisYap(_ali);
        Assert.Equal(2, _o.Bildirim.OkunmamisSayisi());

        _o.Bildirim.TumunuOkundu();

        Assert.Equal(0, _o.Bildirim.OkunmamisSayisi());
        Assert.Equal(1, _o.Db.Bildirimler.Count(b => b.KullaniciId == _ayse && !b.Okundu));
    }

    [Fact]
    public void Deneme_bildirimi_konu_yoksa_reddedilir_varsa_kuyruklanir()
    {
        _o.GirisYap(_ali);
        Assert.Throws<IsKuraliHatasi>(_o.Bildirim.DenemeGonder);

        Tercih(_ali);
        _o.Bildirim.DenemeGonder();
        Assert.Equal(BildirimTuru.Deneme, Assert.Single(_o.Bildirim.Listele()).Tur);
        Assert.Single(_o.Kuyruk.Isler);
    }

    [Fact]
    public void Eslesmis_kisiler_mesajlasir_ve_aliciya_bildirim_gider()
    {
        Eslestir();
        Tercih(_ali);
        _o.GirisYap(_ayse);
        _o.Mesaj.Gonder(_ali, "  Merhaba, ev için konuşalım mı?  ");

        _o.GirisYap(_ali);
        var m = Assert.Single(_o.Mesaj.Konusma(_ayse));
        Assert.Equal("Merhaba, ev için konuşalım mı?", m.Icerik);
        Assert.False(m.Benim);
        Assert.True(_o.Db.Mesajlar.Single().Okundu);

        Assert.Contains(Bildirimleri(_ali), b => b.Tur == BildirimTuru.YeniMesaj);
        // Mesaj icerigi telefona gitmez.
        Assert.DoesNotContain(_o.Kuyruk.Isler, i => i.Metin.Contains("Merhaba"));
    }

    [Fact]
    public void Eslesmemis_veya_bekleyen_kisiye_mesaj_gonderilemez()
    {
        _o.GirisYap(_ali);
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Gonder(_ayse, "Merhaba"));
        _o.Eslesme.IstekGonder(_ayse);
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Gonder(_ayse, "Merhaba"));
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Konusma(_ayse));
        Assert.Empty(_o.Db.Mesajlar);
    }

    [Fact]
    public void Bos_uzun_ve_cok_hizli_mesaj_reddedilir()
    {
        Eslestir();
        _o.GirisYap(_ali);
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Gonder(_ayse, "   "));
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Gonder(_ayse, new string('a', MesajServisi.AzamiUzunluk + 1)));

        for (var i = 0; i < MesajServisi.DakikadaAzamiMesaj; i++)
            _o.Mesaj.Gonder(_ayse, $"Mesaj {i}");
        Assert.Throws<IsKuraliHatasi>(() => _o.Mesaj.Gonder(_ayse, "bir tane daha"));

        _o.Saat.Ilerlet(TimeSpan.FromMinutes(1));
        _o.Mesaj.Gonder(_ayse, "bir dakika sonra");
    }

    [Fact]
    public void Yonetici_mesaj_gonderemez()
    {
        var yonetici = _o.Ogrenci("yonetici", profil: false, rol: Rol.Yonetici);
        _o.GirisYap(yonetici, Rol.Yonetici);
        Assert.Throws<YetkisizErisimHatasi>(() => _o.Mesaj.Gonder(_ali, "Merhaba"));
    }
}
