namespace UyumEslestirme.Domain;

public enum Rol
{
    Ogrenci = 0,
    Yonetici = 1,
}

public enum SoruKategorisi
{
    UykuDuzeni = 0,
    Temizlik = 1,
    Sosyallik = 2,
    Gurultu = 3,
    Ders = 4,
}

public enum EslesmeDurumu
{
    Beklemede = 0,
    Kabul = 1,
    Red = 2,
}

public enum BildirimTuru
{
    EslesmeIstegi = 0,
    IstekKabul = 1,
    YeniMesaj = 2,
    Deneme = 3,
}

public enum HariciGonderimDurumu
{
    Kapali = 0,      // tercih kapali veya konu tanimli degil
    Bekliyor = 1,    // kuyrukta
    Gonderildi = 2,
    Basarisiz = 3,
}
