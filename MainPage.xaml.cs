using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SdkComponentLab.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SdkComponentLab;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page
{
    private TableLabPage? _table;
    private ChartsLabPage? _charts;
    private readonly OverviewPage _overview = new();
    private readonly ApiInspectorPage _inspector = new();
    private bool _initializingLanguage = true;

    public MainPage()
    {
        InitializeComponent();
        Language = L.Language;
        LanguagePicker.SelectedIndex = L.Language == "ru-RU" ? 1 : 0;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(LanguagePicker, L.T("LanguageLabel"));
        ToolTipService.SetToolTip(LanguagePicker, L.T("LanguageReloadHint"));
        Navigation.SelectedItem = Navigation.MenuItems[0];
        _initializingLanguage = false;
    }

    internal void RestoreShell(string? tag, int themeIndex)
    {
        ThemePicker.SelectedIndex = themeIndex;
        Navigation.SelectedItem = Navigation.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(item => item.Tag?.ToString() == tag) ?? Navigation.MenuItems[0];
    }

    private void LanguagePicker_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_initializingLanguage) return;
        string language = LanguagePicker.SelectedIndex == 1 ? "ru-RU" : "en-US";
        if (language == L.Language) return;
        string? selectedTag = (Navigation.SelectedItem as NavigationViewItem)?.Tag?.ToString();
        int themeIndex = ThemePicker.SelectedIndex;
        L.SetLanguage(language);
        ((Application.Current as App)?.MainWindow as MainWindow)?.ReloadLanguage(selectedTag, themeIndex);
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (PageHost is null || args.SelectedItem is not NavigationViewItem item) return;
        try
        {
            PageTitle.Text = item.Content.ToString();
            switch (item.Tag?.ToString())
            {
                case "table": PageHost.Content = _table ??= new(); break;
                case "charts": PageHost.Content = _charts ??= new(); break;
                case "api":
                    _table ??= new();
                    _charts ??= new();
                    _inspector.SetTargets(_table.GetApiTargets().Concat(_charts.GetApiTargets()));
                    PageHost.Content = _inspector;
                    break;
                default: PageHost.Content = _overview; break;
            }
        }
        catch (Exception ex)
        {
            PageHost.Content = new TextBox { Text = L.F("MainPage_Navigation_SelectionChanged_001", (ex)), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        }
    }

    private void ThemePicker_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        var theme = ThemePicker.SelectedIndex switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
        RequestedTheme = theme;
        if ((Application.Current as App)?.MainWindow?.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }
}
