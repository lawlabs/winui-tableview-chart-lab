using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Charts;
using Microsoft.UI.Xaml.Media;

namespace SdkComponentLab.Pages;

public sealed partial class ChartsLabPage : Page
{
    private readonly Random _random = new(254);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private readonly ObservableCollection<object> _x = [];
    private readonly List<ObservableCollection<object>> _ys = [];
    private readonly List<Samples> _ySamples = [];
    private readonly string[] _presets = [L.T("ChartsLabPage__presets_001"), L.T("ChartsLabPage__presets_002"), L.T("ChartsLabPage__presets_003"), L.T("ChartsLabPage__presets_004"), L.T("ChartsLabPage__presets_005"), L.T("ChartsLabPage__presets_006"), L.T("ChartsLabPage__presets_007"), L.T("ChartsLabPage__presets_008"), L.T("ChartsLabPage__presets_009"), L.T("ChartsLabPage__presets_010")];
    private static string NumericXAxisNote => L.T("ChartsLabPage_NumericXAxisNote_011");
    private readonly string[] _brushKeys = ["AccentFillColorDefaultBrush", "SystemFillColorSuccessBrush", "SystemFillColorCautionBrush", "TextFillColorPrimaryBrush", "TextFillColorSecondaryBrush", "LabChartBlueBrush", "LabChartCoralBrush", "LabChartVioletBrush", "LabChartMintBrush", "LabChartAmberBrush"];
    private readonly List<Border> _brushProbes = [];
    private readonly Dictionary<Brush, int> _knownBrushes = [];
    private readonly Dictionary<int, SolidColorBrush> _nativeBrushes = [];
    private readonly Dictionary<Brush, double> _fillOpacities = [];
    private readonly Dictionary<string, bool> _sectionExpansion = [];
    private Samples _xSamples = null!;
    private CartesianAxis _xAxis = null!;
    private LinearAxis _yAxis = null!;
    private LinearAxis _secondaryY = null!;
    private bool _loading;
    private int _selectedSeries;
    private int _axisKind;
    private int _counter;
    private double _fillOpacity = 0.28;
    private bool _pointLabels;
    private bool _pointMarkers;
    private NumberBox _pointIndex = null!;
    private TextBox _pointX = null!;
    private NumberBox _pointY = null!;
    private TextBox _overrideText = null!;
    private ComboBox _overrideShape = null!;
    private ComboBox _overrideLabelBrush = null!;
    private ComboBox _overrideMarkerBrush = null!;
    private string _diagnosticStage = "construction";

    public ChartsLabPage()
    {
        try
        {
        DiagnosticStage("InitializeComponent / markup Chart");
        InitializeComponent();
        Plot.SetThemeResourceBinding(Control.ForegroundProperty, "TextFillColorPrimaryBrush");
        _brushProbes.AddRange([AccentBrushProbe, SuccessBrushProbe, CautionBrushProbe, PrimaryBrushProbe, SecondaryBrushProbe, BlueBrushProbe, CoralBrushProbe, VioletBrushProbe, MintBrushProbe, AmberBrushProbe]);
        foreach (var probe in _brushProbes)
            probe.RegisterPropertyChangedCallback(Border.BackgroundProperty, (_, _) =>
                DispatcherQueue.TryEnqueue(() => { if (_xAxis is not null) { RefreshThemeBrushes(); RefreshSettings(); } }));
        DiagnosticStage("preset ComboBox initialization");
        _loading = true;
        PresetPicker.ItemsSource = _presets;
        PresetPicker.SelectedIndex = 0;
        _loading = false;
        _timer.Tick += (_, _) =>
        {
            try { MutateData(true); }
            catch (Exception ex) { _timer.Stop(); StreamButton.IsChecked = false; SetStatus(L.T("ChartsLabPage_Text_012") + ex.Message, InfoBarSeverity.Error); }
        };
        Unloaded += (_, _) => { _timer.Stop(); StreamButton.IsChecked = false; };
        Loaded += (_, _) => { RefreshThemeBrushes(); RefreshSettings(); UpdateValues(); };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { RefreshThemeBrushes(); RefreshSettings(); });
        ApplyPreset(0);
        DiagnosticStage("constructor complete");
        }
        catch (Exception ex)
        {
            string message = $"ChartsLab failed at {_diagnosticStage}: {ex.Message}";
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SdkComponentLab-chart.log"), message + "\n" + ex + "\n");
            throw new InvalidOperationException(message, ex);
        }
    }

    private void DiagnosticStage(string stage)
    {
        _diagnosticStage = stage;
        System.Diagnostics.Debug.WriteLine("ChartsLab stage: " + stage);
    }

    public IEnumerable<KeyValuePair<string, object>> GetApiTargets()
    {
        yield return new("Chart", Plot);
        for (int i = 0; i < Plot.Axes.Count; i++) yield return new($"Axis [{i}]", Plot.Axes[i]);
        for (int i = 0; i < Plot.Series.Count; i++)
        {
            var series = Plot.Series[i];
            yield return new($"Series [{i}] {series.GetType().Name}", series);
            foreach (var item in series.DataLabelOverrides) yield return new($"Label [{i}:{item.Key}]", item.Value);
            foreach (var item in series.DataMarkerOverrides) yield return new($"Marker [{i}:{item.Key}]", item.Value);
        }
        for (int i = 0; i < Plot.Data.Count; i++) yield return new($"Samples [{i}]", Plot.Data[i]);
    }

    public void Reset() => ApplyPreset(Math.Max(PresetPicker.SelectedIndex, 0));

    private CartesianSeries? SelectedSeries => Plot.Series.Count == 0 ? null : Plot.Series[Math.Clamp(_selectedSeries, 0, Plot.Series.Count - 1)];

    public string RunSmokeTests()
    {
        int preset = Math.Max(PresetPicker.SelectedIndex, 0);
        int checkedScenarios = 0;
        int scenarioIndex = -1;
        try
        {
            for (int i = 0; i < _presets.Length; i++)
            {
                scenarioIndex = i;
                DiagnosticStage($"Smoke scenario {i}: {_presets[i]}");
                ApplyPreset(i);
                if (Plot.Axes.Count < 2 || Plot.Series.Count < 1 || Plot.Data.Count != Plot.Series.Count + 1)
                    throw new InvalidOperationException(L.F("ChartsLabPage_RunSmokeTests_013", (i)));
                var series = Plot.Series[0];
                series.ShowDataLabels = true;
                series.ShowDataMarkers = true;
                foreach (MarkerShape shape in Enum.GetValues<MarkerShape>()) series.MarkerShape = shape;
                foreach (StrokeDashStyle dash in Enum.GetValues<StrokeDashStyle>()) series.StrokeDashStyle = dash;
                series.DataLabelOverrides[0] = new DataLabelOverride("API", BrushAt(0));
                series.DataMarkerOverrides[0] = new DataMarkerOverride(MarkerShape.Diamond, BrushAt(0));
                series.IsVisible = false;
                series.IsVisible = true;
                AddPoint();
                MutateData(false);
                RemovePoint();
                checkedScenarios++;
            }
            bool linearXAccepted = ProbeLinearXAxis(out _);
            ApplyPreset(preset);
            string result = L.F("ChartsLabPage_RunSmokeTests_014", (checkedScenarios), ((linearXAccepted ? L.T("ChartsLabPage_RunSmokeTests_015") : L.T("ChartsLabPage_RunSmokeTests_016"))));
            SetStatus(result, InfoBarSeverity.Success);
            return result;
        }
        catch (Exception ex)
        {
            string failedStage = _diagnosticStage;
            string failedName = scenarioIndex >= 0 ? _presets[scenarioIndex] : L.T("ChartsLabPage_RunSmokeTests_017");
            string result = L.F("ChartsLabPage_RunSmokeTests_018", (scenarioIndex), (failedName), (checkedScenarios), (failedStage), (ex.Message));
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SdkComponentLab-chart.log"), result + "\n" + ex + "\n");
            SetStatus(result, InfoBarSeverity.Error);
            try { ApplyPreset(preset); }
            catch (Exception restoreError) { result += L.F("ChartsLabPage_RunSmokeTests_019", (restoreError.Message)); }
            SetStatus(result, InfoBarSeverity.Error);
            return result;
        }
    }

    private void ApplyPreset(int index)
    {
        try
        {
        DiagnosticStage("ApplyPreset: clear Chart collections");
        _loading = true;
        _timer.Stop();
        StreamButton.IsChecked = false;
        Plot.Series.Clear();
        Plot.Axes.Clear();
        Plot.Data.Clear();
        _knownBrushes.Clear();
        _fillOpacities.Clear();
        _ys.Clear();
        _ySamples.Clear();
        _x.Clear();
        _counter = 0;
        _selectedSeries = 0;
        _fillOpacity = 0.28;
        _pointLabels = false;
        _pointMarkers = false;
        ValuesList.SelectedIndex = -1;
        _axisKind = index == 4 || index == 9 ? 1 : index == 5 ? 2 : 0;
        DiagnosticStage("ApplyPreset: create X axis");
        _xAxis = MakeXAxis(_axisKind);
        DiagnosticStage("ApplyPreset: create linear Y axes");
        _yAxis = new LinearAxis { Label = L.T("ChartsLabPage_ApplyPreset_020"), GridLines = GridLines.Major, ShowTickLabels = true, ShowTickMarks = true };
        _secondaryY = new LinearAxis { Label = L.T("ChartsLabPage_ApplyPreset_021"), GridLines = GridLines.None, ShowTickLabels = true, ShowTickMarks = true };
        ApplyAxisBrushes(_xAxis);
        ApplyAxisBrushes(_yAxis);
        ApplyAxisBrushes(_secondaryY);
        DiagnosticStage("ApplyPreset: append axes");
        Plot.Axes.Add(_xAxis);
        Plot.Axes.Add(_yAxis);
        if (index == 6) Plot.Axes.Add(_secondaryY);
        DiagnosticStage("ApplyPreset: new Samples, assign typed source");
        _xSamples = new Samples { ItemsSource = XSnapshot() };
        DiagnosticStage("ApplyPreset: append X Samples to Data");
        Plot.Data.Add(_xSamples);
        DiagnosticStage("ApplyPreset: legend configuration");
        Plot.ShowLegend = true;
        Plot.LegendTitle = L.T("ChartsLabPage_ApplyPreset_022");
        int count = index == 8 ? 0 : index == 9 ? 200 : 7;
        DiagnosticStage("ApplyPreset: populate typed X source before attaching series");
        for (int i = 0; i < count; i++) AddPoint(false);
        _xSamples.ItemsSource = XSnapshot();
        int seriesCount = index == 3 ? 3 : 2;
        for (int i = 0; i < seriesCount; i++)
        {
            DiagnosticStage("ApplyPreset: AddSeries " + i);
            int kind = index == 1 ? 1 : index == 2 || index == 7 ? 2 : index == 3 ? i : 0;
            AddSeries(kind, index == 7, index == 6 && i == 1);
        }
        _loading = false;
        DiagnosticStage("ApplyPreset: RefreshSettings");
        RefreshSettings(resetSections: true);
        DiagnosticStage("ApplyPreset: UpdateValues");
        UpdateValues();
        string message = L.F("ChartsLabPage_ApplyPreset_023", (_presets[index]), (Plot.Series.Count), (_x.Count));
        if (_axisKind == 1) message += " " + NumericXAxisNote;
        SetStatus(message, _axisKind == 1 ? InfoBarSeverity.Warning : InfoBarSeverity.Informational);
        }
        finally { _loading = false; }
    }

    private CartesianAxis MakeXAxis(int kind) => kind switch
    {
        1 => new CategoryAxis { Label = L.T("ChartsLabPage_MakeXAxis_024"), SortKey = CategorySortKey.Index, SortOrder = SortOrder.Ascending, ShowTickLabels = true, ShowTickMarks = true, GridLines = GridLines.Major },
        2 => new DateTimeAxis { Label = L.T("ChartsLabPage_MakeXAxis_025"), IntervalType = DateTimeIntervalType.Day, LabelFormat = "day month.abbreviated", ShowTickLabels = true, ShowTickMarks = true, GridLines = GridLines.Major },
        _ => new CategoryAxis { Label = L.T("ChartsLabPage_MakeXAxis_026"), SortKey = CategorySortKey.Index, SortOrder = SortOrder.Ascending, ShowTickLabels = true, ShowTickMarks = true, GridLines = GridLines.None }
    };

    private CartesianSeries CreateSeries(int kind, string title, Samples y, bool horizontal, bool secondary)
    {
        DiagnosticStage("CreateSeries: constructor kind=" + kind);
        CartesianSeries series = kind switch { 1 => new AreaSeries(), 2 => new BarSeries { Orientation = horizontal ? BarOrientation.Horizontal : BarOrientation.Vertical }, _ => new LineSeries() };
        DiagnosticStage("CreateSeries: Title");
        series.Title = title;
        // Supply typed values before connecting the chosen axes.
        DiagnosticStage("CreateSeries: XValues / " + _axisKind);
        series.XValues = _xSamples;
        DiagnosticStage("CreateSeries: YValues");
        series.YValues = y;
        DiagnosticStage("CreateSeries: XAxis " + _xAxis.GetType().Name);
        series.XAxis = _xAxis;
        DiagnosticStage("CreateSeries: YAxis " + (secondary ? "secondary LinearAxis" : "LinearAxis"));
        series.YAxis = secondary ? _secondaryY : _yAxis;
        DiagnosticStage("CreateSeries: Stroke");
        series.Stroke = BrushAt(5 + Plot.Series.Count % 5);
        series.DataMarkerBrush = series.Stroke;
        series.DataLabelBrush = BrushAt(3);
        DiagnosticStage("CreateSeries: StrokeThickness / IsVisible");
        series.StrokeThickness = 2;
        series.IsVisible = true;
        DiagnosticStage("CreateSeries: MarkerShape / ShowDataMarkers");
        series.MarkerShape = MarkerShape.Circle;
        series.ShowDataMarkers = true;
        if (series is AreaSeries area) area.Fill = FillBrush(series.Stroke);
        if (series is BarSeries bar) bar.Fill = series.Stroke;
        return series;
    }

    private void AddSeries(int kind = 0, bool horizontal = false, bool secondary = false)
    {
        var values = new ObservableCollection<object>();
        for (int i = 0; i < _x.Count; i++) values.Add(25.0 + _random.Next(50));
        DiagnosticStage("AddSeries: Samples.ItemsSource typed double[]");
        var samples = new Samples { ItemsSource = values.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray() };
        _ys.Add(values);
        _ySamples.Add(samples);
        DiagnosticStage("AddSeries: Chart.Data.Add");
        Plot.Data.Add(samples);
        DiagnosticStage("AddSeries: CreateSeries");
        var series = CreateSeries(kind, L.F("ChartsLabPage_AddSeries_027", (Plot.Series.Count + 1)), samples, horizontal, secondary);
        DiagnosticStage("AddSeries: Chart.Series.Add");
        Plot.Series.Add(series);
    }

    private void AddPoint(bool notify = true)
    {
        _counter++;
        _x.Add(_axisKind switch { 1 => (object)(double)_counter, 2 => new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero).AddDays(_counter - 1), _ => L.F("ChartsLabPage_AddPoint_028", (_counter)) });
        for (int i = 0; i < _ys.Count; i++) _ys[i].Add(Math.Round(35 + 22 * Math.Sin(_counter * 0.8 + i) + _random.NextDouble() * 8 + i * 12, 1));
        if (notify) { UpdateValues(); SetStatus(L.F("ChartsLabPage_AddPoint_029", (_counter))); }
    }

    private void RemovePoint()
    {
        if (_x.Count == 0) return;
        uint removedIndex = (uint)(_x.Count - 1);
        _x.RemoveAt(_x.Count - 1);
        foreach (var values in _ys) values.RemoveAt(values.Count - 1);
        foreach (var series in Plot.Series) { series.DataLabelOverrides.Remove(removedIndex); series.DataMarkerOverrides.Remove(removedIndex); }
        UpdateValues();
        SetStatus(L.F("ChartsLabPage_RemovePoint_030", (_x.Count)));
    }

    private void MutateData(bool stream)
    {
        if (stream)
        {
            AddPoint(false);
            bool removed = false;
            while (_x.Count > 30)
            {
                _x.RemoveAt(0);
                foreach (var values in _ys) values.RemoveAt(0);
                removed = true;
            }
            if (removed) foreach (var series in Plot.Series) { series.DataLabelOverrides.Clear(); series.DataMarkerOverrides.Clear(); }
        }
        else foreach (var values in _ys) for (int i = 0; i < values.Count; i++) values[i] = Math.Round(15 + _random.NextDouble() * 90, 1);
        UpdateValues();
        SetStatus(stream ? L.F("ChartsLabPage_MutateData_031", (_x.Count), (_counter)) : L.T("ChartsLabPage_MutateData_032"));
    }

    private Brush BrushAt(int index)
    {
        if (index < 0 || index >= _brushKeys.Length) throw new InvalidOperationException(L.T("ChartsLabPage_BrushAt_033"));
        Brush brush = _brushProbes[index].Background ?? Application.Current.Resources[_brushKeys[index]] as Brush ?? new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        // This preview renderer reads Color.A, but does not apply Brush.Opacity.
        if (brush is SolidColorBrush solid && solid.Opacity != 1)
        {
            var color = solid.Color;
            color.A = (byte)Math.Round(color.A * Math.Clamp(solid.Opacity, 0, 1));
            if (!_nativeBrushes.TryGetValue(index, out var converted) || converted.Color != color)
                _nativeBrushes[index] = converted = new SolidColorBrush(color);
            brush = converted;
        }
        _knownBrushes[brush] = index;
        return brush;
    }
    private void ApplyAxisBrushes(CartesianAxis axis)
    {
        axis.AxisLineBrush = BrushAt(3);
        axis.TickBrush = BrushAt(3);
        axis.TickLabelBrush = BrushAt(3);
        axis.GridLineMajorBrush = BrushAt(4);
        axis.GridLineMinorBrush = BrushAt(4);
    }
    private Brush FillBrush(Brush stroke, double? opacity = null)
    {
        if (stroke is not SolidColorBrush solid) return stroke;
        double factor = Math.Clamp(opacity ?? _fillOpacity, 0, 1);
        var color = solid.Color;
        bool fromPalette = _knownBrushes.TryGetValue(stroke, out int index);
        if (fromPalette && BrushAt(index) is SolidColorBrush palette) color = palette.Color;
        else color.A = 255;
        color.A = (byte)Math.Round(color.A * factor);
        var fill = new SolidColorBrush(color);
        _fillOpacities[fill] = factor;
        if (fromPalette) _knownBrushes[fill] = index;
        return fill;
    }
    private double FillOpacity(Brush? brush) => brush is null ? _fillOpacity
        : _fillOpacities.TryGetValue(brush, out double factor) ? factor
        : brush is SolidColorBrush solid ? solid.Color.A / 255.0 * brush.Opacity : brush.Opacity;
    private int BrushIndex(Brush? brush)
    {
        if (brush is null) return -1;
        if (_knownBrushes.TryGetValue(brush, out int known)) return known;
        for (int i = 0; i < _brushKeys.Length; i++)
            if (brush is SolidColorBrush current && BrushAt(i) is SolidColorBrush preset && current.Color == preset.Color) return i;
        return -1;
    }
    private Brush? RefreshBrush(Brush? brush, bool preserveOpacity = false)
    {
        if (brush is null || !_knownBrushes.TryGetValue(brush, out int index)) return brush;
        var themed = BrushAt(index);
        return preserveOpacity ? FillBrush(themed, FillOpacity(brush)) : themed;
    }
    private void RefreshThemeBrushes()
    {
        foreach (var axis in Plot.Axes.OfType<CartesianAxis>().Concat(new CartesianAxis[] { _xAxis, _yAxis, _secondaryY }).Distinct())
        {
            axis.AxisLineBrush = RefreshBrush(axis.AxisLineBrush);
            axis.TickBrush = RefreshBrush(axis.TickBrush);
            axis.TickLabelBrush = RefreshBrush(axis.TickLabelBrush);
            axis.GridLineMajorBrush = RefreshBrush(axis.GridLineMajorBrush);
            axis.GridLineMinorBrush = RefreshBrush(axis.GridLineMinorBrush);
        }
        foreach (var series in Plot.Series)
        {
            series.Stroke = RefreshBrush(series.Stroke);
            series.DataMarkerBrush = RefreshBrush(series.DataMarkerBrush);
            series.DataLabelBrush = RefreshBrush(series.DataLabelBrush);
            if (series is AreaSeries area) area.Fill = RefreshBrush(area.Fill, true);
            if (series is BarSeries bar) bar.Fill = RefreshBrush(bar.Fill, true);
            foreach (var entry in series.DataLabelOverrides.ToArray()) series.DataLabelOverrides[entry.Key] = new DataLabelOverride(entry.Value.Text, RefreshBrush(entry.Value.Brush));
            foreach (var entry in series.DataMarkerOverrides.ToArray()) series.DataMarkerOverrides[entry.Key] = new DataMarkerOverride(entry.Value.Shape, RefreshBrush(entry.Value.Brush));
        }
    }

    private void RefreshSettings(bool resetSections = false)
    {
        if (_loading || _xAxis is null) return;
        if (resetSections) _sectionExpansion.Clear();
        else foreach (var expander in SettingsHost.Children.OfType<Expander>())
            _sectionExpansion[AutomationProperties.GetAutomationId(expander)] = expander.IsExpanded;
        _loading = true;
        SettingsHost.Children.Clear();
        var chart = Section(L.T("ChartsLabPage_RefreshSettings_034"), true);
        Toggle(chart, "ChartShowLegend", L.T("ChartsLabPage_RefreshSettings_035"), Plot.ShowLegend, value => Plot.ShowLegend = value);
        Text(chart, "ChartLegendTitle", L.T("ChartsLabPage_RefreshSettings_036"), Plot.LegendTitle, value => Plot.LegendTitle = value);
        var selected = Choice(chart, "ChartSeriesSelect", L.T("ChartsLabPage_RefreshSettings_037"), Plot.Series.Select((s, i) => $"{i + 1}. {s.Title}"), _selectedSeries, index => { _selectedSeries = index; RefreshSettings(); UpdateValues(); });
        selected.IsEnabled = Plot.Series.Count > 0;
        if (SelectedSeries is { } series)
        {
            Choice(chart, "ChartSeriesType", L.T("ChartsLabPage_RefreshSettings_038"), [L.T("ChartsLabPage_RefreshSettings_039"), L.T("ChartsLabPage_RefreshSettings_040"), L.T("ChartsLabPage_RefreshSettings_041")], series is AreaSeries ? 1 : series is BarSeries ? 2 : 0, ReplaceSeries);
            Text(chart, "ChartSeriesTitle", L.T("ChartsLabPage_RefreshSettings_042"), series.Title, value => { series.Title = value; });
            Toggle(chart, "ChartSeriesVisible", L.T("ChartsLabPage_RefreshSettings_043"), series.IsVisible, value => series.IsVisible = value);
            Numeric(chart, "ChartStrokeThickness", L.T("ChartsLabPage_RefreshSettings_044"), series.StrokeThickness, 0, 12, value => series.StrokeThickness = value);
            Choice(chart, "ChartStrokeBrush", L.T("ChartsLabPage_RefreshSettings_045"), _brushKeys, BrushIndex(series.Stroke), index => { series.Stroke = BrushAt(index); });
            EnumChoice(chart, "ChartDashStyle", L.T("ChartsLabPage_RefreshSettings_046"), series.StrokeDashStyle, value => series.StrokeDashStyle = value);
            if (series is AreaSeries || series is BarSeries)
            {
                var fill = series is AreaSeries area ? area.Fill : ((BarSeries)series).Fill;
                Choice(chart, "ChartFillBrush", L.T("ChartsLabPage_RefreshSettings_047"), _brushKeys, BrushIndex(fill), index => { if (series is AreaSeries a) a.Fill = FillBrush(BrushAt(index), FillOpacity(a.Fill)); if (series is BarSeries b) b.Fill = FillBrush(BrushAt(index), FillOpacity(b.Fill)); });
                Numeric(chart, "ChartFillOpacity", L.T("ChartsLabPage_RefreshSettings_048"), FillOpacity(fill), 0, 1, value => { if (!double.IsFinite(value)) return; _fillOpacity = value; if (series is AreaSeries a) a.Fill = FillBrush(a.Fill); if (series is BarSeries b) b.Fill = FillBrush(b.Fill); });
            }
            if (series is BarSeries bar) EnumChoice(chart, "ChartBarOrientation", L.T("ChartsLabPage_RefreshSettings_049"), bar.Orientation, value => bar.Orientation = value);
            Toggle(chart, "ChartShowLabels", L.T("ChartsLabPage_RefreshSettings_050"), series.ShowDataLabels, value => series.ShowDataLabels = value);
            Choice(chart, "ChartLabelBrush", L.T("ChartsLabPage_RefreshSettings_051"), _brushKeys, BrushIndex(series.DataLabelBrush), index => series.DataLabelBrush = BrushAt(index));
            Toggle(chart, "ChartShowMarkers", L.T("ChartsLabPage_RefreshSettings_052"), series.ShowDataMarkers, value => series.ShowDataMarkers = value);
            EnumChoice(chart, "ChartMarkerShape", L.T("ChartsLabPage_RefreshSettings_053"), series.MarkerShape, value => series.MarkerShape = value);
            Choice(chart, "ChartMarkerBrush", L.T("ChartsLabPage_RefreshSettings_054"), _brushKeys, BrushIndex(series.DataMarkerBrush), index => series.DataMarkerBrush = BrushAt(index));
            Toggle(chart, "ChartUseSecondaryY", L.T("ChartsLabPage_RefreshSettings_055"), series.YAxis == _secondaryY, value =>
            {
                if (value && !Plot.Axes.Contains(_secondaryY)) Plot.Axes.Add(_secondaryY);
                series.YAxis = value ? _secondaryY : _yAxis;
                if (!value && !Plot.Series.Any(s => s.YAxis == _secondaryY)) Plot.Axes.Remove(_secondaryY);
            });
        }

        var xPanel = Section(L.T("ChartsLabPage_RefreshSettings_056"), false);
        Choice(xPanel, "ChartXAxisKind", L.T("ChartsLabPage_RefreshSettings_057"), [L.T("ChartsLabPage_RefreshSettings_058"), L.T("ChartsLabPage_RefreshSettings_059"), L.T("ChartsLabPage_RefreshSettings_060")], _axisKind, ChangeXAxis);
        Button(xPanel, "ChartProbeLinearX", L.T("ChartsLabPage_RefreshSettings_061"), () =>
        {
            bool accepted = ProbeLinearXAxis(out string details);
            SetStatus(accepted ? L.T("ChartsLabPage_RefreshSettings_062") + details : NumericXAxisNote + " " + details, accepted ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        });
        AxisSettings(xPanel, _xAxis, "X");
        if (_xAxis is CategoryAxis category)
        {
            EnumChoice(xPanel, "ChartCategorySortKey", L.T("ChartsLabPage_RefreshSettings_063"), category.SortKey, value => category.SortKey = value);
            EnumChoice(xPanel, "ChartCategorySortOrder", L.T("ChartsLabPage_RefreshSettings_064"), category.SortOrder, value => category.SortOrder = value);
        }
        if (_xAxis is LinearAxis linearX) LinearSettings(xPanel, linearX, "X");
        if (_xAxis is DateTimeAxis date)
        {
            EnumChoice(xPanel, "ChartDateInterval", L.T("ChartsLabPage_RefreshSettings_065"), date.IntervalType, value => date.IntervalType = value);
            Text(xPanel, "ChartDateFormat", L.T("ChartsLabPage_RefreshSettings_066"), date.LabelFormat, value => date.LabelFormat = value);
            Hint(xPanel, "Windows.Globalization.DateTimeFormatting: day month.abbreviated, month year, shortdate.");
            DateSetting(xPanel, "ChartDateMin", L.T("ChartsLabPage_RefreshSettings_067"), date.Minimum, value => { if (value is null || date.Maximum is null || value < date.Maximum) date.Minimum = value; else SetStatus(L.T("ChartsLabPage_RefreshSettings_068"), InfoBarSeverity.Warning); });
            DateSetting(xPanel, "ChartDateMax", L.T("ChartsLabPage_RefreshSettings_069"), date.Maximum, value => { if (value is null || date.Minimum is null || value > date.Minimum) date.Maximum = value; else SetStatus(L.T("ChartsLabPage_RefreshSettings_070"), InfoBarSeverity.Warning); });
        }
        var yPanel = Section(L.T("ChartsLabPage_RefreshSettings_071"), false);
        AxisSettings(yPanel, _yAxis, "Y");
        LinearSettings(yPanel, _yAxis, "Y");
        var secondaryPanel = Section(L.T("ChartsLabPage_RefreshSettings_072"), false);
        AxisSettings(secondaryPanel, _secondaryY, "Y2");
        LinearSettings(secondaryPanel, _secondaryY, "Y2");

        var points = Section(L.T("ChartsLabPage_RefreshSettings_073"), true);
        _pointIndex = Numeric(points, "ChartPointIndex", L.T("ChartsLabPage_RefreshSettings_074"), Math.Max(ValuesList.SelectedIndex, 0), 0, Math.Max(_x.Count - 1, 0), value =>
        {
            if (_x.Count > 0 && double.IsFinite(value)) ValuesList.SelectedIndex = Math.Clamp((int)value, 0, _x.Count - 1);
            ReadPoint();
        });
        _pointX = Text(points, "ChartPointX", L.T("ChartsLabPage_RefreshSettings_075"), "", _ => { });
        _pointY = Numeric(points, "ChartPointY", L.T("ChartsLabPage_RefreshSettings_076"), 0, -100000, 100000, _ => { });
        Button(points, "ChartApplyPoint", L.T("ChartsLabPage_RefreshSettings_077"), ApplyPoint);
        Hint(points, L.T("ChartsLabPage_RefreshSettings_078"));
        ReadPoint();

        var overrides = Section(L.T("ChartsLabPage_RefreshSettings_079"), false);
        Hint(overrides, L.T("ChartsLabPage_RefreshSettings_080"));
        Toggle(overrides, "ChartLabelOverrideEnabled", L.T("ChartsLabPage_RefreshSettings_081"), _pointLabels, value => _pointLabels = value);
        _overrideText = Text(overrides, "ChartLabelOverrideText", L.T("ChartsLabPage_RefreshSettings_082"), L.T("ChartsLabPage_RefreshSettings_083"), _ => { });
        _overrideLabelBrush = Choice(overrides, "ChartLabelOverrideBrush", L.T("ChartsLabPage_RefreshSettings_084"), _brushKeys, 0, _ => { });
        Toggle(overrides, "ChartMarkerOverrideEnabled", L.T("ChartsLabPage_RefreshSettings_085"), _pointMarkers, value => _pointMarkers = value);
        _overrideShape = EnumChoice(overrides, "ChartMarkerOverrideShape", L.T("ChartsLabPage_RefreshSettings_086"), MarkerShape.Diamond, _ => { });
        _overrideMarkerBrush = Choice(overrides, "ChartMarkerOverrideBrush", L.T("ChartsLabPage_RefreshSettings_087"), _brushKeys, 2, _ => { });
        Button(overrides, "ChartApplyOverrides", L.T("ChartsLabPage_RefreshSettings_088"), ApplyOverrides);
        Button(overrides, "ChartClearOverrides", L.T("ChartsLabPage_RefreshSettings_089"), () => { if (SelectedSeries is { } s) { s.DataLabelOverrides.Clear(); s.DataMarkerOverrides.Clear(); } SetStatus(L.T("ChartsLabPage_RefreshSettings_090")); });
        var api = Section(L.T("ChartsLabPage_RefreshSettings_091"), false);
        Hint(api, L.T("ChartsLabPage_RefreshSettings_092"));
        Hint(api, L.T("ChartsLabPage_RefreshSettings_093"));
        _loading = false;
    }

    private void AxisSettings(StackPanel panel, CartesianAxis axis, string id)
    {
        Text(panel, "Chart" + id + "Label", L.T("ChartsLabPage_AxisSettings_094"), axis.Label, value => axis.Label = value);
        Toggle(panel, "Chart" + id + "Visible", L.T("ChartsLabPage_AxisSettings_095"), axis.IsVisible, value => axis.IsVisible = value);
        Toggle(panel, "Chart" + id + "TickLabels", L.T("ChartsLabPage_AxisSettings_096"), axis.ShowTickLabels, value => axis.ShowTickLabels = value);
        Toggle(panel, "Chart" + id + "TickMarks", L.T("ChartsLabPage_AxisSettings_097"), axis.ShowTickMarks, value => axis.ShowTickMarks = value);
        Choice(panel, "Chart" + id + "GridLines", L.T("ChartsLabPage_AxisSettings_098"), [L.T("ChartsLabPage_AxisSettings_099"), L.T("ChartsLabPage_AxisSettings_100"), L.T("ChartsLabPage_AxisSettings_101")], (int)axis.GridLines, value => axis.GridLines = (GridLines)value);
        Choice(panel, "Chart" + id + "LineBrush", L.T("ChartsLabPage_AxisSettings_102"), _brushKeys, BrushIndex(axis.AxisLineBrush), value => axis.AxisLineBrush = BrushAt(value));
        Choice(panel, "Chart" + id + "MajorBrush", L.T("ChartsLabPage_AxisSettings_103"), _brushKeys, BrushIndex(axis.GridLineMajorBrush), value => axis.GridLineMajorBrush = BrushAt(value));
        Choice(panel, "Chart" + id + "MinorBrush", L.T("ChartsLabPage_AxisSettings_104"), _brushKeys, BrushIndex(axis.GridLineMinorBrush), value => axis.GridLineMinorBrush = BrushAt(value));
        Choice(panel, "Chart" + id + "TickBrush", L.T("ChartsLabPage_AxisSettings_105"), _brushKeys, BrushIndex(axis.TickBrush), value => axis.TickBrush = BrushAt(value));
        Choice(panel, "Chart" + id + "TickLabelBrush", L.T("ChartsLabPage_AxisSettings_106"), _brushKeys, BrushIndex(axis.TickLabelBrush), value => axis.TickLabelBrush = BrushAt(value));
    }

    private void LinearSettings(StackPanel panel, LinearAxis axis, string id)
    {
        Hint(panel, L.T("ChartsLabPage_LinearSettings_107"));
        Numeric(panel, "Chart" + id + "MinValue", L.T("ChartsLabPage_LinearSettings_108"), axis.Minimum ?? double.NaN, -100000, 100000, value => { double? v = NullableNumber(value); if (v is null || axis.Maximum is null || v < axis.Maximum) axis.Minimum = v; else SetStatus(L.T("ChartsLabPage_LinearSettings_109"), InfoBarSeverity.Warning); });
        Numeric(panel, "Chart" + id + "MaxValue", L.T("ChartsLabPage_LinearSettings_110"), axis.Maximum ?? double.NaN, -100000, 100000, value => { double? v = NullableNumber(value); if (v is null || axis.Minimum is null || v > axis.Minimum) axis.Maximum = v; else SetStatus(L.T("ChartsLabPage_LinearSettings_111"), InfoBarSeverity.Warning); });
        Numeric(panel, "Chart" + id + "SpacingValue", L.T("ChartsLabPage_LinearSettings_112"), axis.Spacing ?? double.NaN, 0.01, 10000, value => axis.Spacing = NullableNumber(value));
    }

    private static double? NullableNumber(double value) => double.IsFinite(value) ? value : null;

    private void ChangeXAxis(int kind)
    {
        if (kind == _axisKind) return;
        var oldSeries = Plot.Series.ToArray();
        int previousKind = _axisKind;
        object[] previousX = _x.ToArray();
        var previousY = _yAxis;
        var previousSecondary = _secondaryY;
        if (kind == 1) ProbeLinearXAxis(out _);
        try { ReconfigureXAxis(kind, oldSeries, previousY, previousSecondary, null); }
        catch (Exception ex)
        {
            try { ReconfigureXAxis(previousKind, oldSeries, previousY, previousSecondary, previousX); }
            catch (Exception restore) { throw new InvalidOperationException(L.T("ChartsLabPage_ChangeXAxis_113") + restore.Message, ex); }
            throw new InvalidOperationException(L.T("ChartsLabPage_ChangeXAxis_114") + ex.Message, ex);
        }
        RefreshSettings();
        UpdateValues();
        SetStatus(L.T("ChartsLabPage_ChangeXAxis_115") + (kind == 1 ? " " + NumericXAxisNote : ""), kind == 1 ? InfoBarSeverity.Warning : InfoBarSeverity.Informational);
    }

    private bool ProbeLinearXAxis(out string details)
    {
        var probe = new LineSeries
        {
            XValues = new Samples { ItemsSource = new double[] { 1, 3, 10 } },
            YValues = new Samples { ItemsSource = new double[] { 5, 8, 6 } }
        };
        try { probe.XAxis = new LinearAxis(); details = L.T("ChartsLabPage_ProbeLinearXAxis_116"); return true; }
        catch (ArgumentException ex) { details = $"LineSeries.XAxis, HRESULT 0x{ex.HResult:X8}: {ex.Message}"; return false; }
    }

    private void ReconfigureXAxis(int kind, CartesianSeries[] originalSeries, LinearAxis originalY, LinearAxis originalSecondary, object[]? restoredX)
    {
        Plot.Series.Clear();
        Plot.Axes.Clear();
        Plot.Data.Clear();
        _axisKind = kind;
        _xAxis = MakeXAxis(kind);
        ApplyAxisBrushes(_xAxis);
        _yAxis = CloneLinearAxis(originalY);
        _secondaryY = CloneLinearAxis(originalSecondary);
        for (int i = 0; i < _x.Count; i++)
            _x[i] = restoredX is not null ? restoredX[i] : kind switch { 1 => (object)(double)(i + 1), 2 => new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i), _ => L.F("ChartsLabPage_ReconfigureXAxis_117", (i + 1)) };
        _xSamples = new Samples { ItemsSource = XSnapshot() };
        Plot.Axes.Add(_xAxis);
        Plot.Axes.Add(_yAxis);
        if (originalSeries.Any(s => s.YAxis == originalSecondary)) Plot.Axes.Add(_secondaryY);
        Plot.Data.Add(_xSamples);
        _ySamples.Clear();
        for (int i = 0; i < originalSeries.Length; i++)
        {
            var old = originalSeries[i];
            var samples = new Samples { ItemsSource = _ys[i].Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray() };
            _ySamples.Add(samples);
            Plot.Data.Add(samples);
            var replacement = CreateSeries(old is AreaSeries ? 1 : old is BarSeries ? 2 : 0, old.Title, samples, old is BarSeries b && b.Orientation == BarOrientation.Horizontal, old.YAxis == originalSecondary);
            CopySeriesAppearance(old, replacement);
            Plot.Series.Add(replacement);
        }
    }

    private static LinearAxis CloneLinearAxis(LinearAxis source) => new()
    {
        Label = source.Label, IsVisible = source.IsVisible,
        Minimum = source.Minimum, Maximum = source.Maximum, Spacing = source.Spacing,
        GridLines = source.GridLines, ShowTickLabels = source.ShowTickLabels, ShowTickMarks = source.ShowTickMarks,
        AxisLineBrush = source.AxisLineBrush, TickBrush = source.TickBrush, TickLabelBrush = source.TickLabelBrush,
        GridLineMajorBrush = source.GridLineMajorBrush, GridLineMinorBrush = source.GridLineMinorBrush
    };

    private static void CopySeriesAppearance(CartesianSeries source, CartesianSeries target)
    {
        target.Stroke = source.Stroke;
        target.StrokeThickness = source.StrokeThickness;
        target.StrokeDashStyle = source.StrokeDashStyle;
        target.DataLabelBrush = source.DataLabelBrush;
        target.DataMarkerBrush = source.DataMarkerBrush;
        target.IsVisible = source.IsVisible;
        target.ShowDataLabels = source.ShowDataLabels;
        target.ShowDataMarkers = source.ShowDataMarkers;
        target.MarkerShape = source.MarkerShape;
        if (source is AreaSeries oldArea && target is AreaSeries area) area.Fill = oldArea.Fill;
        if (source is BarSeries oldBar && target is BarSeries bar) bar.Fill = oldBar.Fill;
        foreach (var entry in source.DataLabelOverrides) target.DataLabelOverrides[entry.Key] = entry.Value;
        foreach (var entry in source.DataMarkerOverrides) target.DataMarkerOverrides[entry.Key] = entry.Value;
    }

    private void ReplaceSeries(int kind)
    {
        if (SelectedSeries is not { } old) return;
        int i = Math.Clamp(_selectedSeries, 0, Plot.Series.Count - 1);
        var replacement = CreateSeries(kind, old.Title, _ySamples[i], false, old.YAxis == _secondaryY);
        replacement.Stroke = old.Stroke;
        replacement.StrokeThickness = old.StrokeThickness;
        replacement.StrokeDashStyle = old.StrokeDashStyle;
        replacement.DataLabelBrush = old.DataLabelBrush;
        replacement.DataMarkerBrush = old.DataMarkerBrush;
        replacement.IsVisible = old.IsVisible;
        replacement.ShowDataLabels = old.ShowDataLabels;
        replacement.ShowDataMarkers = old.ShowDataMarkers;
        replacement.MarkerShape = old.MarkerShape;
        foreach (var item in old.DataLabelOverrides) replacement.DataLabelOverrides[item.Key] = item.Value;
        foreach (var item in old.DataMarkerOverrides) replacement.DataMarkerOverrides[item.Key] = item.Value;
        if (replacement is AreaSeries a) a.Fill = FillBrush(old.Stroke);
        if (replacement is BarSeries b) b.Fill = old.Stroke;
        Plot.Series[i] = replacement;
        RefreshSettings();
        SetStatus(L.F("ChartsLabPage_ReplaceSeries_118", (replacement.GetType().Name)));
    }

    private void ReadPoint()
    {
        if (_pointIndex is null || _pointX is null || _pointY is null || _x.Count == 0 || SelectedSeries is null) return;
        int index = Math.Clamp((int)_pointIndex.Value, 0, _x.Count - 1);
        _pointX.Text = _x[index] is DateTimeOffset date ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : Convert.ToString(_x[index], CultureInfo.InvariantCulture) ?? "";
        _pointY.Value = Convert.ToDouble(_ys[_selectedSeries][index], CultureInfo.InvariantCulture);
    }

    private void ApplyPoint()
    {
        if (_x.Count == 0 || SelectedSeries is null) return;
        int index = Math.Clamp((int)_pointIndex.Value, 0, _x.Count - 1);
        if (_axisKind == 1)
        {
            if (!double.TryParse(_pointX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number)) { SetStatus(L.T("ChartsLabPage_ApplyPoint_119"), InfoBarSeverity.Warning); return; }
            _x[index] = number;
        }
        else if (_axisKind == 2)
        {
            if (!DateTimeOffset.TryParse(_pointX.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) { SetStatus(L.T("ChartsLabPage_ApplyPoint_120"), InfoBarSeverity.Warning); return; }
            _x[index] = date;
        }
        else _x[index] = _pointX.Text;
        if (double.IsFinite(_pointY.Value)) _ys[_selectedSeries][index] = _pointY.Value;
        UpdateValues();
        SetStatus(L.F("ChartsLabPage_ApplyPoint_121", (index), (_selectedSeries + 1)));
    }

    private void ApplyOverrides()
    {
        if (_x.Count == 0 || SelectedSeries is not { } series) return;
        uint index = (uint)Math.Clamp((int)_pointIndex.Value, 0, _x.Count - 1);
        if (_pointLabels) { series.ShowDataLabels = true; series.DataLabelOverrides[index] = new DataLabelOverride(_overrideText.Text, BrushAt(_overrideLabelBrush.SelectedIndex)); }
        else series.DataLabelOverrides.Remove(index);
        if (_pointMarkers) { series.ShowDataMarkers = true; series.DataMarkerOverrides[index] = new DataMarkerOverride(Enum.GetValues<MarkerShape>()[_overrideShape.SelectedIndex], BrushAt(_overrideMarkerBrush.SelectedIndex)); }
        else series.DataMarkerOverrides.Remove(index);
        SetStatus(L.F("ChartsLabPage_ApplyOverrides_122", (index), (series.DataLabelOverrides.Count), (series.DataMarkerOverrides.Count)));
    }

    private void UpdateValues()
    {
        RefreshSampleSources();
        int selected = _pointIndex is not null && double.IsFinite(_pointIndex.Value) ? (int)_pointIndex.Value : ValuesList.SelectedIndex;
        ValuesList.ItemsSource = _x.Select((x, i) => $"{i,3}    X: {(x is DateTimeOffset d ? d.ToString("dd.MM.yyyy") : x)}    Y: {(_ys.Count == 0 ? "—" : _ys[Math.Clamp(_selectedSeries, 0, _ys.Count - 1)][i])}").ToArray();
        if (_x.Count > 0) ValuesList.SelectedIndex = Math.Clamp(selected, 0, _x.Count - 1);
        if (_pointIndex is not null) _pointIndex.Maximum = Math.Max(_x.Count - 1, 0);
    }

    // Typed arrays preserve the WinRT numeric/category/date element type.
    // Assigning a new ItemsSource pushes each edit to the experimental native component.
    private object XSnapshot() => _axisKind switch
    {
        1 => _x.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)).ToArray(),
        2 => _x.Cast<DateTimeOffset>().ToArray(),
        _ => _x.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "").ToArray()
    };

    private void RefreshSampleSources()
    {
        if (_xSamples is null) return;
        _xSamples.ItemsSource = XSnapshot();
        for (int i = 0; i < _ySamples.Count; i++)
            _ySamples[i].ItemsSource = _ys[i].Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray();
    }

    private StackPanel Section(string title, bool expanded)
    {
        var panel = new StackPanel { Spacing = 10 };
        var expander = new Expander { Header = title, Content = panel, IsExpanded = expanded, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        string sectionId = "ChartSection" + SettingsHost.Children.Count;
        AutomationProperties.SetAutomationId(expander, sectionId);
        if (_sectionExpansion.TryGetValue(sectionId, out bool savedExpanded)) expander.IsExpanded = savedExpanded;
        SettingsHost.Children.Add(expander);
        return panel;
    }

    private void Identify(Control control, string id, string name)
    {
        AutomationProperties.SetAutomationId(control, id);
        AutomationProperties.SetName(control, name);
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private void Hint(StackPanel panel, string text) => panel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = BrushAt(4), FontSize = 12 });
    private TextBox Text(StackPanel panel, string id, string label, string value, Action<string> changed)
    {
        var box = new TextBox { Header = label, Text = value ?? "" };
        Identify(box, id, label);
        box.TextChanged += (_, _) => { if (!_loading) Safe(() => changed(box.Text)); };
        panel.Children.Add(box);
        return box;
    }
    private ToggleSwitch Toggle(StackPanel panel, string id, string label, bool value, Action<bool> changed)
    {
        var toggle = new ToggleSwitch { Header = label, IsOn = value, OnContent = L.T("ChartsLabPage_Toggle_123"), OffContent = L.T("ChartsLabPage_Toggle_124") };
        Identify(toggle, id, label);
        toggle.Toggled += (_, _) => { if (!_loading) Safe(() => changed(toggle.IsOn)); };
        panel.Children.Add(toggle);
        return toggle;
    }
    private NumberBox Numeric(StackPanel panel, string id, string label, double value, double min, double max, Action<double> changed)
    {
        var box = new NumberBox { Header = label, Minimum = min, Maximum = max, Value = value, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, SmallChange = max <= 1 ? 0.05 : 1 };
        Identify(box, id, label);
        box.ValueChanged += (_, args) => { if (!_loading) Safe(() => changed(args.NewValue)); };
        panel.Children.Add(box);
        return box;
    }
    private ComboBox Choice(StackPanel panel, string id, string label, IEnumerable<string> values, int selected, Action<int> changed)
    {
        var box = new ComboBox { Header = label, ItemsSource = values.ToArray(), SelectedIndex = selected, PlaceholderText = ReferenceEquals(values, _brushKeys) ? L.T("ChartsLabPage_Choice_125") : L.T("ChartsLabPage_Choice_126") };
        Identify(box, id, label);
        box.SelectionChanged += (_, _) => { if (!_loading && box.SelectedIndex >= 0) Safe(() => changed(box.SelectedIndex)); };
        panel.Children.Add(box);
        return box;
    }
    private ComboBox EnumChoice<T>(StackPanel panel, string id, string label, T selected, Action<T> changed) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return Choice(panel, id, label, values.Select(v => v.ToString()), Array.IndexOf(values, selected), i => changed(values[i]));
    }
    private void DateSetting(StackPanel panel, string id, string label, DateTimeOffset? value, Action<DateTimeOffset?> changed)
    {
        var picker = new CalendarDatePicker { Header = label, Date = value, PlaceholderText = L.T("ChartsLabPage_DateSetting_127"), DateFormat = "{day.integer}.{month.integer}.{year.full}" };
        Identify(picker, id, label);
        picker.DateChanged += (_, _) => { if (!_loading) Safe(() => changed(picker.Date)); };
        panel.Children.Add(picker);
        Button(panel, id + "Clear", L.T("ChartsLabPage_DateSetting_128"), () => picker.Date = null);
    }
    private void Button(StackPanel panel, string id, string label, Action clicked)
    {
        var button = new Button { Content = label };
        Identify(button, id, label);
        button.Click += (_, _) => Safe(clicked);
        panel.Children.Add(button);
    }
    private void Safe(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            string message = $"{_diagnosticStage}: {ex.Message}";
            System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SdkComponentLab-chart.log"), message + "\n" + ex + "\n");
            SetStatus(message, InfoBarSeverity.Error);
        }
    }
    private void SetStatus(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Status.Message = message;
        Status.Severity = severity;
        AutomationProperties.SetName(Status, message);
    }

    private void PresetPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!_loading && PresetPicker.SelectedIndex >= 0) Safe(() => ApplyPreset(PresetPicker.SelectedIndex)); }
    private void ValuesList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (_pointIndex is not null && ValuesList.SelectedIndex >= 0) { _pointIndex.Value = ValuesList.SelectedIndex; ReadPoint(); } }
    private void AddPoint_Click(object sender, RoutedEventArgs e) => Safe(() => AddPoint());
    private void RemovePoint_Click(object sender, RoutedEventArgs e) => Safe(RemovePoint);
    private void Randomize_Click(object sender, RoutedEventArgs e) => Safe(() => MutateData(false));
    private void Stream_Checked(object sender, RoutedEventArgs e) { _timer.Start(); SetStatus(L.T("ChartsLabPage_Stream_Checked_129")); }
    private void Stream_Unchecked(object sender, RoutedEventArgs e) { _timer.Stop(); SetStatus(L.T("ChartsLabPage_Stream_Unchecked_130")); }
    private void AddSeries_Click(object sender, RoutedEventArgs e) => Safe(() => { AddSeries(); _selectedSeries = Plot.Series.Count - 1; RefreshSettings(); UpdateValues(); });
    private void RemoveSeries_Click(object sender, RoutedEventArgs e) => Safe(() =>
    {
        if (SelectedSeries is null) return;
        int i = Math.Clamp(_selectedSeries, 0, Plot.Series.Count - 1);
        Plot.Series.RemoveAt(i);
        Plot.Data.Remove(_ySamples[i]);
        _ySamples.RemoveAt(i);
        _ys.RemoveAt(i);
        _selectedSeries = Math.Max(0, i - 1);
        RefreshSettings();
        UpdateValues();
        SetStatus(L.F("ChartsLabPage_RemoveSeries_Click_131", (Plot.Series.Count)));
    });
    private void ClearData_Click(object sender, RoutedEventArgs e) => Safe(() => { _x.Clear(); foreach (var values in _ys) values.Clear(); foreach (var series in Plot.Series) { series.DataLabelOverrides.Clear(); series.DataMarkerOverrides.Clear(); } UpdateValues(); SetStatus(L.T("ChartsLabPage_ClearData_Click_132")); });
    private void Reset_Click(object sender, RoutedEventArgs e) => Safe(Reset);
    private void Smoke_Click(object sender, RoutedEventArgs e) => RunSmokeTests();
    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width, WorkspaceScroll.ActualHeight);
    }

    private void WorkspaceScroll_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyResponsiveLayout(Root.ActualWidth, e.NewSize.Height);

    private void ApplyResponsiveLayout(double width, double viewportHeight)
    {
        bool narrow = width < 900;
        WorkspaceScroll.VerticalScrollMode = narrow ? ScrollMode.Enabled : ScrollMode.Disabled;
        WorkspaceScroll.VerticalScrollBarVisibility = narrow ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        Workspace.Height = narrow ? Math.Max(600, viewportHeight) : viewportHeight;
        Workspace.RowDefinitions[0].Height = narrow ? new GridLength(300) : new GridLength(1, GridUnitType.Star);
        SettingsColumn.Width = narrow ? new GridLength(0) : new GridLength(350);
        SettingsRow.Height = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(SettingsScroll, narrow ? 0 : 1);
        Grid.SetRow(SettingsScroll, narrow ? 1 : 0);
        ValuesList.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
        ValuesHeading.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
        ((Grid)ValuesList.Parent).RowDefinitions[2].Height = narrow ? new GridLength(0) : new GridLength(150);
    }
}
