using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class MovieDetailPage : Page
{
    public MovieDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MovieDetailViewModel>();
    }
}