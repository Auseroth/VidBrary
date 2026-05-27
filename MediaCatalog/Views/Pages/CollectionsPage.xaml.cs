using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class CollectionsPage : Page
{
    public CollectionsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<CollectionsViewModel>();
    }
}