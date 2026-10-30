# Hafta 4: Veritabanı ve Veri Erişim Katmanı

## Şema nasıl oluşuyor

Tablolar EF Core migration ile oluşturulur (`src/UyumEslestirme.Data/Migrations`). Migration, şema değişikliklerini kod olarak sürümler: aynı şema her bilgisayarda aynı komutla kurulur ve değişiklikler Git geçmişinde izlenir.

## Kısıtlar

| Kısıt | Tablo | Ne sağlar |
| --- | --- | --- |
| Yabancı anahtar | Tüm ilişkiler | Olmayan bir kullanıcıya kayıt bağlanamaz. |
| Benzersiz indeks | `Kullanici.Eposta` | Aynı e-posta ile iki hesap açılamaz. |
| Birleşik birincil anahtar | `AnketCevabi` | Bir kullanıcı aynı soruya iki kez cevap veremez. |
| `CHECK` 1-5 | `AnketCevabi.Deger`, `Agirlik` | Ölçek dışı değer veritabanına giremez. |
| `CHECK` A < B | `UyumSkoru` | Bir çift için tek skor satırı olur. |
| `CHECK` Gönderen ≠ Alıcı | `Eslesme` | Kullanıcı kendine istek gönderemez. |

## İndeksler

| İndeks | Neden |
| --- | --- |
| `Kullanici(Eposta)` | Her girişte e-posta ile arama yapılır. |
| `Ilan(Sehir, Aktif)` | İlan listesi şehir ve aktiflik durumuna göre süzülür. |
| `Mesaj(AliciId, GonderimTarihi)` | Gelen kutusu alıcıya göre tarih sıralı okunur. |
| `Bildirim(KullaniciId, Okundu)` | Okunmamış bildirimler hızlı bulunur. |

`Ilan.Aciklama` gibi uzun metin alanlarına indeks eklenmedi: bu alanlarda eşitlik araması yapılmıyor, indeks yalnızca yazmayı yavaşlatıp yer kaplardı.

## Örnek veri

20 öğrenci, 1 yönetici, 10 anket sorusu, 200 cevap ve 5 ilan üretilir. Tüm kayıtlar sentetiktir: adlar `Örnek Öğrenci 01` biçimindedir ve e-postalar ayrılmış `.test` alan adını kullanır. Gerçek kişisel veri olmadığı için KVKK kapsamında bir risk oluşturmaz.

## Örnek CRUD SQL karşılıkları

```sql
INSERT INTO Kullanicilar (Eposta, ParolaHash, AdSoyad, Rol, BasarisizGiris, OlusturmaTarihi)
VALUES ('a@ornek.test', '...', 'Test', 0, 0, '2026-10-30');

SELECT * FROM Kullanicilar WHERE Eposta = 'a@ornek.test';

UPDATE Kullanicilar SET AdSoyad = 'Yeni Ad' WHERE Id = 1;

DELETE FROM Kullanicilar WHERE Id = 1;
```
