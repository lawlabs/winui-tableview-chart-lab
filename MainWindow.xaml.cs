using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SdkComponentLab;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = L.T("MainWindow_Window1/Title");
        Width = 1440;
        Height = 900;
        MinWidth = 760;
        MinHeight = 600;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        WindowRoot.Loaded += (_, _) => UpdateCaptionColors();
        WindowRoot.ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateCaptionColors);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    private void UpdateCaptionColors()
    {
        if (CaptionBrushProbe.Background is not Microsoft.UI.Xaml.Media.SolidColorBrush brush) return;
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonForegroundColor = brush.Color;
        titleBar.ButtonInactiveForegroundColor = brush.Color;
        titleBar.ButtonHoverForegroundColor = brush.Color;
        titleBar.ButtonPressedForegroundColor = brush.Color;
    }

    internal void ReloadLanguage(string? pageTag, int themeIndex)
    {
        Title = L.T("MainWindow_Window1/Title");
        var page = new MainPage();
        RootFrame.Content = page;
        page.RestoreShell(pageTag, themeIndex);
    }
}
