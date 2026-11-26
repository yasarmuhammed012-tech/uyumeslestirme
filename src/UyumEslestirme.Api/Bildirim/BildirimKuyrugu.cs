using System.Threading.Channels;
using UyumEslestirme.Business;
using UyumEslestirme.Data;
using UyumEslestirme.Domain;

namespace UyumEslestirme.Api.Bildirim;

// Bellek ici kuyruk: istekler bildirimi buraya birakip hemen yanit doner.
public sealed class BildirimKuyrugu : IBildirimKuyrugu
{
    public const int Kapasite = 500;

    private readonly Channel<HariciBildirimIsi> _kanal = Channel.CreateBounded<HariciBildirimIsi>(
        new BoundedChannelOptions(Kapasite) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    public ChannelReader<HariciBildirimIsi> Okuyucu => _kanal.Reader;

    public bool Ekle(HariciBildirimIsi is_) => _kanal.Writer.TryWrite(is_);
}

// Arka planda kuyruktaki bildirimleri harici kanala gonderir ve sonucu (Gonderildi/Basarisiz)
// veritabanindaki bildirime yazar. Bir bildirimin hatasi digerlerini ve uygulamayi etkilemez.
public sealed class BildirimGondericisi(
    BildirimKuyrugu kuyruk,
    IHariciBildirimKanali kanal,
    IServiceScopeFactory kapsamlar,
    ILogger<BildirimGondericisi> gunluk) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken durdur)
    {
        await foreach (var is_ in kuyruk.Okuyucu.ReadAllAsync(durdur))
        {
            HariciGonderimDurumu durum;
            string? hata = null;
            try
            {
                await kanal.Gonder(is_, durdur);
                durum = HariciGonderimDurumu.Gonderildi;
            }
            catch (BildirimGonderimHatasi h)
            {
                durum = HariciGonderimDurumu.Basarisiz;
                hata = h.Message;
            }
            catch (OperationCanceledException) when (durdur.IsCancellationRequested)
            {
                return;
            }
            catch (Exception h)
            {
                gunluk.LogError(h, "Bildirim {Id} gönderilirken beklenmeyen hata", is_.BildirimId);
                durum = HariciGonderimDurumu.Basarisiz;
                hata = "Beklenmeyen bir hata oluştu.";
            }

            try
            {
                using var kapsam = kapsamlar.CreateScope();
                var db = kapsam.ServiceProvider.GetRequiredService<UygulamaDbContext>();
                if (db.Bildirimler.Find(is_.BildirimId) is { } bildirim)
                {
                    bildirim.HariciDurum = durum;
                    bildirim.HariciHata = hata?.Length > 200 ? hata[..200] : hata;
                    db.SaveChanges();
                }
            }
            catch (Exception h)
            {
                gunluk.LogError(h, "Bildirim {Id} durumu kaydedilemedi", is_.BildirimId);
            }
        }
    }
}
