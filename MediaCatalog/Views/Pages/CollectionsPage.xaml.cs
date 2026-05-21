using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class CollectionsPage : Page
{
    public CollectionsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<CollectionsViewModel>();
    }
}