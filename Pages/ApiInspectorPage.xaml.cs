using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Charts;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace SdkComponentLab.Pages;

public sealed class ApiPropertyRow
{
    public required PropertyInfo Property { get; init; }
    public string Name => Property.Name;
    public required string Summary { get; init; }
}

public sealed partial class ApiInspectorPage : Page
{
    private List<KeyValuePair<string, object>> _targets = [];
    private readonly Dictionary<(object Target, string Property), object?> _original = [];
    private object? Target => TargetPicker.SelectedIndex >= 0 && TargetPicker.SelectedIndex < _targets.Count ? _targets[TargetPicker.SelectedIndex].Value : null;
    private ApiPropertyRow? Selected => PropertyList.SelectedItem as ApiPropertyRow;
    private static readonly string[] Namespaces = ["Microsoft.UI.Xaml.Controls.Tabular", "Microsoft.UI.Xaml.Controls.Charts"];

    public ApiInspectorPage() => InitializeComponent();

    public void SetTargets(IEnumerable<KeyValuePair<string, object>> targets)
    {
        var previousName = TargetPicker.SelectedItem as string;
        _targets = targets.ToList();
        // Retain originals only for objects still alive in the laboratories.
        foreach (var key in _original.Keys.Where(key => !_targets.Any(t => ReferenceEquals(t.Value, key.Target))).ToList()) _original.Remove(key);
        TargetPicker.ItemsSource = _targets.Select(t => t.Key).ToList();
        TargetPicker.SelectedIndex = Math.Max(0, _targets.FindIndex(t => t.Key == previousName));
        Rebuild();
    }

    private void TargetChanged(object sender, SelectionChangedEventArgs e) => Rebuild();
    private void SearchChanged(object sender, TextChangedEventArgs e) => Rebuild();
    private void FilterChanged(object sender, RoutedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (PropertyList is null || Target is not object target) return;
        var selectedName = Selected?.Name;
        var properties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
            .Where(p => Inherited.IsChecked == true || Namespaces.Contains(p.DeclaringType?.Namespace))
            .Where(p => p.Name.Contains(Search.Text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .Select(p => new ApiPropertyRow { Property = p, Summary = $"{TypeName(p.PropertyType)} · {(p.CanWrite ? "get / set" : L.T("ApiInspectorPage_Rebuild_001"))} · {ReadValue(target, p)}" })
            .ToList();
        PropertyList.ItemsSource = properties;
        PropertyList.SelectedItem = properties.FirstOrDefault(p => p.Name == selectedName) ?? properties.FirstOrDefault();
        Members.Text = DescribeMembers(target.GetType());
    }

    private void PropertyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is not ApiPropertyRow row || Target is not object target)
        {
            SelectedTitle.Text = L.T("ApiInspectorPage_PropertyChanged_002");
            SelectedDetails.Text = L.T("ApiInspectorPage_PropertyChanged_003");
            PropertyValue.Text = "";
            ApplyButton.IsEnabled = RestoreButton.IsEnabled = false;
            return;
        }
        var property = row.Property;
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        SelectedTitle.Text = property.Name;
        SelectedDetails.Text = L.F("ApiInspectorPage_PropertyChanged_004", (property.DeclaringType?.FullName), (TypeName(property.PropertyType)), (ReadValue(target, property)));
        PropertyValue.Text = ReadValue(target, property, false);
        var isEnum = type.IsEnum || type == typeof(bool);
        EnumValue.Visibility = isEnum ? Visibility.Visible : Visibility.Collapsed;
        PropertyValue.Visibility = isEnum ? Visibility.Collapsed : Visibility.Visible;
        if (isEnum)
        {
            EnumValue.ItemsSource = type == typeof(bool) ? new[] { "False", "True" } : Enum.GetNames(type);
            EnumValue.SelectedItem = ReadValue(target, property, false);
        }
        ApplyButton.IsEnabled = property.CanWrite && IsEditable(type);
        RestoreButton.IsEnabled = _original.ContainsKey((target, property.Name));
    }

    private static bool IsEditable(Type t) => t.IsEnum || t == typeof(string) || t == typeof(bool) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(DateTimeOffset) || t == typeof(GridLength) || t == typeof(Thickness) || t == typeof(CornerRadius) || t == typeof(Color) || typeof(Brush).IsAssignableFrom(t);

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (Target is not object target || Selected is not ApiPropertyRow row) return;
        try
        {
            var original = row.Property.GetValue(target);
            var input = EnumValue.Visibility == Visibility.Visible ? EnumValue.SelectedItem?.ToString() ?? "" : PropertyValue.Text;
            var converted = ConvertValue(input, row.Property.PropertyType);
            row.Property.SetValue(target, converted);
            _original.TryAdd((target, row.Name), original);
            ShowResult(L.F("ApiInspectorPage_Apply_Click_005", (row.Name), (ReadValue(target, row.Property))), false);
            Rebuild();
        }
        catch (Exception ex) { ShowResult(ex.GetBaseException().Message, true); }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (Target is not object target || Selected is not ApiPropertyRow row) return;
        try
        {
            if (_original.TryGetValue((target, row.Name), out var value))
            {
                row.Property.SetValue(target, value);
                _original.Remove((target, row.Name));
            }
            ShowResult(L.F("ApiInspectorPage_Restore_Click_006", (row.Name)), false);
            Rebuild();
        }
        catch (Exception ex) { ShowResult(ex.GetBaseException().Message, true); }
    }

    private object? ConvertValue(string value, Type declaredType)
    {
        var nullable = Nullable.GetUnderlyingType(declaredType);
        if (nullable is not null && string.IsNullOrWhiteSpace(value)) return null;
        var type = nullable ?? declaredType;
        if (typeof(Brush).IsAssignableFrom(type) && string.IsNullOrWhiteSpace(value)) return null;
        if (type == typeof(string)) return value;
        if (type.IsEnum) return Enum.Parse(type, value, true);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(GridLength))
        {
            if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;
            if (value.EndsWith('*')) return new GridLength(value.Length == 1 ? 1 : double.Parse(value[..^1], CultureInfo.InvariantCulture), GridUnitType.Star);
            return new GridLength(double.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == typeof(Thickness) || type == typeof(CornerRadius))
        {
            var parts = value.Split(',').Select(x => double.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
            if (parts.Length != 1 && parts.Length != 4) throw new FormatException(L.T("ApiInspectorPage_ConvertValue_007"));
            if (type == typeof(Thickness)) return parts.Length == 1 ? new Thickness(parts[0]) : new Thickness(parts[0], parts[1], parts[2], parts[3]);
            return parts.Length == 1 ? new CornerRadius(parts[0]) : new CornerRadius(parts[0], parts[1], parts[2], parts[3]);
        }
        if (type == typeof(Color) || typeof(Brush).IsAssignableFrom(type))
        {
            if (!value.StartsWith('#'))
            {
                if (type == typeof(Color)) throw new FormatException(L.T("ApiInspectorPage_ConvertValue_008"));
                return Resources.TryGetValue(value, out var resource) ? resource : Application.Current.Resources[value];
            }
            var hex = value[1..];
            if (hex.Length is not (6 or 8)) throw new FormatException(L.T("ApiInspectorPage_ConvertValue_009"));
            var argb = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (hex.Length == 6) argb |= 0xFF000000;
            var color = Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            return type == typeof(Color) ? color : new SolidColorBrush(color);
        }
        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    private static string ReadValue(object target, PropertyInfo p, bool truncate = true)
    {
        try
        {
            var value = p.GetValue(target);
            var result = value switch
            {
                null => "",
                SolidColorBrush brush => brush.Color.ToString(),
                DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                Thickness t => FormattableString.Invariant($"{t.Left},{t.Top},{t.Right},{t.Bottom}"),
                CornerRadius c => FormattableString.Invariant($"{c.TopLeft},{c.TopRight},{c.BottomRight},{c.BottomLeft}"),
                IFormattable format => format.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
            return truncate && result.Length > 160 ? result[..160] + "…" : result;
        }
        catch (Exception ex) { return L.F("ApiInspectorPage_ReadValue_010", (ex.GetBaseException().Message)); }
    }

    private static string TypeName(Type t) => Nullable.GetUnderlyingType(t) is Type nullable ? TypeName(nullable) + "?" : t.IsGenericType ? t.Name.Split('`')[0] + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">" : t.Name;

    private static string DescribeMembers(Type type)
    {
        var sb = new StringBuilder();
        foreach (var ctor in type.GetConstructors()) sb.AppendLine($"new {type.Name}({string.Join(", ", ctor.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name))})");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => !m.IsSpecialName && Namespaces.Contains(m.DeclaringType?.Namespace)).OrderBy(m => m.Name))
            sb.AppendLine($"{TypeName(method.ReturnType)} {method.Name}({string.Join(", ", method.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name))})");
        foreach (var evt in type.GetEvents().Where(e => Namespaces.Contains(e.DeclaringType?.Namespace)).OrderBy(e => e.Name)) sb.AppendLine($"event {evt.Name}: {TypeName(evt.EventHandlerType!)}");
        return sb.ToString();
    }

    private async void Catalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var assemblies = new[] { typeof(TableView).Assembly, typeof(Chart).Assembly }.Distinct();
            var types = assemblies.SelectMany(a => a.GetExportedTypes()).Where(t => Namespaces.Contains(t.Namespace)).OrderBy(t => t.FullName).ToList();
            var sb = new StringBuilder("Windows App SDK 2.5.4-experimental — public API\n\n");
            foreach (var type in types)
            {
                sb.AppendLine(type.FullName);
                if (type.IsEnum) sb.AppendLine(string.Join(", ", Enum.GetNames(type)));
                foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(p => p.Name))
                    sb.AppendLine($"  {TypeName(p.PropertyType)} {p.Name} {{ {(p.CanRead ? "get; " : "")}{(p.CanWrite ? "set; " : "")} }}");
                sb.AppendLine(DescribeMembers(type));
            }
            var text = new TextBox { Text = sb.ToString(), IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, Height = 480 };
            var dialog = new ContentDialog { Title = L.F("ApiInspectorPage_Catalog_Click_011", (types.Count)), Content = text, PrimaryButtonText = L.T("ApiInspectorPage_Catalog_Click_012"), CloseButtonText = L.T("ApiInspectorPage_Catalog_Click_013"), XamlRoot = XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var data = new DataPackage(); data.SetText(sb.ToString()); Clipboard.SetContent(data);
                ShowResult(L.F("ApiInspectorPage_Catalog_Click_014", (types.Count)), false);
            }
        }
        catch (Exception ex) { ShowResult(ex.GetBaseException().Message, true); }
    }

    private void ShowResult(string message, bool error)
    {
        Result.Message = message; Result.Severity = error ? InfoBarSeverity.Error : InfoBarSeverity.Success; Result.IsOpen = true;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(Result, message);
    }
}
