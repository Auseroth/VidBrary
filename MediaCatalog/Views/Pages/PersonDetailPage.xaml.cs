using System.Windows.Controls;
using VidBrary.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace VidBrary.Views.Pages;

public partial class PersonDetailPage : Page
{
    public PersonDetailPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<PersonDetailViewModel>();
    }
}