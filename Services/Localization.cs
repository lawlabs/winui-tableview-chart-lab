global using SdkComponentLab.Services;
using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace SdkComponentLab.Services;

/// <summary>Shared MRT Core string resources for XAML and code. English is the first-run default.</summary>
internal static class L
{
    private const string SettingKey = "DisplayLanguage";
    private static ResourceLoader? _loader;
    public static string Language { get; private set; } = "en-US";

    public static void Initialize()
    {
        string? saved = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
        SetLanguage(saved == "ru-RU" ? "ru-RU" : "en-US");
    }

    public static void SetLanguage(string language)
    {
        Language = language == "ru-RU" ? "ru-RU" : "en-US";
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Language;
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Language;
        var culture = CultureInfo.GetCultureInfo(Language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        ApplicationData.Current.LocalSettings.Values[SettingKey] = Language;
        _loader = new ResourceLoader();
    }

    public static string T(string key)
    {
        string value = (_loader ??= new ResourceLoader()).GetString(key);
        return string.IsNullOrEmpty(value) ? throw new InvalidOperationException($"Missing localized resource: {key}") : value;
    }

    public static string F(string key, params object?[] arguments) => string.Format(CultureInfo.CurrentCulture, T(key), arguments);
}
