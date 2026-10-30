# Hafta 6: Profil ve Tercih Anketi

## Veri modeli

- **`Profil`:** Kullanıcıyla 1-1 ilişkilidir. `KullaniciId` hem birincil hem yabancı anahtardır.
- **`AnketSorusu`:** Soru metni, kategori ve sıra bilgisini tutar. Sorular veri olduğu için yeni soru eklemek kod değişikliği gerektirmez. Soruyu kaldırmak yerine `Aktif = false` yapılır, böylece eski cevaplar korunur.
- **`AnketCevabi`:** Kullanıcı ile soru arasındaki N-N ilişkinin ara tablosudur. Her satır `Deger` ve `Agirlik` taşır.

## Ölçek

Her soru için iki değer girilir, ikisi de 1-5 aralığındadır:

- **Cevap (`Deger`):** Likert ölçeği. 1 = hiç katılmıyorum, 5 = tamamen katılıyorum.
- **Önem (`Agirlik`):** Bu konunun kullanıcı için ne kadar önemli olduğu. 1 = önemsiz, 5 = çok önemli.

Önem ayrı tutulur, çünkü iki kişi aynı cevabı verse de konunun onlar için önemi farklı olabilir. Örneğin sessizliği seven ama buna çok önem vermeyen biri, gürültüye daha kolay uyum sağlar.

## Tercih özeti

Her kategori için iki değer hesaplanır:

```text
Ortalama      = Σ(Deger × Agirlik) / Σ(Agirlik)
Önem yüzdesi  = Kategorinin Σ(Agirlik) / Tüm soruların Σ(Agirlik) × 100
```

## İş kuralları

| Kural | Neden |
| --- | --- |
| Tüm aktif sorular cevaplanmalı | Eksik anketle uyum skoru güvenilir hesaplanamaz. |
| Cevap ve önem 1-5 arasında olmalı | Ölçek dışı değer skoru bozar. Veritabanında `CHECK` kısıtıyla da korunur. |
| Aynı soru tekrar edemez, geçersiz soru cevaplanamaz | Hatalı veya kurcalanmış girdiyi engeller. |
| Yaş 16-80 arasında olmalı | Yanlış girilmiş doğum yıllarını engeller. |
| Bölüm ve şehir zorunlu | Eşleştirme ve ilan süzme bu alanları kullanacak. |

## Erişim ve sahiplik

`AnketServisi` kullanıcı kimliğini parametre olarak almaz, oturumdan okur. Bu nedenle bir kullanıcının başkasının cevaplarını okuması veya değiştirmesi mümkün değildir. `ProfilServisi.Goruntule` ise başkasının profilini yalnızca yöneticiye açar.

## Diğer modüllerle entegrasyon

- **Kimlik doğrulama (Hafta 5):** Tüm işlemler `OturumYoneticisi` üzerinden açık oturum ister.
- **Veri katmanı (Hafta 4):** Servisler `AnketDeposu` ve `Depo<Profil>` kullanır. Cevaplar transaction içinde değiştirilir, hata olursa eski cevaplar korunur.
- **Uyum skorlama (sonraki haftalar):** `Deger` ve `Agirlik` verileri skor hesabının girdisi olacak.
