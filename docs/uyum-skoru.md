# Uyum Skoru

Hesap `UyumHesaplayici` sınıfındadır. Sınıf veritabanına ve oturuma bağlı değildir, yalnızca iki kişinin cevaplarından skor üretir. Bu sayede tek başına test edilebilir.

## Hesaplama

Her aktif soru için:

```text
Benzerlik = 1 - |CevapA - CevapB| / 4
Ağırlık   = (ÖnemA + ÖnemB) / 2
```

Toplam skor:

```text
Uyum = Σ(Benzerlik × Ağırlık) / Σ(Ağırlık) × 100
```

- Aynı cevap benzerlik 1, en uzak cevap (1 ile 5) benzerlik 0 verir.
- İki kişinin de önem verdiği sorular skoru daha çok etkiler.
- Ağırlık iki kişinin ortalaması olduğu için A'nın B'ye uyumu, B'nin A'ya uyumuyla aynıdır.
- "En uyumlu" ve "en farklı" konular aynı formülün kategori bazında hesaplanmasıyla bulunur.

## Saklama

Skorlar `UyumSkoru` tablosunda önbellek olarak tutulur (`UyumServisi`):

- **Tek satır kuralı:** Her çift için tek satır vardır. Küçük kimlik her zaman `KullaniciAId` tarafındadır; bunu `CHECK (KullaniciAId < KullaniciBId)` kısıtı ve benzersiz indeks sağlar.
- **Okuma:** Skor istendiğinde önce tabloya bakılır, yoksa hesaplanıp yazılır.
- **Geçersiz kılma:** Bir kullanıcı anketini değiştirince ona ait tüm skorlar, cevaplarla aynı transaction içinde silinir. Bu yüzden eski skor gösterilmez.
