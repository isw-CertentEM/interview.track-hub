using TrackHub.Application.Services;

namespace TrackHub.Application.BackgroundJobs;

/// <summary>
/// Scheduled by Hangfire every 5 minutes. For each active carrier, downloads
/// any new tracking files from the carrier's SFTP drop and feeds each one to
/// EventIngestionService.
///
/// Hangfire is configured with: AutomaticRetry(Attempts = 10).
/// </summary>
public class CarrierPollJob
{
    private readonly EventIngestionService _ingestion;
    private readonly ICarrierSftpClient _sftp;       // wraps SSH.NET
    private readonly ICarrierRegistry _carriers;     // lists active carriers

    public CarrierPollJob(EventIngestionService ingestion,
                          ICarrierSftpClient sftp,
                          ICarrierRegistry carriers)
    {
        _ingestion = ingestion;
        _sftp = sftp;
        _carriers = carriers;
    }

    public async Task RunAsync()
    {
        foreach (var carrier in _carriers.GetActive())
        {
            var files = await _sftp.ListNewFilesAsync(carrier.Code);
            foreach (var file in files)
            {
                using var stream = await _sftp.OpenAsync(carrier.Code, file);
                await _ingestion.IngestAsync(carrier.Code, stream);
                await _sftp.ArchiveAsync(carrier.Code, file);
            }
        }
    }
}

// Stub interfaces so the file compiles standalone — implementations are elsewhere.
public interface ICarrierSftpClient
{
    Task<IReadOnlyList<string>> ListNewFilesAsync(string carrierCode);
    Task<Stream> OpenAsync(string carrierCode, string fileName);

    /// <summary>
    /// Moves the file out of the carrier's inbox into the archive folder.
    /// Subsequent calls to <see cref="ListNewFilesAsync"/> will not return it.
    /// </summary>
    Task ArchiveAsync(string carrierCode, string fileName);
}
public interface ICarrierRegistry
{
    IEnumerable<(string Code, string Name)> GetActive();
}
