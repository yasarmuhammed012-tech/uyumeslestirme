# ER Diyagramı

```mermaid
erDiagram
    KULLANICI ||--o| PROFIL : "sahiptir (1-1)"
    KULLANICI ||--o{ ANKET_CEVABI : "verir"
    ANKET_SORUSU ||--o{ ANKET_CEVABI : "yanıtlanır"
    KULLANICI ||--o{ UYUM_SKORU : "A tarafı"
    KULLANICI ||--o{ UYUM_SKORU : "B tarafı"
    KULLANICI ||--o{ ESLESME : "gönderir"
    KULLANICI ||--o{ ESLESME : "alır"
    KULLANICI ||--o{ ILAN : "yayınlar"
    KULLANICI ||--o{ MESAJ : "gönderir"
    KULLANICI ||--o{ MESAJ : "alır"
    KULLANICI ||--o{ BILDIRIM : "alır"

    KULLANICI {
        int Id PK
        string Eposta UK
        string ParolaHash
        string AdSoyad
        int Rol
        int BasarisizGiris
        datetime KilitBitis
        datetime OlusturmaTarihi
    }
    PROFIL {
        int KullaniciId PK,FK
        string Bolum
        int DogumYili
        string Sehir
        string Hakkinda
    }
    ANKET_SORUSU {
        int Id PK
        int Kategori
        string Metin
        int Sira
        bool Aktif
    }
    ANKET_CEVABI {
        int KullaniciId PK,FK
        int SoruId PK,FK
        int Deger "1-5"
        int Agirlik "1-5"
    }
    UYUM_SKORU {
        int Id PK
        int KullaniciAId FK
        int KullaniciBId FK
        double Skor "0-100"
        datetime HesaplamaTarihi
    }
    ESLESME {
        int Id PK
        int GonderenId FK
        int AliciId FK
        int Durum
        datetime OlusturmaTarihi
    }
    ILAN {
        int Id PK
        int SahipId FK
        string Baslik
        string Aciklama
        string Sehir
        decimal AylikKira
        bool Aktif
        datetime OlusturmaTarihi
    }
    MESAJ {
        int Id PK
        int GonderenId FK
        int AliciId FK
        string Icerik
        bool Okundu
        datetime GonderimTarihi
    }
    BILDIRIM {
        int Id PK
        int KullaniciId FK
        string Metin
        bool Okundu
        datetime OlusturmaTarihi
    }
```

## İlişkiler

| Tür | Örnek | Nasıl kuruldu |
| --- | --- | --- |
| 1-1 | Kullanıcı — Profil | `Profil.KullaniciId` hem birincil hem yabancı anahtar. Bir kullanıcının en fazla bir profili olabilir. |
| 1-N | Kullanıcı — İlan | `Ilan.SahipId` yabancı anahtarı. Bir kullanıcı birden çok ilan verebilir. |
| N-N | Kullanıcı — Anket sorusu | `AnketCevabi` ara tablosu. Birleşik birincil anahtar `(KullaniciId, SoruId)` aynı soruya iki kez cevap verilmesini engeller; tablo ayrıca `Deger` ve `Agirlik` taşır. |

## Normalizasyon

- **1NF:** Her alan tek değer tutar. Tercihler tek bir metin alanına virgülle yazılmaz, her biri `AnketCevabi` satırıdır.
- **2NF:** `AnketCevabi` içindeki `Deger` ve `Agirlik` birleşik anahtarın tamamına bağlıdır. Soru metni bu tabloda tekrar edilmez.
- **3NF:** Soru metni yalnızca `AnketSorusu`, kişisel bilgiler yalnızca `Kullanici` ve `Profil` tablosundadır.

`UyumSkoru`, cevaplardan hesaplanabilen bir değerdir. Her listelemede tüm çiftleri yeniden hesaplamamak için bilinçli olarak saklanır.
