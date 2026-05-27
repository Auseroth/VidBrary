using System.Windows.Controls;
using System.Windows.Input;

namespace VidBrary.Views.Pages;

public partial class HomePage : Page
{
    public HomePage() => InitializeComponent();

    // Passes vertical mouse wheel scroll through horizontal strip ScrollViewers
    // so the page keeps scrolling when the cursor is over a horizontal list.
    private void HorizontalStrip_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta == 0) return;
        PageScroller.ScrollToVerticalOffset(PageScroller.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}