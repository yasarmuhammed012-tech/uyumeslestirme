# Arkadaş Bul ve Uyum Skoru

**Arkadaş bul** sayfası, profilini ve anketini tamamlamış diğer öğrencileri kullanıcıya uyumlarına göre sıralar.

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
- "En uyumlu" ve "en farklı" alanları aynı formülün kategori bazında hesaplanmasıyla bulunur.

## Kurallar

| Kural | Neden |
| --- | --- |
| Adayları görmek için kendi anketini tamamlamak gerekir | Karşılaştıracak cevap olmadan skor hesaplanamaz. |
| Eksik profil veya anketi olanlar listelenmez | Skoru veya bilgileri eksik kişi gösterilmez. |
| Kullanıcının kendisi ve yöneticiler listelenmez | Yalnızca arkadaş arayan öğrenciler görünür. |

## Gizlilik

Sayfada ad, bölüm, şehir, yaş, hakkında yazısı ve uyum bilgileri görünür. E-posta, doğum yılı ve tek tek anket cevapları gösterilmez; `AdayOzeti` bu alanları hiç taşımaz.

## Şimdilik yapılmayanlar

Skor her açılışta hesaplanır, henüz `UyumSkoru` tablosuna yazılmaz. Arkadaşlık isteği ve mesajlaşma ilgili haftalarda eklenecek.
