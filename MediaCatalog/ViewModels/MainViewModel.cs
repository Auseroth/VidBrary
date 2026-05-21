using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaCatalog.Services.Navigation;
using MediaCatalog.Services.Settings;

namespace MediaCatalog.ViewModels;

public partial class MainViewModel(
    INavigationService navigationService,
    ISettingsService settingsService) : ObservableObject
{
    [ObservableProperty] private bool _canGoBack;

    [RelayCommand] private void NavigateHome()        => navigationService.NavigateTo<HomeViewModel>();
    [RelayCommand] private void NavigateMovies()      => navigationService.NavigateTo<MoviesViewModel>();
    [RelayCommand] private void NavigateTvShows()     => navigationService.NavigateTo<TvShowsViewModel>();
    [RelayCommand] private void NavigateCollections() => navigationService.NavigateTo<CollectionsViewModel>();
    [RelayCommand] private void NavigatePeople()      => navigationService.NavigateTo<PeopleViewModel>();
    [RelayCommand] private void NavigateTags()        => navigationService.NavigateTo<TagsViewModel>();
    [RelayCommand] private void NavigateSettings()    => navigationService.NavigateTo<SettingsViewModel>();
    [RelayCommand] private void NavigateProfiles()    => navigationService.NavigateTo<ProfilesViewModel>();

    [RelayCommand]
    private void GoBack()
    {
        navigationService.GoBack();
        CanGoBack = navigationService.CanGoBack;
    }
}