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
