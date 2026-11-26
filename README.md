# Ev/Yurt Arkadaşı Uyum Eşleştirme (P19)

Öğrencilerin yaşam tarzı tercihlerine göre uyumlu ev/yurt arkadaşı bulmasını sağlayan uygulama. Sunucu (API + web arayüzü) ve masaüstü sürümü vardır.

**Ders:** Bilgisayar Uygulamaları I — Bingöl Üniversitesi, Bilgisayar Programcılığı

**Tarayıcıda dene (demo):** https://yasarmuhammed012-tech.github.io/uyumeslestirme/

**Güncel sürüm:** `v0.2.0` ([değişiklikler](CHANGELOG.md))

## Teknoloji

| Alan | Seçim |
| --- | --- |
| Dil / platform | C#, .NET 10 |
| Sunucu | ASP.NET Core Minimal API, JWT ile kimlik doğrulama |
| Arayüz | Blazor WebAssembly (web), Windows Forms (masaüstü) |
| Veritabanı | SQLite (tek dosya, kurulum gerektirmez) |
| Veri erişimi | Entity Framework Core |
| Bildirim (Hafta 9) | ntfy REST API (telefona anlık bildirim) |
| Yapay zekâ (Hafta 10) | Claude, Anthropic C# SDK |
| Test | xUnit, WebApplicationFactory |

## Çalıştırma

Gereksinim: .NET 10 SDK.

### Sunucu sürümü (gerçek veritabanı ile kayıt ve giriş)

Web arayüzü API sunucusundan yayınlanır. Kayıt olan kullanıcılar sunucudaki SQLite veritabanında (`veri/uyum.db`, yol `ConnectionStrings:Uyum` ayarında) kalıcı olarak saklanır.

İlk seferde JWT imza anahtarını tanımlayın (koda yazılmaz):

```powershell
dotnet user-secrets set "Jwt:Anahtar" "<en az 32 karakterlik rastgele bir değer>" --project src/UyumEslestirme.Api
```

Sonra:

```powershell
dotnet run --project src/UyumEslestirme.Api
```

Tarayıcıda http://localhost:5158 adresini açın, **Kayıt olun** ile hesap oluşturup giriş yapın. Geliştirme ortamında sentetik demo hesaplar da vardır (parola `Demo1234`): `yonetici@ornek.test`, `ogrenci01@ornek.test` … `ogrenci20@ornek.test`.

İsteğe bağlı anahtarlar (koda ve depoya yazılmaz, ortam değişkeni veya user-secrets):

| Ayar | Ne işe yarar | Yoksa |
| --- | --- | --- |
| `ANTHROPIC_API_KEY` | Önerilenler sayfasında gerekçeyi Claude yazar | Gerekçeler kural tabanlı yazılır |
| `Bildirim__Anahtar` | ntfy erişim anahtarı | Herkese açık ntfy konusuna anahtarsız gönderilir |

### Masaüstü sürümü

Yalnızca Windows'ta çalışır:

```powershell
dotnet run --project src/UyumEslestirme.Desktop
```

Veritabanı ilk açılışta yerel klasörde oluşur. Debug derlemesinde aynı demo hesaplar eklenir. Masaüstünde Bildirimler, Mesajlar ve Önerilenler ekranları henüz yoktur.

### Web demosu (GitHub Pages)

`main` dalına her gönderimde GitHub Actions testleri çalıştırır ve web arayüzünü GitHub Pages'te yayınlar. Orada sunucu olmadığı için arayüz demo modunda açılır, sunucu sürümünden şu açılardan ayrılır:

- **Veri:** Veriler yalnızca o tarayıcı sekmesinde tutulur, sayfa yenilenince demo verilere döner.
- **Parola özeti:** Tarayıcıda hızlı giriş için PBKDF2 tekrar sayısı düşürülmüştür. Burada gerçek parola kullanılmamalıdır.
- **Harici servisler:** Telefon bildirimi gönderilmez, öneri gerekçeleri kural tabanlıdır.

Arayüz açılışta `/api/saglik` adresini dener: sunucu varsa sunucu modunda, yoksa demo modunda çalışır.

### Testler

```powershell
dotnet test tests/UyumEslestirme.Tests
dotnet test tests/UyumEslestirme.Api.Tests
```

## Proje yapısı

```text
src/
  UyumEslestirme.Domain/    Varlıklar ve enum'lar (başka katmana bağımlı değil)
  UyumEslestirme.Data/      Veritabanı ve veri erişimi
  UyumEslestirme.Business/  İş kuralları, doğrulama, bildirim ve öneri mantığı
  UyumEslestirme.Api/       Sunucu: REST API, JWT, ntfy ve Claude bağlantıları, web arayüzünü yayınlar
  UyumEslestirme.Web/       Tarayıcı arayüzü (Blazor WebAssembly)
  UyumEslestirme.Desktop/   Windows Forms arayüzü
tests/
  UyumEslestirme.Tests/     Birim ve entegrasyon testleri
  UyumEslestirme.Api.Tests/ API testleri (sahte ntfy ve sahte Claude ile)
docs/                       ER diyagramı, mimari ve haftalık notlar
```

## Belgeler

- [ER diyagramı](docs/er-diyagrami.md)
- [Mimari](docs/mimari.md)
- [Hafta 4: Veritabanı](docs/hafta-4-veritabani.md)
- [Hafta 5: Güvenlik notu](docs/hafta-5-guvenlik-notu.md)
- [Hafta 6: Profil ve tercih anketi](docs/hafta-6-profil-anket.md)
- [Hafta 7: Uyum skorlama ve eşleştirme](docs/hafta-7-uyum-eslestirme.md)
- [Hafta 8: Ara sürüm, entegrasyon ve demo senaryosu](docs/hafta-8-ara-surum.md)
- [Hafta 9: Bildirim (harici API) ve gerçek veritabanı ile giriş](docs/hafta-9-bildirim.md)
- [Hafta 10: Açıklamalı uyum önerisi (yapay zekâ)](docs/hafta-10-aciklamali-oneri.md)
- [Uyum skoru hesabı](docs/uyum-skoru.md)
- [Değişiklik günlüğü](CHANGELOG.md)
- [Haftalık raporlar ve AI kullanım günlükleri](raporlar/)

## Haftalık ilerleme

- [x] Hafta 3: Kurulum, mimari ve veri modeli
- [x] Hafta 4: Veritabanı ve veri erişim katmanı
- [x] Hafta 5: Kullanıcı yönetimi ve kimlik doğrulama
- [x] Hafta 6: Profil ve tercih anketi
- [x] Hafta 7: Uyum skorlama ve eşleştirme
- [x] Hafta 8: Ara sürüm `v0.1.0`, uçtan uca entegrasyon testleri, demo senaryosu
- [x] Hafta 9: Bildirim entegrasyonu (ntfy), mesajlaşma, web arayüzünün sunucu ve veritabanına bağlanması
- [x] Hafta 10: Açıklamalı uyum önerisi (skor + Claude, çıktı doğrulama), sürüm `v0.2.0`
