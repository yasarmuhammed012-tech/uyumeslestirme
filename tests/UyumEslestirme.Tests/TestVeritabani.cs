using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UyumEslestirme.Data;

namespace UyumEslestirme.Tests;

// Her test icin bellekte ayri bir SQLite veritabani. Baglanti acik kaldikca veritabani yasar.
public sealed class TestVeritabani : IDisposable
{
    private readonly SqliteConnection _baglanti;

    public UygulamaDbContext Db { get; }

    public TestVeritabani()
    {
        _baglanti = new SqliteConnection("Data Source=:memory:");
        _baglanti.Open();
        var options = new DbContextOptionsBuilder<UygulamaDbContext>().UseSqlite(_baglanti).Options;
        Db = new UygulamaDbContext(options);
        Db.Database.Migrate();
    }

    public void Dispose()
    {
        Db.Dispose();
        _baglanti.Dispose();
    }
}
