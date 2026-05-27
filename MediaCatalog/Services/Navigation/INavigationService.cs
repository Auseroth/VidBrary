namespace VidBrary.Services.Navigation;

public interface INavigationService
{
    /// <summary>Navigate to a page by its ViewModel type.</summary>
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : notnull;

    /// <summary>Navigate back if history exists.</summary>
    void GoBack();

    bool CanGoBack { get; }
}