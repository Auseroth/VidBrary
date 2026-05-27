using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Services.MediaInfo;
using VidBrary.Services.Scanner;
using VidBrary.Services.Tmdb;
using VidBrary.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.ViewModels;

public partial class ScanViewModel(IServiceScopeFactory scopeFactory) : ViewModelBase
{
    [ObservableProperty] private string _scanStatus = "Ready";
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string? _lastScanSummary;

    private CancellationTokenSource? _cts;

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        _cts = new CancellationTokenSource();
        var progress = new Progress<string>(msg => ScanStatus = msg);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var scanner   = scope.ServiceProvider.GetRequiredService<IScannerService>();
            var tmdb      = scope.ServiceProvider.GetRequiredService<ITmdbService>();
            var mediaInfo = scope.ServiceProvider.GetRequiredService<IMediaInfoService>();

            // Phase 1 — discover files
            ScanStatus = "Scanning directories...";
            var result = await scanner.ScanAllAsync(_cts.Token);
            LastScanSummary = result.ToString();

            // Phase 2 — TMDB metadata
            ScanStatus = "Fetching metadata from TMDB...";
            await tmdb.EnrichAllMoviesAsync(progress, _cts.Token);
            await tmdb.EnrichAllShowsAsync(progress, _cts.Token);

            // Phase 3 — file technical info
            ScanStatus = "Reading file information...";
            await mediaInfo.EnrichAllAsync(progress, _cts.Token);

            ScanStatus = "Scan complete";
        }
        catch (OperationCanceledException)
        {
            ScanStatus = "Scan cancelled";
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            ScanStatus = "⚠ TMDB API key not set — files scanned, metadata skipped";
            LastScanSummary = "Add your TMDB API key in Settings to fetch metadata.";
        }
        catch (Exception ex)
        {
            ScanStatus = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelScan() => _cts?.Cancel();
}