namespace UyumEslestirme.Business;

// Kullaniciya gosterilebilecek dogrulama ve is kurali hatalari.
public class IsKuraliHatasi(string mesaj) : Exception(mesaj);

public class YetkisizErisimHatasi(string mesaj = "Bu işlem için yetkiniz yok.") : Exception(mesaj);
