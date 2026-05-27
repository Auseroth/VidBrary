using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Data;
using VidBrary.Services.Navigation;
using VidBrary.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace VidBrary.ViewModels;

public partial class CollectionsViewModel(
    VidBraryDbContext db,
    INavigationService navigation) : ViewModelBase
{
    private List<CollectionListItemViewModel> _all = [];

    [ObservableProperty] private ObservableCollection<CollectionListItemViewModel> _collections = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedSource = "All";
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _filteredCount;

    // ── New collection creation ───────────────────────────────────────────────
    [ObservableProperty] private bool _showCreateDialog;
    [ObservableProperty] private string _newCollectionName = string.Empty;
    [ObservableProperty] private string? _createError;

    public IReadOnlyList<string> SourceFilters { get; } = ["All", "TMDB", "Custom"];

    public override async Task OnNavigatedToAsync(object? parameter = null) =>
        await LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;

        var collections = await db.Collections
            .Include(c => c.Movies)
            .AsNoTracking()
            .ToListAsync();

        _all = collections.Select(CollectionListItemViewModel.FromCollection).ToList();
        TotalCount = _all.Count;
        ApplyFilters();

        IsBusy = false;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedSourceChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        query = SelectedSource switch
        {
            "TMDB"   => query.Where(c => !c.IsUserCreated),
            "Custom" => query.Where(c => c.IsUserCreated),
            _        => query
        };

        Collections = new ObservableCollection<CollectionListItemViewModel>(
            query.OrderBy(c => c.Name));
        FilteredCount = Collections.Count;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenCollection(CollectionListItemViewModel item) =>
        navigation.NavigateTo<CollectionDetailViewModel>(item.Id);

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    // ── Create custom collection ──────────────────────────────────────────────

    [RelayCommand]
    private void OpenCreateDialog()
    {
        NewCollectionName = string.Empty;
        CreateError = null;
        ShowCreateDialog = true;
    }

    [RelayCommand]
    private void CloseCreateDialog() => ShowCreateDialog = false;

    [RelayCommand]
    private async Task CreateCollectionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName))
        {
            CreateError = "Please enter a collection name.";
            return;
        }

        var exists = await db.Collections
            .AnyAsync(c => c.Name == NewCollectionName.Trim());

        if (exists)
        {
            CreateError = "A collection with that name already exists.";
            return;
        }

        db.Collections.Add(new Models.MediaCollection
        {
            Name          = NewCollectionName.Trim(),
            IsUserCreated = true
        });
        await db.SaveChangesAsync();

        ShowCreateDialog = false;
        await LoadAsync();
    }
}