using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UyumEslestirme.Data;

// "dotnet ef migrations" komutu DbContext'i bu sinif uzerinden olusturur.
public class TasarimZamaniDbContextFabrikasi : IDesignTimeDbContextFactory<UygulamaDbContext>
{
    public UygulamaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UygulamaDbContext>()
            .UseSqlite("Data Source=tasarim.db")
            .Options;
        return new UygulamaDbContext(options);
    }
}
