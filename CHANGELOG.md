# Değişiklik Günlüğü

Sürüm numaraları [Semantic Versioning](https://semver.org/lang/tr/) kuralına uyar. Her sürüm Git'te `vX.Y.Z` etiketiyle işaretlenir.

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
