using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Settings;
using VidBrary.Services.Tmdb;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class MovieDetailViewModel(
    VidBraryDbContext db,
    ITmdbService tmdb,
    ISettingsService settings) : ViewModelBase
{
    private int _movieId;

    // ── Core metadata ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _originalTitle;
    [ObservableProperty] private int? _year;
    [ObservableProperty] private string? _overview;
    [ObservableProperty] private double? _tmdbRating;
    [ObservableProperty] private int? _runtimeMinutes;
    [ObservableProperty] private string? _certification;
    [ObservableProperty] private string? _originalLanguage;
    [ObservableProperty] private string? _posterPath;
    [ObservableProperty] private string? _backdropPath;
    [ObservableProperty] private string _genres = string.Empty;
    [ObservableProperty] private string? _collectionName;
    [ObservableProperty] private int? _collectionId;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileSize = string.Empty;

    // ── Technical info ────────────────────────────────────────────────────────
    [ObservableProperty] private string? _videoCodec;
    [ObservableProperty] private string? _videoResolution;
    [ObservableProperty] private string? _audioCodec;
    [ObservableProperty] private string? _audioTracks;
    [ObservableProperty] private string? _subtitleTracks;

    // ── People ────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CastMemberViewModel> _cast = [];
    [ObservableProperty] private string _directors = string.Empty;
    [ObservableProperty] private string _writers = string.Empty;

    // ── Tags ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<TagViewModel> _tags = [];
    [ObservableProperty] private ObservableCollection<TagViewModel> _availableTags = [];
    [ObservableProperty] private bool _showTagPicker;

    // ── My List ───────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isInMyList;
    public string MyListButtonLabel => IsInMyList ? "✓ In My List" : "＋ My List";

    // ── Match status ──────────────────────────────────────────────────────────
    [ObservableProperty] private MatchStatus _matchStatus;
    [ObservableProperty] private int _candidateCount;
    [ObservableProperty] private bool _showMatchDialog;
    [ObservableProperty] private ObservableCollection<TmdbCandidateViewModel> _candidates = [];

    // ── Derived display ───────────────────────────────────────────────────────
    public string RuntimeDisplay => RuntimeMinutes.HasValue
        ? $"{RuntimeMinutes / 60}h {RuntimeMinutes % 60}m" : "—";

    public string RatingDisplay => TmdbRating.HasValue ? $"★ {TmdbRating:F1}" : "—";

    public string MatchButtonLabel => MatchStatus switch
    {
        MatchStatus.NoResults         => "No Results",
        MatchStatus.AutoMatched       => "Matched (1)",
        MatchStatus.ManualMatched     => $"Matched ({CandidateCount})",
        MatchStatus.PendingSelection  => $"Select Match ({CandidateCount} Results)",
        MatchStatus.ManuallyUnmatched => $"Unmatched ({CandidateCount} Available)",
        _                             => "Unknown"
    };

    public string MatchButtonColor => MatchStatus switch
    {
        MatchStatus.NoResults         => "#6c757d",
        MatchStatus.AutoMatched       => "#1a73e8",
        MatchStatus.ManualMatched     => "#1a73e8",
        MatchStatus.PendingSelection  => "#e94560",
        MatchStatus.ManuallyUnmatched => "#f59e0b",
        _                             => "#6c757d"
    };

    public bool MatchButtonEnabled => MatchStatus != MatchStatus.NoResults;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnNavigatedToAsync(object? parameter = null)
    {
        if (parameter is not int movieId) return;
        _movieId = movieId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;

        var movie = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .Include(m => m.Cast).ThenInclude(c => c.Person)
            .Include(m => m.Crew).ThenInclude(c => c.Person)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == _movieId);

        if (movie is null) { IsBusy = false; return; }

        Title           = movie.Title ?? movie.FileName;
        OriginalTitle   = movie.OriginalTitle;
        Year            = movie.Year;
        Overview        = movie.Overview ?? "No overview available.";
        TmdbRating      = movie.TmdbRating;
        RuntimeMinutes  = movie.RuntimeMinutes;
        Certification   = movie.CertificationRating;
        OriginalLanguage = movie.OriginalLanguage?.ToUpperInvariant();
        PosterPath      = movie.PosterPath;
        BackdropPath    = movie.BackdropPath;
        Genres          = string.Join(" · ", movie.Genres.Select(g => g.Genre.Name));
        CollectionName  = movie.Collection?.Name;
        CollectionId    = movie.CollectionId;
        FilePath        = movie.FilePath;
        FileSize        = FormatFileSize(movie.FileSizeBytes);
        VideoCodec      = movie.VideoCodec;
        VideoResolution = movie.VideoResolution;
        AudioCodec      = movie.AudioCodec;
        AudioTracks     = movie.AudioTracks;
        SubtitleTracks  = movie.SubtitleTracks;
        MatchStatus     = movie.MatchStatus;
        CandidateCount  = movie.TmdbCandidateCount;

        Cast = new ObservableCollection<CastMemberViewModel>(
            movie.Cast.OrderBy(c => c.Order).Take(20)
                 .Select(c => new CastMemberViewModel
                 {
                     PersonId  = c.PersonId,
                     Name      = c.Person.Name,
                     Character = c.Character ?? "—",
                     PhotoPath = c.Person.ProfilePath
                 }));

        Directors = string.Join(", ", movie.Crew
            .Where(c => c.Job == "Director").Select(c => c.Person.Name));
        Writers = string.Join(", ", movie.Crew
            .Where(c => c.Job is "Writer" or "Screenplay")
            .Select(c => c.Person.Name));

        Tags = new ObservableCollection<TagViewModel>(
            movie.Tags.Select(t => new TagViewModel
            {
                Id      = t.UserTag.Id,
                Name    = t.UserTag.Name,
                IconKey = t.UserTag.IconKey,
                Color   = t.UserTag.ColorHex ?? "#0f3460"
            }));

        // Load all tags for the picker
        var allTags = await db.Tags.AsNoTracking().ToListAsync();
        AvailableTags = new ObservableCollection<TagViewModel>(
            allTags.Select(t => new TagViewModel
            {
                Id      = t.Id,
                Name    = t.Name,
                IconKey = t.IconKey,
                Color   = t.ColorHex ?? "#0f3460"
            }));

        // Check My List state
        var profileId = settings.Current.ActiveProfileId;
        IsInMyList = await db.MyList
            .AnyAsync(m => m.UserProfileId == profileId && m.MovieId == _movieId);

        OnPropertyChanged(nameof(RuntimeDisplay));
        OnPropertyChanged(nameof(RatingDisplay));
        OnPropertyChanged(nameof(MatchButtonLabel));
        OnPropertyChanged(nameof(MatchButtonColor));
        OnPropertyChanged(nameof(MatchButtonEnabled));
        OnPropertyChanged(nameof(MyListButtonLabel));

        IsBusy = false;
    }

    // ── Play ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PlayAsync()
    {
        var player = settings.Current.DefaultMediaPlayerPath;
        var psi = string.IsNullOrWhiteSpace(player)
            ? new ProcessStartInfo(FilePath) { UseShellExecute = true }
            : new ProcessStartInfo(player, $"\"{FilePath}\"") { UseShellExecute = true };

        try
        {
            Process.Start(psi);

            // Log to watch history (last 10 kept per profile)
            var profileId = settings.Current.ActiveProfileId;
            db.WatchHistory.Add(new WatchHistory
            {
                UserProfileId = profileId,
                MovieId       = _movieId,
                WatchedAt     = DateTime.UtcNow,
                Completed     = false
            });
            await db.SaveChangesAsync();

            // Trim to last 10 play events for this profile (movies only, to keep it simple)
            var old = await db.WatchHistory
                .Where(w => w.UserProfileId == profileId && w.MovieId != null)
                .OrderByDescending(w => w.WatchedAt)
                .Skip(10)
                .ToListAsync();
            if (old.Count > 0)
            {
                db.WatchHistory.RemoveRange(old);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not launch player: {ex.Message}";
        }
    }

    // ── My List ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddToMyListAsync()
    {
        var profileId = settings.Current.ActiveProfileId;

        var existing = await db.MyList
            .FirstOrDefaultAsync(m => m.UserProfileId == profileId && m.MovieId == _movieId);

        if (existing is null)
        {
            db.MyList.Add(new MyListItem
            {
                UserProfileId = profileId,
                MovieId       = _movieId,
                AddedAt       = DateTime.UtcNow
            });
        }
        else
        {
            db.MyList.Remove(existing);
        }

        await db.SaveChangesAsync();
        IsInMyList = existing is null; // toggled
        OnPropertyChanged(nameof(MyListButtonLabel));
    }

    // ── Match dialog ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenMatchDialogAsync()
    {
        if (!MatchButtonEnabled) return;

        var raw = await db.TmdbCandidates
            .Where(c => c.MovieId == _movieId)
            .AsNoTracking()
            .ToListAsync();

        Candidates = new ObservableCollection<TmdbCandidateViewModel>(
            raw.Select(TmdbCandidateViewModel.FromCandidate));

        // Pre-select the current match if any
        if (MatchStatus is MatchStatus.AutoMatched or MatchStatus.ManualMatched)
        {
            var movie = await db.Movies.FindAsync(_movieId);
            var current = Candidates.FirstOrDefault(c => c.TmdbId == movie?.TmdbId);
            if (current is not null) current.IsSelected = true;
        }

        ShowMatchDialog = true;
    }

    [RelayCommand]
    private void ToggleCandidate(TmdbCandidateViewModel candidate)
    {
        if (!candidate.IsExpanded)
        {
            foreach (var c in Candidates) c.IsExpanded = false;
            candidate.IsExpanded = true;
        }
        else
        {
            var wasSelected = candidate.IsSelected;
            foreach (var c in Candidates) c.IsSelected = false;
            candidate.IsSelected = !wasSelected;
        }
    }

    [RelayCommand]
    private async Task SaveMatchAsync()
    {
        var selected = Candidates.FirstOrDefault(c => c.IsSelected);

        if (selected is null)
            await tmdb.ClearMovieMatchAsync(_movieId);
        else
            await tmdb.ApplyMovieMatchAsync(_movieId, selected.Id);

        ShowMatchDialog = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CloseMatchDialog() => ShowMatchDialog = false;

    // ── Tags ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTagPicker() => ShowTagPicker = !ShowTagPicker;

    [RelayCommand]
    private async Task ToggleTagAsync(TagViewModel tag)
    {
        var existing = await db.Set<MovieTag>()
            .FirstOrDefaultAsync(t => t.MovieId == _movieId && t.UserTagId == tag.Id);

        if (existing is null)
            db.Set<MovieTag>().Add(new MovieTag { MovieId = _movieId, UserTagId = tag.Id });
        else
            db.Set<MovieTag>().Remove(existing);

        await db.SaveChangesAsync();
        await LoadAsync();
    }

    // ── Collection nav ────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenCollection()
    {
        // Navigation to CollectionDetailViewModel wired up in the next step
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "—";
        double gb = bytes / 1_073_741_824.0;
        return gb >= 1 ? $"{gb:F2} GB" : $"{bytes / 1_048_576.0:F0} MB";
    }
}

public class CastMemberViewModel
{
    public int PersonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Character { get; init; } = string.Empty;
    public string? PhotoPath { get; init; }
}

public class TagViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IconKey { get; init; }
    public string Color { get; init; } = "#0f3460";
}