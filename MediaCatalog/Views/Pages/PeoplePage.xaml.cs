using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class PeoplePage : Page
{
    public PeoplePage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PeopleViewModel>();
    }
}