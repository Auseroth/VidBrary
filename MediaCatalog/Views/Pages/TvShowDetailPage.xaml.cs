using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class TvShowDetailPage : Page
{
    public TvShowDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TvShowDetailViewModel>();
    }
}