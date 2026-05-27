using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class MoviesPage : Page
{
    public MoviesPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MoviesViewModel>();
    }
}