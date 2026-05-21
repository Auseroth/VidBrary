using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class MoviesPage : Page
{
    public MoviesPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MoviesViewModel>();
    }
}