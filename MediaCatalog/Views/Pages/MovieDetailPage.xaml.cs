using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class MovieDetailPage : Page
{
    public MovieDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MovieDetailViewModel>();
    }
}