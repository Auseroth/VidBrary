using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Navigation;
using VidBrary.Services.Settings;
using VidBrary.ViewModels;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

public partial class HomeViewModel(
    VidBraryDbContext db,
    ISettingsService settingsService,
    INavigationService navigation,
    ScanViewModel scanViewModel) : ViewModelBase
{
    // ── Stats ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _totalMovies;
    [ObservableProperty] private int _totalShows;
    [ObservableProperty] private int _totalEpisodes;
    [ObservableProperty] private string _totalRuntime = "—";
    [ObservableProperty] private int _pendingMatches;

    // ── Continue Watching ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<RecentlyWatchedViewModel> _continueWatching = [];

    // ── Recently Added ────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<MovieListItemViewModel> _recentMovies = [];
    [ObservableProperty]
    private ObservableCollection<TvShowListItemViewModel> _recentShows = [];

    // ── My List spotlight ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<MyListSpotlightItem> _myListSpotlight = [];

    // ── Random picks ─────────────────────────────────────────────────────────
    [ObservableProperty] private MovieListItemViewModel? _randomMovie;
    [ObservableProperty] private bool _showRandomMovie;
    [ObservableProperty] private TvShowListItemViewModel? _randomShow;
    [ObservableProperty] private bool _showRandomShow;

    public ScanViewModel ScanViewModel => scanViewModel;

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;

        var profileId = settingsService.Current.ActiveProfileId;

        // Move all DB work off the UI thread in one shot
        var data = await Task.Run(async () =>
        {
            var totalMovies    = await db.Movies.CountAsync();
            var totalShows     = await db.TvShows.CountAsync();
            var totalEpisodes  = await db.Episodes.CountAsync();
            var pendingMatches = await db.Movies.CountAsync(m => m.MatchStatus == MatchStatus.PendingSelection)
                               + await db.TvShows.CountAsync(s => s.MatchStatus == MatchStatus.PendingSelection);
            var movieMinutes   = await db.Movies.SumAsync(m => (int?)m.RuntimeMinutes ?? 0);
            var epMinutes      = await db.Episodes.SumAsync(e => (int?)e.RuntimeMinutes ?? 0);

            var recentWatches = await db.WatchHistory
                .Where(w => w.UserProfileId == profileId && !w.Completed)
                .Include(w => w.Movie)
                .Include(w => w.Episode).ThenInclude(e => e!.Season).ThenInclude(s => s.TvShow)
                .OrderByDescending(w => w.WatchedAt)
                .Take(10).AsNoTracking().ToListAsync();

            var recentMovies = await db.Movies
                .Include(m => m.Genres).ThenInclude(g => g.Genre)
                .Include(m => m.Tags).ThenInclude(t => t.UserTag)
                .Include(m => m.Collection)
                .OrderByDescending(m => m.FileLastScanned)
                .Take(12).AsNoTracking().ToListAsync();

            var recentShows = await db.TvShows
                .Include(s => s.Genres).ThenInclude(g => g.Genre)
                .Include(s => s.Tags).ThenInclude(t => t.UserTag)
                .OrderByDescending(s => s.LastScanned)
                .Take(8).AsNoTracking().ToListAsync();

            var myListItems = await db.MyList
                .Where(m => m.UserProfileId == profileId)
                .Include(m => m.Movie)
                .Include(m => m.TvShow)
                .AsNoTracking().ToListAsync();

            return (totalMovies, totalShows, totalEpisodes, pendingMatches,
                    movieMinutes, epMinutes, recentWatches, recentMovies,
                    recentShows, myListItems);
        });

        // Back on UI thread — assign observable properties
        TotalMovies    = data.totalMovies;
        TotalShows     = data.totalShows;
        TotalEpisodes  = data.totalEpisodes;
        PendingMatches = data.pendingMatches;

        var span = TimeSpan.FromMinutes(data.movieMinutes + data.epMinutes);
        TotalRuntime = span.TotalDays >= 1
            ? $"{(int)span.TotalDays}d {span.Hours}h"
            : $"{span.Hours}h {span.Minutes}m";

        ContinueWatching = new ObservableCollection<RecentlyWatchedViewModel>(
            data.recentWatches
                .Where(w => w.Movie != null || w.Episode?.Season?.TvShow != null)
                .Select(w => new RecentlyWatchedViewModel(w)));

        // ── Recently Added ────────────────────────────────────────────────────
        var recentMovies = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .OrderByDescending(m => m.FileLastScanned)
            .Take(12)
            .AsNoTracking()
            .ToListAsync();
        RecentMovies = new ObservableCollection<MovieListItemViewModel>(
            recentMovies.Select(MovieListItemViewModel.FromMovie));

        var recentShows = await db.TvShows
            .Include(s => s.Genres).ThenInclude(g => g.Genre)
            .Include(s => s.Tags).ThenInclude(t => t.UserTag)
            .OrderByDescending(s => s.LastScanned)
            .Take(8)
            .AsNoTracking()
            .ToListAsync();
        RecentShows = new ObservableCollection<TvShowListItemViewModel>(
            recentShows.Select(TvShowListItemViewModel.FromShow));

        // ── My List spotlight ─────────────────────────────────────────────────
        var myListItems = await db.MyList
            .Where(m => m.UserProfileId == profileId)
            .Include(m => m.Movie)
            .Include(m => m.TvShow)
            .AsNoTracking()
            .ToListAsync();

        MyListSpotlight = new ObservableCollection<MyListSpotlightItem>(
            myListItems
                .OrderBy(_ => Random.Shared.Next())
                .Take(10)
                .Select(m => new MyListSpotlightItem(m)));

        ShowRandomMovie = false;
        ShowRandomShow  = false;
        IsBusy = false;
    }

    // ── Random picks ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickRandomMovieAsync()
    {
        var count = await db.Movies.CountAsync();
        if (count == 0) return;
        var movie = await db.Movies
            .Include(m => m.Genres).ThenInclude(g => g.Genre)
            .Include(m => m.Tags).ThenInclude(t => t.UserTag)
            .Include(m => m.Collection)
            .AsNoTracking()
            .Skip(Random.Shared.Next(count))
            .FirstOrDefaultAsync();
        if (movie is null) return;
        RandomMovie     = MovieListItemViewModel.FromMovie(movie);
        ShowRandomMovie = true;
    }

    [RelayCommand]
    private async Task PickRandomShowAsync()
    {
        var count = await db.TvShows.CountAsync();
        if (count == 0) return;
        var show = await db.TvShows
            .Include(s => s.Genres).ThenInclude(g => g.Genre)
            .Include(s => s.Tags).ThenInclude(t => t.UserTag)
            .AsNoTracking()
            .Skip(Random.Shared.Next(count))
            .FirstOrDefaultAsync();
        if (show is null) return;
        RandomShow     = TvShowListItemViewModel.FromShow(show);
        ShowRandomShow = true;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenMovie(MovieListItemViewModel item) =>
        navigation.NavigateTo<MovieDetailViewModel>(item.Id);

    [RelayCommand]
    private void OpenShow(TvShowListItemViewModel item) =>
        navigation.NavigateTo<TvShowDetailViewModel>(item.Id);

    [RelayCommand]
    private void OpenMyListItem(MyListSpotlightItem item)
    {
        if (item.MovieId.HasValue)
            navigation.NavigateTo<MovieDetailViewModel>(item.MovieId.Value);
        else if (item.TvShowId.HasValue)
            navigation.NavigateTo<TvShowDetailViewModel>(item.TvShowId.Value);
    }

    [RelayCommand]
    private void OpenWatchHistoryItem(RecentlyWatchedViewModel item)
    {
        if (item.MovieId.HasValue)
            navigation.NavigateTo<MovieDetailViewModel>(item.MovieId.Value);
        else if (item.TvShowId.HasValue)
            navigation.NavigateTo<TvShowDetailViewModel>(item.TvShowId.Value);
    }

    [RelayCommand]
    private void GoToMovies() => navigation.NavigateTo<MoviesViewModel>();

    [RelayCommand]
    private void GoToShows() => navigation.NavigateTo<TvShowsViewModel>();

    [RelayCommand]
    private void GoToMyList() => navigation.NavigateTo<MyListViewModel>();

    [RelayCommand]
    private void GoToActionNeeded() =>
        navigation.NavigateTo<MoviesViewModel>("filter:pending");
}