using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Data;

public class UygulamaDbContext(DbContextOptions<UygulamaDbContext> options) : DbContext(options)
{
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Profil> Profiller => Set<Profil>();
    public DbSet<AnketSorusu> AnketSorulari => Set<AnketSorusu>();
    public DbSet<AnketCevabi> AnketCevaplari => Set<AnketCevabi>();
    public DbSet<UyumSkoru> UyumSkorlari => Set<UyumSkoru>();
    public DbSet<Eslesme> Eslesmeler => Set<Eslesme>();
    public DbSet<Ilan> Ilanlar => Set<Ilan>();
    public DbSet<Mesaj> Mesajlar => Set<Mesaj>();
    public DbSet<Bildirim> Bildirimler => Set<Bildirim>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Kullanici>(e =>
        {
            e.Property(x => x.Eposta).HasMaxLength(200).IsRequired();
            e.Property(x => x.AdSoyad).HasMaxLength(100).IsRequired();
            e.Property(x => x.ParolaHash).HasMaxLength(200).IsRequired();
            // Giris sirasinda e-posta ile arama yapilir; benzersizlik de bu indeksle saglanir.
            e.HasIndex(x => x.Eposta).IsUnique();
        });

        b.Entity<Profil>(e =>
        {
            e.HasKey(x => x.KullaniciId);
            e.Property(x => x.Bolum).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sehir).HasMaxLength(60).IsRequired();
            e.Property(x => x.Hakkinda).HasMaxLength(500);
            e.HasOne(x => x.Kullanici).WithOne(k => k.Profil)
                .HasForeignKey<Profil>(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AnketSorusu>(e =>
        {
            e.Property(x => x.Metin).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Sira).IsUnique();
        });

        b.Entity<AnketCevabi>(e =>
        {
            e.HasKey(x => new { x.KullaniciId, x.SoruId });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_AnketCevabi_Deger", "\"Deger\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_AnketCevabi_Agirlik", "\"Agirlik\" BETWEEN 1 AND 5");
            });
            e.HasOne(x => x.Kullanici).WithMany(k => k.Cevaplar)
                .HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Soru).WithMany(s => s.Cevaplar)
                .HasForeignKey(x => x.SoruId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<UyumSkoru>(e =>
        {
            // Her cift tek satir: A her zaman B'den kucuk tutulur, (A,B) ve (B,A) ayni anda olamaz.
            e.HasIndex(x => new { x.KullaniciAId, x.KullaniciBId }).IsUnique();
            e.HasIndex(x => x.KullaniciBId);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_UyumSkoru_Sira", "\"KullaniciAId\" < \"KullaniciBId\"");
                t.HasCheckConstraint("CK_UyumSkoru_Aralik", "\"Skor\" BETWEEN 0 AND 100");
            });
            e.HasOne(x => x.KullaniciA).WithMany().HasForeignKey(x => x.KullaniciAId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.KullaniciB).WithMany().HasForeignKey(x => x.KullaniciBId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Eslesme>(e =>
        {
            e.HasIndex(x => new { x.GonderenId, x.AliciId }).IsUnique();
            e.HasIndex(x => x.AliciId);
            e.ToTable(t => t.HasCheckConstraint("CK_Eslesme_Farkli", "\"GonderenId\" <> \"AliciId\""));
            e.HasOne(x => x.Gonderen).WithMany().HasForeignKey(x => x.GonderenId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alici).WithMany().HasForeignKey(x => x.AliciId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Ilan>(e =>
        {
            e.Property(x => x.Baslik).HasMaxLength(120).IsRequired();
            e.Property(x => x.Aciklama).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Sehir).HasMaxLength(60).IsRequired();
            e.Property(x => x.AylikKira).HasPrecision(10, 2);
            // Ilan listesi en cok sehir ve aktiflik durumuna gore suzulur.
            e.HasIndex(x => new { x.Sehir, x.Aktif });
            e.ToTable(t => t.HasCheckConstraint("CK_Ilan_Kira", "\"AylikKira\" >= 0"));
            e.HasOne(x => x.Sahip).WithMany(k => k.Ilanlar).HasForeignKey(x => x.SahipId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Mesaj>(e =>
        {
            e.Property(x => x.Icerik).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.AliciId, x.GonderimTarihi });
            e.HasOne(x => x.Gonderen).WithMany().HasForeignKey(x => x.GonderenId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alici).WithMany().HasForeignKey(x => x.AliciId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Bildirim>(e =>
        {
            e.Property(x => x.Metin).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.KullaniciId, x.Okundu });
            e.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
