using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaCatalog.Data;
using MediaCatalog.Services.Navigation;
using MediaCatalog.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace MediaCatalog.ViewModels;

public partial class PeopleViewModel(
    MediaCatalogDbContext db,
    INavigationService navigation) : ViewModelBase
{
    private List<PersonListItemViewModel> _all = [];

    [ObservableProperty] private ObservableCollection<PersonListItemViewModel> _people = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _filteredCount;

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;

        var people = await db.People
            .Include(p => p.MovieCast)
            .Include(p => p.MovieCrew)
            .Include(p => p.TvShowCast)
            .AsNoTracking()
            .ToListAsync();

        _all = people
            .Select(p => new PersonListItemViewModel
            {
                Id          = p.Id,
                Name        = p.Name,
                PhotoPath   = p.ProfilePath,
                MovieCount  = p.MovieCast.Count + p.MovieCrew.Count,
                ShowCount   = p.TvShowCast.Count
            })
            .OrderBy(p => p.Name)
            .ToList();

        TotalCount = _all.Count;
        ApplyFilter();
        IsBusy = false;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = _all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        People = new ObservableCollection<PersonListItemViewModel>(query);
        FilteredCount = People.Count;
    }

    [RelayCommand]
    private void OpenPerson(PersonListItemViewModel item) =>
        navigation.NavigateTo<PersonDetailViewModel>(item.Id);

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;
}

public class PersonListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PhotoPath { get; init; }
    public int MovieCount { get; init; }
    public int ShowCount { get; init; }

    public string CreditsSummary =>
        (MovieCount, ShowCount) switch
        {
            (> 0, > 0) => $"{MovieCount} movie{(MovieCount != 1 ? "s" : "")}, {ShowCount} show{(ShowCount != 1 ? "s" : "")}",
            (> 0, 0)   => $"{MovieCount} movie{(MovieCount != 1 ? "s" : "")}",
            (0, > 0)   => $"{ShowCount} show{(ShowCount != 1 ? "s" : "")}",
            _          => "No credits"
        };
}