using System.Windows.Controls;
using MediaCatalog.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCatalog.Views.Pages;

public partial class PersonDetailPage : Page
{
    public PersonDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PersonDetailViewModel>();
    }
}