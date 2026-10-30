# Hafta 5: Güvenlik Notu

## Parola saklama

Parolalar düz metin olarak saklanmaz. `ParolaHasleyici`, .NET'in yerleşik PBKDF2-SHA256 uygulamasını 600.000 tekrarla kullanır.

- **Hash:** Tek yönlüdür. Veritabanı sızsa bile parola doğrudan okunamaz.
- **Tuz (salt):** Her kullanıcı için rastgele üretilen 16 bayttır. Aynı parolayı kullanan iki kişinin kaydı farklı olur ve hazır tablolar (rainbow table) işe yaramaz.
- **Yavaşlık:** Yüksek tekrar sayısı, kaba kuvvet denemelerini pahalı hale getirir.
- **Sabit süreli karşılaştırma:** `FixedTimeEquals`, karşılaştırma süresinden bilgi sızmasını önler.

## Oturum ve token

| | Oturum | Token (JWT) |
| --- | --- | --- |
| Durum nerede | Uygulamada | Token'ın içinde, istemcide |
| İptal | Kolay, oturum kapatılır | Süresi dolana kadar zor |
| Uygun olduğu yer | Tek süreçli uygulama | Dağıtık web API |

Bu uygulama sunucusuz ve tek süreçli bir masaüstü programıdır; ağ üzerinden kimlik taşınmadığı için JWT gereksizdir. Bu nedenle bellekte tutulan oturum seçildi. Oturum 30 dakika sonra kapanır ve arayüz kullanıcıyı giriş ekranına döndürür.

## Başarısız giriş

- 5 hatalı denemeden sonra hesap 15 dakika kilitlenir.
- Olmayan e-posta ve yanlış parola aynı mesajı verir, böylece kayıtlı e-postalar deneme yoluyla öğrenilemez.
- Olmayan e-postada da hash hesabı yapılır, böylece yanıt süresi fark ettirmez.

## Yetkilendirme

Kimlik doğrulama "kimsin?", yetkilendirme "bunu yapabilir misin?" sorusunu yanıtlar.

Roller `Ogrenci` ve `Yonetici`'dir. Kontrol iş katmanındadır: `YonetimServisi` her çağrıda `oturum.RolGerekli(Rol.Yonetici)` çalıştırır. Öğrenci için yönetim sekmesi gösterilmez, ama asıl koruma bu değildir: düğme gizlense de servis çağrısı `YetkisizErisimHatasi` fırlatır. Bu davranış birim testiyle doğrulanır. Kayıt ekranı rol almaz, yeni hesaplar her zaman öğrencidir.

## Bilinen sınırlar

- **Yerel veritabanı:** SQLite dosyası kullanıcının bilgisayarındadır. Uygulama dışından dosyaya erişebilen biri parola özetleri dahil verileri okuyabilir, rol kaydını da değiştirebilir. Yetki kontrolü bu saldırıya karşı koruma sağlamaz. Gerçek çok kullanıcılı kullanımda veri merkezi bir sunucuda tutulmalıdır.
- **Hesap kilidi:** Kilit mesajı bir hesabın var olduğunu açığa çıkarabilir ve bir başkasının hesabını geçici olarak kilitlemek mümkündür.
- **Demo hesaplar:** Yalnızca Debug derlemesinde oluşur ve parolası kaynak kodda açıktır (`Demo1234`). Release derlemesinde oluşmaz.
