using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class TvShowsPage : Page
{
    public TvShowsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TvShowsViewModel>();
    }
}