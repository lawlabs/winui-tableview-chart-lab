# Native WinUI Chart: API and experiments

Baseline: Windows App SDK **2.5.4-experimental**. Research checked Microsoft Learn's C# declarations, official samples, installed metadata, and runtime behavior on October 7, 2026. Most new reference descriptions are still brief; signatures alone do not establish rendering behavior.

## Official documentation

- [Release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)
- [Chart API namespace](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts?view=windows-app-sdk-2.0-experimental)
- [Official Chart samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/ChartApp)
- [Chart sample App.xaml](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/ChartApp/ChartAppCsUnpackaged/App.xaml)

The XAML namespace is `using:Microsoft.UI.Xaml.Controls.Charts`. The package supplies native Chart DLL/PRI resources and the C# projection in Microsoft.WinUI.dll. This app uses self-contained Windows App SDK deployment.

## Complete Chart type inventory

| Type | Public functionality | Try in the lab |
| --- | --- | --- |
| Chart | Axes, Data, Series; ShowLegend, LegendTitle | All ten scenarios; legend; add/remove series |
| CartesianSeries | Title, IsVisible; XValues, YValues; XAxis, YAxis; Stroke, StrokeThickness, StrokeDashStyle; ShowDataLabels, DataLabelBrush, DataLabelOverrides; ShowDataMarkers, DataMarkerBrush, MarkerShape, DataMarkerOverrides | Series editor and per-point overrides |
| LineSeries | CartesianSeries line rendering | Lines, stream, dates |
| AreaSeries | Adds Fill | Areas, mixed series, opacity |
| BarSeries | Adds Fill, Orientation | Vertical/horizontal bars and mixed series |
| Axis | Label, IsVisible; protected constructor | Each axis section |
| CartesianAxis | AxisLineBrush, GridLines, GridLineMajorBrush, GridLineMinorBrush, ShowTickLabels, ShowTickMarks, TickBrush, TickLabelBrush | X and both Y axes |
| CategoryAxis | SortKey, SortOrder | Categorical X sorting |
| LinearAxis | Nullable double Minimum, Maximum, Spacing | Both Y axes; dedicated X rejection probe |
| DateTimeAxis | Nullable DateTimeOffset Minimum, Maximum; IntervalType, LabelFormat | Date scenario, ISO editor, calendar range |
| Samples | ItemsSource : object | Typed sources, point editing, CRUD, stream |
| DataLabelOverride | Constructor(string text, Brush brush); read-only Text, Brush | Individual point label |
| DataMarkerOverride | Constructor(MarkerShape shape, Brush brush); read-only Shape, Brush | Individual point marker |
| XamlChartsResources | ResourceDictionary; public constructor | Resource integration |
| XamlControlsChartsXamlMetaDataProvider | IXamlMetadataProvider: GetXamlType(Type/string), GetXmlnsDefinitions() | Generated XAML integration |
| WinUIChartingContract | WinRT contract version | API reference |

Each dependency property also exposes a static `...Property` identifier. Chart's collection properties and override maps are read-only properties with mutable contents. The API inspector lists inherited properties separately and edits supported simple values on live objects; collections are manipulated through lab commands.

Individual official references: [Chart](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.chart?view=windows-app-sdk-2.0-experimental), [CartesianSeries](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.cartesianseries?view=windows-app-sdk-2.0-experimental), [Axis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.axis?view=windows-app-sdk-2.0-experimental), [Samples](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.samples?view=windows-app-sdk-2.0-experimental).

## All enum values

| Enum | Values |
| --- | --- |
| BarOrientation | Horizontal=0, Vertical=1 |
| GridLines | None=0, Major=1, Minor=2; no declared Flags/Both |
| MarkerShape | None=0, Square=1, Diamond=2, Triangle=3, X=4, Asterisk=5, ShortDash=6, LongDash=7, Circle=8, Plus=9 |
| CategorySortKey | Index=0, Value=1 |
| SortOrder | Ascending=0, Descending=1 |
| StrokeDashStyle | Solid=0, Dash=1, Dot=2, DashDot=3, DashDotDot=4 |
| DateTimeIntervalType | Auto=0, Day=1, Week=2, Month=3, Year=4 |

## Binding data to a native Chart

```csharp
var x = new Samples { ItemsSource = new[] { "London", "Paris", "Berlin" } };
var y = new Samples { ItemsSource = new[] { 42.0, 63.0, 51.0 } };
var xAxis = new CategoryAxis { Label = "City", SortKey = CategorySortKey.Index };
var yAxis = new LinearAxis { Label = "Value", Minimum = 0 };
chart.Data.Add(x);
chart.Data.Add(y);
chart.Axes.Add(xAxis);
chart.Axes.Add(yAxis);
var line = new LineSeries
{
    Title = "Observed", XValues = x, YValues = y,
    XAxis = xAxis, YAxis = yAxis,
    ShowDataLabels = true, ShowDataMarkers = true,
    MarkerShape = MarkerShape.Circle,
    StrokeDashStyle = StrokeDashStyle.DashDot, StrokeThickness = 2
};
chart.Series.Add(line);
line.DataLabelOverrides[1] = new DataLabelOverride("Selected", brush);
line.DataMarkerOverrides[1] = new DataMarkerOverride(MarkerShape.Diamond, brush);
y.ItemsSource = new[] { 42.0, 75.0, 51.0 }; // assign an updated typed source
```

The app owns editable/observable data and assigns fresh `string[]`, `double[]`, or `DateTimeOffset[]` to Samples after changes. Direct assignment of ObservableCollection<object> followed by additions did not draw the expected lines in the tested build. The sample demonstrates source replacement; it does not claim native observation of every managed collection type. Streaming is an app timer with a rolling 30-point window.

## Axis compatibility observation

Assigning `new LinearAxis()` to `LineSeries.XAxis` returned ArgumentException `0x80070057`: `The axis scale is incompatible with the series data slot.` Tested with populated double[] and different assignment orders. The dedicated probe preserves the current graph.

Numeric X scenarios therefore use **string categories with equal spacing**, visibly explained in the app. They are not continuous numeric X plots. LinearAxis on Y and DateTimeAxis on X work. Sample dates use UTC midnight; editors accept ISO dates with explicit offsets. Empty linear range fields restore automatic/null bounds.

## Appearance and theme behavior

The sample offers five semantic WinUI brushes and a vivid blue/coral/violet/mint/amber palette. Series identity is also conveyed by legends and marker shapes. Label/axis defaults use semantic text brushes.

Observed native rendering ignored SolidColorBrush.Opacity for Area fill, but honored alpha in Color. The app converts the selected fill factor to Color.A with Brush.Opacity=1; tests verify transparent/half/opaque ARGB through the live inspector and screenshots. Semantic brush opacity is also normalized so major/minor grid colors remain distinct.

Series and axes inherit DependencyObject, while the new SetThemeResourceBinding belongs to FrameworkElement. Hidden theme-probe Borders resolve resources through XAML ThemeResource, and Background property changes trigger brush refreshes after theme resolution. ActualThemeChanged alone could read an old black tick brush in Dark. The current check confirms white tick-label ARGB on a dark chart while preserving fill opacity. Chart.Foreground and the SDK overview separately demonstrate SetThemeResourceBinding. [WinUI theme resources](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/CommonStyles/Common_themeresources_any.xaml).

## Experiments and boundaries

The ten scenarios cover Line, Area, Bar, mixed series, numeric categories, dates, two Y axes, horizontal bars, empty data, and 200 points. Settings expose every series and axis property above, per-point overrides, visible/hidden series, label/marker brushes, and data source changes. Open settings sections remain open across series/axis changes.

The published API declares no Pie/Donut, Scatter, stacking, built-in zoom/pan, point-click event, tooltip configurator, or standalone legend control. The lab's editor and stream are application behavior built on Samples, not additional SDK controls.

In this build, Chart's NamedContainerAutomationPeer reports zero bounds and IsOffscreen=true while the graph is visibly rendered. UI tests assert object presence and workspace geometry; PNG review verifies actual rendering. Synchronous API smoke tests iterate configurations but do not wait for each intermediate frame.

## Verified C# signatures

```csharp
// Chart
IObservableVector<Axis> Axes { get; }
IObservableVector<Samples> Data { get; }
IObservableVector<CartesianSeries> Series { get; }

// CartesianSeries
Samples XValues { get; set; }
Samples YValues { get; set; }
CartesianAxis XAxis { get; set; }
CartesianAxis YAxis { get; set; }
IObservableMap<uint, DataLabelOverride> DataLabelOverrides { get; }
IObservableMap<uint, DataMarkerOverride> DataMarkerOverrides { get; }

// Samples
object ItemsSource { get; set; }

// LinearAxis
double? Minimum { get; set; }
double? Maximum { get; set; }
double? Spacing { get; set; }

// DateTimeAxis
DateTimeOffset? Minimum { get; set; }
DateTimeOffset? Maximum { get; set; }
string LabelFormat { get; set; }
```

[Russian research notes](ru/chart-research.md).
