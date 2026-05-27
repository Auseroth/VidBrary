using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Models;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class TvShowsViewModel(
    VidBraryDbContext db,
    INavigationService navigation) : ViewModelBase
{
    private List<TvShowListItemViewModel> _allShows = [];

    [ObservableProperty] private ObservableCollection<TvShowListItemViewModel> _shows = [];
    [ObservableProperty] private TvShowListItemViewModel? _selectedShow;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedGenre = "All";
    [ObservableProperty] private string _selectedStatus = "All";
    [ObservableProperty] private string _selectedMatchFilter = "All";
    [ObservableProperty] private string _selectedSort = "Title A–Z";
    [ObservableProperty] private ObservableCollection<string> _availableGenres = ["All"];
    [ObservableProperty] private ObservableCollection<string> _availableStatuses = ["All"];
    [ObservableProperty] private ViewMode _viewMode = ViewMode.ComfortableGrid;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _filteredCount;

    public IReadOnlyList<string> MatchFilters { get; } =
        ["All", "Matched", "Action Needed", "No Match", "Unmatched"];

    public IReadOnlyList<string> SortOptions { get; } =
        ["Title A–Z", "Title Z–A", "Year ↑", "Year ↓", "Rating ↓", "Rating ↑", "Seasons ↓"];

    public IReadOnlyList<ViewMode> ViewModes { get; } = Enum.GetValues<ViewMode>().ToList();

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadShowsAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadShowsAsync();

    private async Task LoadShowsAsync()
    {
        IsBusy = true;

        var shows = await db.TvShows
            .Include(s => s.Genres).ThenInclude(g => g.Genre)
            .Include(s => s.Tags).ThenInclude(t => t.UserTag)
            .AsNoTracking()
            .ToListAsync();

        _allShows = shows.Select(TvShowListItemViewModel.FromShow).ToList();
        TotalCount = _allShows.Count;

        var genres = _allShows
            .SelectMany(s => s.Genres.Split(", ", StringSplitOptions.RemoveEmptyEntries))
            .Distinct().OrderBy(g => g).Prepend("All");
        AvailableGenres = new ObservableCollection<string>(genres);

        var statuses = _allShows
            .Where(s => s.Status is not null)
            .Select(s => s.Status!)
            .Distinct().OrderBy(s => s).Prepend("All");
        AvailableStatuses = new ObservableCollection<string>(statuses);

        ApplyFilters();
        IsBusy = false;
    }

    // Navigates when the user selects a row in Detail List view
    partial void OnSelectedShowChanged(TvShowListItemViewModel? value)
    {
        if (value is null) return;
        navigation.NavigateTo<TvShowDetailViewModel>(value.Id);
        SelectedShow = null; // reset so the same row can be re-selected
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedGenreChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusChanged(string value) => ApplyFilters();
    partial void OnSelectedMatchFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allShows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(s =>
                s.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Genres.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedGenre != "All")
            query = query.Where(s =>
                s.Genres.Contains(SelectedGenre, StringComparison.OrdinalIgnoreCase));

        if (SelectedStatus != "All")
            query = query.Where(s =>
                s.Status?.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase) == true);

        query = SelectedMatchFilter switch
        {
            "Matched"       => query.Where(s => s.MatchStatus is MatchStatus.AutoMatched
                                                              or MatchStatus.ManualMatched),
            "Action Needed" => query.Where(s => s.MatchStatus == MatchStatus.PendingSelection),
            "No Match"      => query.Where(s => s.MatchStatus == MatchStatus.NoResults),
            "Unmatched"     => query.Where(s => s.MatchStatus == MatchStatus.ManuallyUnmatched),
            _               => query
        };

        query = SelectedSort switch
        {
            "Title A–Z"  => query.OrderBy(s => s.Title),
            "Title Z–A"  => query.OrderByDescending(s => s.Title),
            "Year ↑"     => query.OrderBy(s => s.FirstAirYear ?? 0),
            "Year ↓"     => query.OrderByDescending(s => s.FirstAirYear ?? 0),
            "Rating ↓"   => query.OrderByDescending(s => s.TmdbRating ?? 0),
            "Rating ↑"   => query.OrderBy(s => s.TmdbRating ?? 0),
            "Seasons ↓"  => query.OrderByDescending(s => s.TotalSeasons ?? 0),
            _            => query.OrderBy(s => s.Title)
        };

        Shows = new ObservableCollection<TvShowListItemViewModel>(query);
        FilteredCount = Shows.Count;
    }

    [RelayCommand]
    private void OpenShow(TvShowListItemViewModel item) =>
        navigation.NavigateTo<TvShowDetailViewModel>(item.Id);

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SelectedGenre = "All";
        SelectedStatus = "All";
        SelectedMatchFilter = "All";
    }
}