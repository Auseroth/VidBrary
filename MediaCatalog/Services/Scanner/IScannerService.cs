namespace VidBrary.Services.Scanner;

public interface IScannerService
{
    event EventHandler<string>? ProgressChanged;

    Task<ScanResult> ScanAllAsync(CancellationToken cancellationToken = default);
    Task<ScanResult> ScanDirectoryAsync(string path, bool isTvDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes any movies, shows, seasons, and episodes whose source files
    /// no longer exist on disk. Returns a summary of what was deleted.
    /// </summary>
    Task<ScanResult> PurgeStaleDataAsync(CancellationToken cancellationToken = default);
}