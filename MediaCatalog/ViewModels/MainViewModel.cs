using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidBrary.Services.Navigation;
using VidBrary.Services.Settings;

namespace VidBrary.ViewModels;

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
    [RelayCommand] private void NavigateMyList()      => navigationService.NavigateTo<MyListViewModel>();

    [RelayCommand]
    private void GoBack()
    {
        navigationService.GoBack();
        CanGoBack = navigationService.CanGoBack;
    }
}