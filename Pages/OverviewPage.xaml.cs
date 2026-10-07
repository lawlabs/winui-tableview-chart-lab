using Microsoft.UI.Xaml.Controls;

namespace SdkComponentLab.Pages;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        InitializeComponent();
        ThemeDemo.SetThemeResourceBinding(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
    }
}
