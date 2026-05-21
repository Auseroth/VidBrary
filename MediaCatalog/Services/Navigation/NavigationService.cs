using System.Windows.Controls;
using MediaCatalog.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Services.Navigation;

public class NavigationService(IServiceProvider services) : INavigationService
{
    private Frame? _frame;
    private readonly Stack<(Type ViewModelType, object? Parameter)> _history = new();

    /// <summary>Must be called once from MainWindow with the host Frame.</summary>
    public void SetFrame(Frame frame) => _frame = frame;

    public bool CanGoBack => _history.Count > 1;

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : notnull
    {
        Navigate(typeof(TViewModel), parameter);
    }

    private void Navigate(Type viewModelType, object? parameter)
    {
        if (_frame is null) return;

        // Resolve the ViewModel from DI
        var viewModel = (ViewModelBase)services.GetRequiredService(viewModelType);

        // Resolve the matching View by convention: MoviesViewModel → MoviesPage
        var viewTypeName = viewModelType.FullName!
            .Replace(".ViewModels.", ".Views.Pages.")
            .Replace("ViewModel", "Page");

        var viewType = Type.GetType(viewTypeName)
            ?? throw new InvalidOperationException(
                $"No View found for {viewModelType.Name}. Expected: {viewTypeName}");

        var page = (Page)Activator.CreateInstance(viewType)!;
        page.DataContext = viewModel;

        _frame.Navigate(page);
        _history.Push((viewModelType, parameter));

        _ = viewModel.OnNavigatedToAsync(parameter);
    }

    public void GoBack()
    {
        if (!CanGoBack) return;
        _history.Pop();
        var (viewModelType, parameter) = _history.Peek();
        Navigate(viewModelType, parameter);
    }
}