# Değişiklik Günlüğü

Sürüm numaraları [Semantic Versioning](https://semver.org/lang/tr/) kuralına uyar. Her sürüm Git'te `vX.Y.Z` etiketiyle işaretlenir.

## [0.2.0] - 2026-11-26

9. ve 10. haftaların modülleri. Web arayüzü artık sunucuya ve gerçek veritabanına bağlı.

### Eklendi

- **Sunucu (API):** ASP.NET Core API; JWT ile giriş, SQLite veritabanı, giriş ve kayıtta hız sınırı, çıkışta token iptali. Web arayüzü aynı sunucudan yayınlanır; kayıt olan kullanıcılar veritabanında kalıcı olarak saklanır.
- **Bildirimler (Hafta 9):** Eşleşme isteği, kabul ve yeni mesajda bildirim; menüde okunmamış sayısı. İsteğe bağlı olarak ntfy REST API ile telefona gönderim. Kuyruk ve arka plan gönderici; zaman aşımı, yeniden deneme, 429 ve 401 yönetimi. Bildirim tercihleri (migration `Hafta9Bildirim`).
- **Mesajlaşma:** Yalnızca eşleşmiş kişiler arasında, dakikada en fazla 20 mesaj.
- **Açıklamalı uyum önerisi (Hafta 10):** Skora göre seçilen 3 kişi için Claude'un yazdığı gerekçe. JSON şemalı çıktı ve gerçek değerlerle doğrulama; hata, zaman aşımı veya geçersiz çıktıda kural tabanlı gerekçe.
- **Testler:** Bildirim ve mesaj kuralları, ntfy hata senaryoları, öneri doğrulayıcı ve API uçları için yeni testler. GitHub Actions API testlerini de çalıştırıyor.

### Değişti

- Web arayüzü iki modda çalışıyor: sunucudan açılınca API ve veritabanı, GitHub Pages'te bellek içi demo.
- `Servisler` isteğe bağlı bildirim kuyruğu ve yapay zekâ istemcisi alıyor; masaüstü değişmeden çalışıyor.

## [0.1.0] - 2026-11-12

İlk ara sürüm (vize). 3-7. haftaların modülleri uçtan uca birlikte çalışıyor.

### Eklendi

- **Kimlik doğrulama (Hafta 5):** Kayıt, giriş, çıkış, rol tabanlı yetki (öğrenci ve yönetici), 30 dakikalık oturum, PBKDF2 parola özeti, hatalı denemede hesap kilidi.
- **Profil ve tercih anketi (Hafta 6):** 5 kategoride 10 soru, her soruda cevap ve önem derecesi, kategori bazında tercih özeti.
- **Uyum skorlama ve eşleştirme (Hafta 7):** Ağırlıklı uyum skoru ve skor önbelleği. Arkadaş bul listesi ve şehir filtresi. İstek gönderme, kabul, ret ve geri çekme. Görünürlük ayarı. E-posta yalnızca karşılıklı kabulden sonra görünür.
- **Yönetim ekranı:** Kullanıcı listesi ve arama.
- **Web sürümü:** Aynı iş katmanıyla çalışan Blazor WebAssembly arayüzü, GitHub Pages'te yayında.
- **Entegrasyon testleri (Hafta 8):** Kayıttan eşleşmeye tam akış, SQLite ve bellek içi veritabanında.
- **Sürüm numarası:** Giriş ekranında görünür.

### Düzeltildi

- Kilitli hesap farklı bir hata mesajı veriyor ve parola kontrolünü atlıyordu. Bu yüzden bir e-postanın kayıtlı olduğu anlaşılabiliyordu. Artık tüm başarısız girişler aynı mesajı veriyor.
- Web sürümünde anket kaydı bellek içi veritabanında çalışmıyordu. Kayıt artık iki veritabanında da aynı kodla ve tek işlemde yapılıyor.
- Anket soruları kategori içinde aynı yöne çevrildi (migration `SoruYonuDuzeltme`).
