namespace VidBrary.Services.Scanner;

public interface IScannerService
{
    /// <summary>Fires on each significant scanner action.</summary>
    event EventHandler<string>? ProgressChanged;

    /// <summary>Full scan of all configured directories.</summary>
    Task<ScanResult> ScanAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Scan a single specific directory.</summary>
    Task<ScanResult> ScanDirectoryAsync(string path, bool isTvDirectory,
        CancellationToken cancellationToken = default);
}