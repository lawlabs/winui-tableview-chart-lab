# Windows App SDK 2.5.4-experimental: Chart

Исследовано 6 октября 2026 года. Релиз вышел 29 сентября 2026 года. Контролы имеют `[Experimental]` и контракт `Microsoft.UI.Xaml.Controls.Charts.WinUIChartingContract`, версия `131077` (2.5). Для API справочника Microsoft пока используется moniker `windows-app-sdk-2.0-experimental`, несмотря на пакет 2.5.4-experimental.

Проверены исходная [страница релиза](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental), страницы каждого типа и ключевых свойств, официальный [ChartApp sample Microsoft](https://github.com/microsoft/microsoft-ui-xaml/tree/main/Samples/ChartApp), содержимое NuGet Microsoft.WindowsAppSDK.WinUI 2.3.10-experimental (входит в Windows App SDK 2.5.4-experimental). Публичной развернутой обучающей статьи и полноценного Chart sample в WinUI Gallery на момент исследования не обнаружено. `winapp find-ui Chart` вернул только сторонний core pattern LiveCharts; лаборатория его не использует.

## Полный функциональный API inventory

Общие унаследованные свойства `DependencyObject`, `Control` и `FrameworkElement` не дублируются; их можно просмотреть в инспекторе приложения.

| Тип | Публичная функциональность | Где попробовать |
| --- | --- | --- |
| [Chart](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.chart?view=windows-app-sdk-2.0-experimental) | Коллекции `Axes`, `Data`, `Series`; `ShowLegend`, `LegendTitle` | Все сценарии; настройки легенды; добавление/удаление серий; инспектор |
| [CartesianSeries](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.cartesianseries?view=windows-app-sdk-2.0-experimental) | `Title`, `IsVisible`; `XValues`, `YValues`; `XAxis`, `YAxis`; `Stroke`, `StrokeThickness`, `StrokeDashStyle`; `ShowDataLabels`, `DataLabelBrush`, `DataLabelOverrides`; `ShowDataMarkers`, `DataMarkerBrush`, `MarkerShape`, `DataMarkerOverrides` | Выбор и редактор серии; переключение Y; overrides точек |
| [LineSeries](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.lineseries?view=windows-app-sdk-2.0-experimental) | Линейная серия; наследует всю функциональность CartesianSeries | Линии; числовая/дата X; поток |
| [AreaSeries](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.areaseries?view=windows-app-sdk-2.0-experimental) | Серия с площадью; добавляет `Fill` | Площадь; смешанные серии; цвет и прозрачность заливки |
| [BarSeries](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.barseries?view=windows-app-sdk-2.0-experimental) | Столбцы; `Fill`, `Orientation` | Столбцы; горизонтальные столбцы; смешанные серии |
| [Axis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.axis?view=windows-app-sdk-2.0-experimental) | Базовый тип: `Label`, `IsVisible`; protected constructor | Настройки каждой оси |
| [CartesianAxis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.cartesianaxis?view=windows-app-sdk-2.0-experimental) | `AxisLineBrush`, `GridLines`, `GridLineMajorBrush`, `GridLineMinorBrush`, `ShowTickLabels`, `ShowTickMarks`, `TickBrush`, `TickLabelBrush`; protected constructor | Ось X, основная и дополнительная Y |
| [CategoryAxis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.categoryaxis?view=windows-app-sdk-2.0-experimental) | Категории: `SortKey`, `SortOrder` | Категориальная X, сортировка по индексу/значению |
| [LinearAxis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.linearaxis?view=windows-app-sdk-2.0-experimental) | Числовая ось: nullable `double` `Minimum`, `Maximum`, `Spacing` | Обе Y; пустые поля возвращают auto/null; отдельная проверка отклонения LinearAxis на X |
| [DateTimeAxis](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.datetimeaxis?view=windows-app-sdk-2.0-experimental) | nullable `DateTimeOffset` `Minimum`, `Maximum`; `IntervalType`, `LabelFormat` | Даты; календарные границы; interval enum; строка формата |
| [Samples](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.samples?view=windows-app-sdk-2.0-experimental) | `ItemsSource : object`; отдельные наборы X и Y | Типизированные массивы из авторской `ObservableCollection<object>`; добавление, удаление, замена, редактирование, поток |
| [DataLabelOverride](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.datalabeloverride?view=windows-app-sdk-2.0-experimental) | Конструктор `(string text, Brush brush)`; read-only `Text`, `Brush` | Индивидуальная подпись точки |
| [DataMarkerOverride](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.datamarkeroverride?view=windows-app-sdk-2.0-experimental) | Конструктор `(MarkerShape shape, Brush brush)`; read-only `Shape`, `Brush` | Индивидуальный маркер точки |
| [XamlChartsResources](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.xamlchartsresources?view=windows-app-sdk-2.0-experimental) | `ResourceDictionary`, публичный конструктор | Ресурсы приложения |
| [XamlControlsChartsXamlMetaDataProvider](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.xamltypeinfo.xamlcontrolschartsxamlmetadataprovider?view=windows-app-sdk-2.0-experimental) | `IXamlMetadataProvider`: `GetXamlType(Type)`, `GetXamlType(string)`, `GetXmlnsDefinitions()` | Интеграция XAML; генерируемая цепочка metadata |
| [WinUIChartingContract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.winuichartingcontract?view=windows-app-sdk-2.0-experimental) | Версия WinRT API contract; не визуальный компонент | Справочник |

С каждым DP-свойством также публикуется статическое `...Property` для обычных XAML binding/styling и наблюдения через `RegisterPropertyChangedCallback`. `Chart.Axes`, `Data`, `Series` и словари overrides предоставляют коллекции только для чтения как свойства, но содержимое коллекций изменяемо.

## Все перечисления

| Enum | Значения |
| --- | --- |
| [BarOrientation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.barorientation?view=windows-app-sdk-2.0-experimental) | `Horizontal = 0`, `Vertical = 1` |
| [GridLines](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.gridlines?view=windows-app-sdk-2.0-experimental) | `None = 0`, `Major = 1`, `Minor = 2`. Атрибут Flags и отдельное значение Both не опубликованы. |
| [MarkerShape](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.markershape?view=windows-app-sdk-2.0-experimental) | `None = 0`, `Square = 1`, `Diamond = 2`, `Triangle = 3`, `X = 4`, `Asterisk = 5`, `ShortDash = 6`, `LongDash = 7`, `Circle = 8`, `Plus = 9` |
| [CategorySortKey](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.categorysortkey?view=windows-app-sdk-2.0-experimental) | `Index = 0`, `Value = 1` |
| [SortOrder](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.sortorder?view=windows-app-sdk-2.0-experimental) | `Ascending = 0`, `Descending = 1` |
| [StrokeDashStyle](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.strokedashstyle?view=windows-app-sdk-2.0-experimental) | `Solid = 0`, `Dash = 1`, `Dot = 2`, `DashDot = 3`, `DashDotDot = 4` |
| [DateTimeIntervalType](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.datetimeintervaltype?view=windows-app-sdk-2.0-experimental) | `Auto = 0`, `Day = 1`, `Week = 2`, `Month = 3`, `Year = 4` |

## Проверенные C# сигнатуры

Сигнатуры взяты непосредственно из C# blocks Microsoft Learn, не выведены по имени свойства:

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

Источники: [Chart.Series](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.chart.series?view=windows-app-sdk-2.0-experimental), [Chart.Axes](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.chart.axes?view=windows-app-sdk-2.0-experimental), [Chart.Data](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.chart.data?view=windows-app-sdk-2.0-experimental), [Samples.ItemsSource](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.samples.itemssource?view=windows-app-sdk-2.0-experimental), [DataLabelOverrides](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.cartesianseries.datalabeloverrides?view=windows-app-sdk-2.0-experimental), [DataMarkerOverrides](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.cartesianseries.datamarkeroverrides?view=windows-app-sdk-2.0-experimental), [LinearAxis.Minimum](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.linearaxis.minimum?view=windows-app-sdk-2.0-experimental), [DateTimeAxis.Minimum](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.charts.datetimeaxis.minimum?view=windows-app-sdk-2.0-experimental).

Использование, реализованное в лаборатории:

```csharp
var categories = new ObservableCollection<object> { "Москва", "Казань", "Пермь" };
var values = new ObservableCollection<object> { 42.0, 63.0, 51.0 };
var x = new Samples { ItemsSource = categories.Cast<string>().ToArray() };
var y = new Samples { ItemsSource = values.Cast<double>().ToArray() };
var xAxis = new CategoryAxis { Label = "Город", SortKey = CategorySortKey.Index };
var yAxis = new LinearAxis { Label = "Значение", Minimum = 0, Maximum = null, Spacing = null };
chart.Data.Add(x);
chart.Data.Add(y);
chart.Axes.Add(xAxis);
chart.Axes.Add(yAxis);
var line = new LineSeries
{
    Title = "Пример",
    XValues = x, YValues = y,
    XAxis = xAxis, YAxis = yAxis,
    ShowDataLabels = true,
    ShowDataMarkers = true,
    MarkerShape = MarkerShape.Circle,
    StrokeDashStyle = StrokeDashStyle.DashDot,
    StrokeThickness = 2
};
chart.Series.Add(line);
line.DataLabelOverrides[1] = new DataLabelOverride("Выбранная точка", brush);
line.DataMarkerOverrides[1] = new DataMarkerOverride(MarkerShape.Diamond, brush);
values[1] = 75.0; // изменение авторской коллекции
y.ItemsSource = values.Cast<double>().ToArray(); // передать обновлённые данные в Chart
```

## Подключение и ограничения

Дополнительно проверено на работающем приложении 7 октября 2026: назначение `LinearAxis` в `LineSeries.XAxis` возвращает `ArgumentException` с HRESULT `0x80070057` и сообщением `The axis scale is incompatible with the series data slot.` Проверены пустой и заполненный `double[]`, оба порядка назначения осей и источников. Это наблюдение о конкретной сборке SDK, а не вывод из сигнатуры `CartesianAxis`. Лаборатория сохраняет рабочий график при отдельной проверке ограничения. Сценарии с числами X используют `CategoryAxis` и строковые представления чисел: это равномерные категории, без непрерывной числовой шкалы. `LinearAxis` на Y и `DateTimeAxis` на X работают. Даты демонстрации заданы полуночью UTC, чтобы календарные подписи соответствовали редактору.

При изменении типа оси, серии или темы открытые секции параметров сохраняются. Числовые поля автоматизации имеют отдельные идентификаторы `...MinValue`, `...MaxValue`, `...SpacingValue`, чтобы исключить частичное совпадение с `...MinorBrush`.

Ещё одно наблюдение визуального QA: `SolidColorBrush.Opacity = 0.5` не меняет заливку Area в этой сборке, а alpha в `SolidColorBrush.Color` работает (проверено через инспектор с `#80FF8800`). Поэтому лаборатория кодирует выбранную непрозрачность в `Color.A`, оставляя `Brush.Opacity = 1`. Тематические кисти с собственной Opacity также преобразуются в ARGB для нативного рендера; это сохраняет различие основных и второстепенных линий сетки. Поле приложения показывает выбранный коэффициент заливки. В инспекторе можно отдельно проверить исходный `Brush.Opacity` и цвет `#AARRGGBB`.

В проекте используется `Microsoft.WindowsAppSDK` `2.5.4-experimental`. Пакет WinUI доставляет нативные `Microsoft.UI.Xaml.Controls.Charts.dll` и `.pri`, C# projection находится в `Microsoft.WinUI.dll`, WinMD — в `Microsoft.UI.Xaml.winmd`. Namespace XAML: `using:Microsoft.UI.Xaml.Controls.Charts`.

Официальный [App.xaml sample](https://github.com/microsoft/microsoft-ui-xaml/blob/main/Samples/ChartApp/ChartAppCsUnpackaged/App.xaml) подключает `XamlControlsResources` и создаёт Chart из XAML и кода. Отдельно опубликованы `XamlChartsResources` и `XamlControlsChartsXamlMetaDataProvider`; для явной интеграции компонентных словарей и metadata используются эти типы. Проект лаборатории управляет ресурсами централизованно в App.

В объявленном публичном API этого релиза обнаружены только Line, Area, Bar и Cartesian axes. Pie/Donut, Scatter, stacking, встроенные zoom/pan, point-click события, tooltip-конфигуратор и отдельный control легенды не объявлены. Не приписывать их SDK. Легенда, подписи и маркеры есть; редактор точек и поток лаборатории являются сценариями приложения через `Samples.ItemsSource`.

При runtime QA прямое назначение пустой `ObservableCollection<object>` с последующим добавлением элементов не показало линии, хотя приложение имело семь точек. Поэтому приложение передаёт типизированные `string[]`, `double[]`, `DateTimeOffset[]` после изменений. Это обход наблюдаемого поведения экспериментальной версии; справочник пока не документирует контракт отслеживания managed collection и не даёт гарантии для всех `ItemsSource` типов.

В API reference большинство описаний новых свойств пока пустые. Сигнатуры и набор типов подтверждены официальным справочником; форматирование дат, реакция на динамические источники и visual behavior требуют запуска приложения, а не выводятся только из имён API. Запуск и проверку выполняет общий build/run workflow лаборатории. `RunSmokeTests()` синхронно проверяет создание и изменение API во всех десяти сценариях, но не ждёт отдельной отрисовки каждого промежуточного сценария; визуальная проверка производится отдельно.

Цвета серии и осей выбираются из семантических WinUI resources: `AccentFillColorDefaultBrush`, `SystemFillColorSuccessBrush`, `SystemFillColorCautionBrush`, `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`. Ключи проверены в [исходном словаре WinUI](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/CommonStyles/Common_themeresources_any.xaml). Невидимые `Border` в дереве страницы разрешают эти ключи через XAML `{ThemeResource}`. Серии и оси наследуют `DependencyObject`, а новый `SetThemeResourceBinding` является методом `FrameworkElement`; напрямую применить его к серии нельзя. Лаборатория отслеживает изменения `Background` у Border и обновляет ссылки на кисти осей и серий после разрешения новой темы, сохраняя выбранный ресурс и коэффициент Fill. Одного `ActualThemeChanged` было недостаточно: проверка получала старую чёрную кисть на тёмном фоне. Итоговая проверка подтверждает белый ARGB цвет подписей в реальной оси и читаемую отрисовку. Новый `SetThemeResourceBinding` отдельно используется на `Chart.Foreground` и в обзоре SDK.

UI Automation peer `NativeChart` в этой сборке имеет тип `NamedContainerAutomationPeer` и возвращает нулевой прямоугольник / `IsOffscreen=true`, хотя график виден. Проверка компактного окна подтверждает наличие объекта и геометрию ScrollViewer; отрисовка самого Chart проверяется по PNG. Нельзя выводить отсутствие графика из нулевых координат этого peer.
