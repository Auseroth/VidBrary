using System.Windows.Controls;
using VidBrary.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Services.Navigation;

public class NavigationService(IServiceProvider services) : INavigationService
{
    private Frame? _frame;
    private readonly Stack<(Type ViewModelType, object? Parameter)> _history = new();

    public event EventHandler? NavigationChanged;

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

        var viewType = ResolveViewType(viewModelType)
            ?? throw new InvalidOperationException(
                $"No View found for {viewModelType.Name}. Expected view name: " +
                $"{viewModelType.Name.Replace("ViewModel", "Page")} in VidBrary.Views.Pages");

        var page = (Page)Activator.CreateInstance(viewType)!;
        page.DataContext = viewModel;

        _frame.Navigate(page);
        _history.Push((viewModelType, parameter));

        _ = viewModel.OnNavigatedToAsync(parameter)
                     .ContinueWith(t =>
                     {
                         if (t.IsFaulted)
                             System.Diagnostics.Debug.WriteLine(
                                 $"Navigation load failed: {t.Exception?.GetBaseException().Message}");
                     }, TaskScheduler.Default);
        NavigationChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Type? ResolveViewType(Type viewModelType)
    {
        var pageName = viewModelType.Name.Replace("ViewModel", "Page");
        var fullViewName = $"VidBrary.Views.Pages.{pageName}";

        // Search the assembly that contains the views
        return typeof(NavigationService).Assembly.GetType(fullViewName);
    }

    public void GoBack()
    {
        if (!CanGoBack) return;
        _history.Pop();
        var (viewModelType, parameter) = _history.Peek();
        Navigate(viewModelType, parameter);
    }
}