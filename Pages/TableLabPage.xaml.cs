using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace SdkComponentLab.Pages;

public sealed partial class TableLabPage : Page
{
    private readonly ObservableCollection<TableLabRow> _rows = [];
    private readonly Dictionary<string, TableLabGroup> _groupKeys = [];
    private readonly List<string> _log = [];
    private readonly DispatcherTimer _readoutTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private TableViewSource _source = null!;
    private TableViewColumn? _inspectedColumn;
    private bool _ready;
    private bool _syncingColumn;
    private int _nextId;
    private int _dynamicColumnCount;
    private bool _narrowLayout;
    private bool _logExpandedBeforeCompact = true;

    public TableLabPage()
    {
        InitializeComponent();
        Table.Sorting += (_, args) =>
        {
            args.Cancel = VetoSortCheck.IsChecked == true;
            Log($"Sorting: {args.Column?.Header ?? "<clear>"} → {args.Direction}; Cancel={args.Cancel}");
        };
        Table.Sorted += (_, args) => { Log($"Sorted: {args.Column?.Header ?? "<clear>"} → {args.Direction}"); UpdateStatus(); };
        Table.SelectionChanged += (_, args) =>
        {
            Log($"SelectionChanged: +{args.AddedItems.Count} −{args.RemovedItems.Count}; index={Table.SelectedIndex}; item={(Table.SelectedItem as TableLabRow)?.Name ?? "∅"}");
            UpdateStatus();
        };
        Table.BeginningEdit += (_, args) =>
        {
            args.Cancel = VetoBeginCheck.IsChecked == true;
            Log($"BeginningEdit: {(args.Item as TableLabRow)?.Name}, {args.Column.Header}; Cancel={args.Cancel}");
            UpdateStatus();
        };
        Table.CellEditEnding += (_, args) =>
        {
            args.Cancel = args.EditAction == TableViewEditAction.Commit && VetoCommitCheck.IsChecked == true;
            Log($"CellEditEnding: {(args.Item as TableLabRow)?.Name}, {args.Column.Header}; {args.EditAction}; Cancel={args.Cancel}");
            DispatcherQueue.TryEnqueue(UpdateStatus);
        };
        _readoutTimer.Tick += (_, _) => UpdateStatus();
        Loaded += (_, _) => { SyncPresentationFromLive(); _readoutTimer.Start(); UpdateStatus(); };
        Unloaded += (_, _) => _readoutTimer.Stop();
        ResetLab();
    }

    public IEnumerable<KeyValuePair<string, object>> GetApiTargets()
    {
        yield return new("TableView", Table);
        yield return new("TableViewSource", _source);
        for (var i = 0; i < Table.Columns.Count; i++)
            yield return new(L.F("TableLabPage_GetApiTargets_001", (i), (Table.Columns[i].Header)), Table.Columns[i]);
        if (Table.SelectedItem is TableLabRow row) yield return new(L.T("TableLabPage_GetApiTargets_002"), row);
        foreach (var info in Descendants(Table).OfType<TableViewGroupHeader>().Select(h => h.Content).OfType<TableViewGroupInfo>())
            yield return new(L.F("TableLabPage_GetApiTargets_003", (info.KeyText)), info);
    }

    public void ResetLab()
    {
        _ready = false;
        Table.CancelEdit();
        VetoSortCheck.IsChecked = VetoBeginCheck.IsChecked = VetoCommitCheck.IsChecked = false;
        PresetCombo.SelectedIndex = 0;
        SourceCombo.SelectedIndex = 0;
        FilterBox.IsEnabled = HighOnlyToggle.IsEnabled = GroupCombo.IsEnabled = true;
        FilterBox.Text = "";
        HighOnlyToggle.IsOn = false;
        GroupCombo.SelectedIndex = 0;
        UserSortToggle.IsOn = UserResizeToggle.IsOn = HeadersToggle.IsOn = BandingToggle.IsOn = ToolTipsToggle.IsOn = true;
        GroupTemplateToggle.IsOn = EmptyTemplateToggle.IsOn = true;
        ReadOnlyToggle.IsOn = false;
        DensityCombo.SelectedIndex = SelectionCombo.SelectedIndex = 1;
        Table.SelectionMode = TableViewSelectionMode.Single;
        GridLinesCombo.SelectedIndex = FlowCombo.SelectedIndex = SortViaCombo.SelectedIndex = 0;
        RebuildScenario(0);
        _ready = true;
        ApplyPresentation();
        ApplyToolTips();
        SyncColumnPicker();
        Log(L.T("TableLabPage_ResetLab_004"));
        UpdateStatus();
    }

    private void RebuildScenario(int preset)
    {
        Table.CancelEdit();
        Table.ItemsSource = null;
        Table.Columns.Clear();
        _rows.Clear();
        _nextId = 0;
        var count = preset switch { 4 => 10000, 5 => 0, _ => 120 };
        for (var i = 0; i < count; i++) _rows.Add(CreateRow(i));
        _source = TableViewSource.From(_rows);
        AddTextColumn("ID", nameof(TableLabRow.Id), 70, true);
        var name = AddTextColumn(L.T("TableLabPage_RebuildScenario_005"), nameof(TableLabRow.Name), 220);
        name.FrozenEdge = TableViewFrozenEdge.Leading;
        AddTextColumn(L.T("TableLabPage_RebuildScenario_006"), nameof(TableLabRow.Category), 150);
        var score = AddTextColumn("Score", nameof(TableLabRow.Score), 90);
        score.SortCycle = TableViewSortCycle.DescendingAscendingNone;
        Table.Columns.Add(new TableViewTemplateColumn
        {
            Header = L.T("TableLabPage_RebuildScenario_007"), CellTemplate = CellTemplateResource("ProgressCell"),
            SortMemberPath = nameof(TableLabRow.Score), Width = new(160), IsReadOnly = true
        });
        Table.Columns.Add(new TableViewTemplateColumn
        {
            Header = L.T("TableLabPage_RebuildScenario_008"), CellTemplate = CellTemplateResource("NotesCell"), CellEditingTemplate = CellTemplateResource("NotesEditor"),
            SortMemberPath = nameof(TableLabRow.Notes), Width = new(1, GridUnitType.Star), MinWidth = 180
        });
        AddTextColumn(L.T("TableLabPage_RebuildScenario_009"), nameof(TableLabRow.Joined), 240, true);
        if (preset == 3)
        {
            Table.Columns.Add(new TableViewTemplateColumn { Header = L.T("TableLabPage_RebuildScenario_010"), CellTemplate = CellTemplateResource("InteractiveCell"), SortMemberPath = nameof(TableLabRow.IsActive), Width = new(160), IsReadOnly = true });
            Table.Columns.Add(new TableViewTemplateColumn { Header = L.T("TableLabPage_RebuildScenario_011"), CellTemplate = CellTemplateResource("ExpanderCell"), Width = new(260), CanSort = false, IsReadOnly = true });
            Table.Columns.Add(new TableLabGaugeColumn { Header = "Custom column", Width = new(160), SortMemberPath = nameof(TableLabRow.Score), IsReadOnly = true });
        }
        foreach (var column in Table.Columns)
        {
            if (preset == 1) column.Width = new(1, GridUnitType.Auto);
            if (preset == 2) { column.Width = new(1, GridUnitType.Star); column.MinWidth = 60; }
            column.MaxWidth = 800;
        }
        Table.ItemsSource = SourceCombo.SelectedIndex == 1 ? _rows : _source;
        _inspectedColumn = name;
    }

    private TableViewTextColumn AddTextColumn(string header, string path, double width, bool readOnly = false)
    {
        var column = new TableViewTextColumn
        {
            Header = header, Binding = new Binding { Path = new PropertyPath(path) },
            Width = new(width), IsReadOnly = readOnly, SortCycle = TableViewSortCycle.AscendingDescendingNone
        };
        Table.Columns.Add(column);
        return column;
    }

    private TableLabRow CreateRow(int index)
    {
        string[] names = [L.T("TableLabPage_CreateRow_012"), L.T("TableLabPage_CreateRow_013"), L.T("TableLabPage_CreateRow_014"), L.T("TableLabPage_CreateRow_015"), L.T("TableLabPage_CreateRow_016"), L.T("TableLabPage_CreateRow_017"), L.T("TableLabPage_CreateRow_018")];
        string[] categories = [L.T("TableLabPage_CreateRow_019"), L.T("TableLabPage_CreateRow_020"), L.T("TableLabPage_CreateRow_021"), L.T("TableLabPage_CreateRow_022")];
        var category = categories[index % categories.Length];
        if (!_groupKeys.TryGetValue(category, out var group)) _groupKeys[category] = group = new(category, category);
        return new TableLabRow
        {
            Id = ++_nextId, Name = $"{names[index % names.Length]} {index + 1}", Category = category,
            Group = group, Score = (index * 17 + 42) % 101, IsActive = index % 3 != 0,
            Joined = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.FromHours(3)).AddDays(index % 30),
            Notes = index % 7 == 0 ? L.T("TableLabPage_CreateRow_023") : L.F("TableLabPage_CreateRow_024", (index + 1))
        };
    }

    private DataTemplate CellTemplateResource(string key) => (DataTemplate)Resources[key];
    private TableViewColumn? CurrentColumn => _inspectedColumn is not null && Table.Columns.Contains(_inspectedColumn) ? _inspectedColumn : Table.Columns.FirstOrDefault();
    private bool CanChangeColumn => _ready && !_syncingColumn && CurrentColumn is not null;
    private bool HasShapingSource => SourceCombo.SelectedIndex == 0;

    private void ApplyFilter()
    {
        if (!_ready || !HasShapingSource) return;
        var text = FilterBox.Text.Trim();
        var high = HighOnlyToggle.IsOn;
        if (text.Length == 0 && !high) _source.ClearFilter();
        else _source.Filter(new TableViewPredicate(item => Matches((TableLabRow)item, text, high)));
        Log($"Filter: '{text}', Score≥70={high}");
        UpdateStatus();
    }

    private static bool Matches(TableLabRow item, string text, bool high) => (!high || item.Score >= 70) &&
        (text.Length == 0 || item.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) || item.Category.Contains(text, StringComparison.CurrentCultureIgnoreCase) || item.Notes.Contains(text, StringComparison.CurrentCultureIgnoreCase));

    private void ApplyGroup()
    {
        if (!_ready || !HasShapingSource) return;
        switch (GroupCombo.SelectedIndex)
        {
            case 1: _source.GroupBy(new TableViewKeySelector(item => CategoryBucket((TableLabRow)item))); break;
            case 2: _source.GroupBy(new TableViewKeySelector(item => ((TableLabRow)item).Score >= 70 ? "70–100" : "0–69")); break;
            case 3: _source.GroupBy(new TableViewKeySelector(item => ((TableLabRow)item).Group), new TableViewIdentitySelector(key => ((TableLabGroup)key).Id)); break;
            default: _source.ClearGroupBy(); break;
        }
        Log($"GroupBy: {GroupCombo.SelectedIndex}; reference keys use GroupIdentitySelector.");
        UpdateStatus();
    }

    private static string CategoryBucket(TableLabRow row) =>
        string.IsNullOrWhiteSpace(row.Category) ? L.T("TableLabPage_CategoryBucket_025") : row.Category;

    private void ApplyPresentation()
    {
        if (!_ready) return;
        Table.IsReadOnly = ReadOnlyToggle.IsOn;
        Table.CanUserResizeColumns = UserResizeToggle.IsOn;
        Table.CanUserSortColumns = UserSortToggle.IsOn;
        Table.HeadersVisibility = HeadersToggle.IsOn ? TableViewHeadersVisibility.Column : TableViewHeadersVisibility.None;
        Table.GridLinesVisibility = (TableViewGridLinesVisibility)Math.Max(0, GridLinesCombo.SelectedIndex);
        Table.Density = (TableViewDensity)Math.Max(0, DensityCombo.SelectedIndex);
        Table.FlowDirection = FlowCombo.SelectedIndex == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Table.GroupHeaderTemplate = GroupTemplateToggle.IsOn ? CellTemplateResource("GroupHeader") : null;
        Table.EmptyTemplate = EmptyTemplateToggle.IsOn ? CellTemplateResource("EmptyState") : null;
        if (BandingToggle.IsOn)
        {
            Table.SetThemeResourceBinding(Control.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            Table.SetThemeResourceBinding(TableView.RowBackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            Table.SetThemeResourceBinding(TableView.AlternatingRowBackgroundProperty, "SubtleFillColorSecondaryBrush");
        }
        else
        {
            Table.ClearValue(TableView.RowBackgroundProperty);
            Table.ClearValue(TableView.AlternatingRowBackgroundProperty);
        }
        UpdateStatus();
    }

    private void ApplyToolTips()
    {
        if (!_ready) return;
        foreach (var column in Table.Columns)
        {
            column.HeaderToolTip = ToolTipsToggle.IsOn ? L.F("TableLabPage_ApplyToolTips_026", (column.Header)) : null;
            column.CellToolTipBinding = ToolTipsToggle.IsOn ? new Binding { Path = new PropertyPath(nameof(TableLabRow.ToolTipText)) } : null;
        }
        Log($"HeaderToolTip + CellToolTipBinding = {ToolTipsToggle.IsOn}");
    }

    private void SyncPresentationFromLive()
    {
        // Navigation back from the API inspector must reflect its mutations, not overwrite them.
        var wasReady = _ready;
        _ready = false;
        try
        {
            ReadOnlyToggle.IsOn = Table.IsReadOnly;
            UserResizeToggle.IsOn = Table.CanUserResizeColumns;
            UserSortToggle.IsOn = Table.CanUserSortColumns;
            HeadersToggle.IsOn = Table.HeadersVisibility == TableViewHeadersVisibility.Column;
            GridLinesCombo.SelectedIndex = (int)Table.GridLinesVisibility;
            DensityCombo.SelectedIndex = (int)Table.Density;
            SelectionCombo.SelectedIndex = (int)Table.SelectionMode;
            FlowCombo.SelectedIndex = Table.FlowDirection == FlowDirection.RightToLeft ? 1 : 0;
            GroupTemplateToggle.IsOn = Table.GroupHeaderTemplate is not null;
            EmptyTemplateToggle.IsOn = Table.EmptyTemplate is not null;
            BandingToggle.IsOn = Table.AlternatingRowBackground is not null;
            ToolTipsToggle.IsOn = Table.Columns.Any(c => c.HeaderToolTip is not null || c.CellToolTipBinding is not null);
            SourceCombo.SelectedIndex = Table.ItemsSource is TableViewSource ? 0 : 1;
            if (Table.ItemsSource is TableViewSource source) _source = source;
            FilterBox.IsEnabled = HighOnlyToggle.IsEnabled = GroupCombo.IsEnabled = HasShapingSource;
            SyncColumnPicker();
        }
        finally { _ready = wasReady; }
    }

    private void SyncColumnPicker()
    {
        _syncingColumn = true;
        ColumnCombo.Items.Clear();
        foreach (var column in Table.Columns) ColumnCombo.Items.Add(column.Header?.ToString() ?? L.T("TableLabPage_SyncColumnPicker_027"));
        ColumnCombo.SelectedIndex = _inspectedColumn is not null ? Table.Columns.IndexOf(_inspectedColumn) : 0;
        if (ColumnCombo.SelectedIndex < 0 && Table.Columns.Count > 0) ColumnCombo.SelectedIndex = 0;
        _inspectedColumn = ColumnCombo.SelectedIndex >= 0 ? Table.Columns[ColumnCombo.SelectedIndex] : null;
        _syncingColumn = false;
        SyncColumnEditor();
    }

    private void SyncColumnEditor()
    {
        var column = CurrentColumn;
        if (column is null) return;
        _syncingColumn = true;
        HeaderBox.Text = column.Header?.ToString() ?? "";
        WidthCombo.SelectedIndex = column.Width.GridUnitType switch { GridUnitType.Auto => 1, GridUnitType.Star => 2, _ => 0 };
        WidthValue.Value = column.Width.Value;
        WidthValue.IsEnabled = WidthCombo.SelectedIndex != 1;
        MinWidthBox.Value = column.MinWidth;
        MaxWidthBox.Value = double.IsFinite(column.MaxWidth) ? column.MaxWidth : 800;
        ColumnVisibleToggle.IsOn = column.Visibility == Visibility.Visible;
        FrozenToggle.IsOn = column.FrozenEdge == TableViewFrozenEdge.Leading;
        ColumnResizeToggle.IsOn = column.CanResize;
        ColumnSortToggle.IsOn = column.CanSort;
        ColumnReadOnlyToggle.IsOn = column.IsReadOnly;
        SortCycleCombo.SelectedIndex = (int)column.SortCycle;
        ComparerCombo.SelectedIndex = column.CustomSortComparer is TableLabComparer comparer ? comparer.Mode : 0;
        SortPathBox.Text = column.SortMemberPath ?? "";
        HeaderTemplateCombo.SelectedIndex = column.HeaderTemplateSelector is not null ? 2 : column.HeaderTemplate is not null ? 1 : 0;
        _syncingColumn = false;
        UpdateStatus();
    }

    private void ApplyColumnWidth()
    {
        if (!CanChangeColumn || !double.IsFinite(WidthValue.Value)) return;
        var column = CurrentColumn!;
        var kind = WidthCombo.SelectedIndex switch { 1 => GridUnitType.Auto, 2 => GridUnitType.Star, _ => GridUnitType.Pixel };
        column.Width = new GridLength(kind == GridUnitType.Auto ? 1 : Math.Max(0, WidthValue.Value), kind);
        WidthValue.IsEnabled = kind != GridUnitType.Auto;
        UpdateStatus();
    }

    private void ApplySort(SortDirection direction)
    {
        var column = CurrentColumn;
        if (!_ready || column is null) return;
        if (SortViaCombo.SelectedIndex == 0)
            Log($"SortByColumn returned {Table.SortByColumn(column, direction)}");
        else if (!HasShapingSource) Log(L.T("TableLabPage_ApplySort_028"));
        else if (SortViaCombo.SelectedIndex == 1)
        {
            var path = column.SortMemberPath;
            if (string.IsNullOrEmpty(path) && column is TableViewTextColumn text) path = text.Binding?.Path?.Path;
            if (string.IsNullOrEmpty(path)) { Log(L.T("TableLabPage_ApplySort_029")); return; }
            _source.ClearSort().Sort(path, direction);
            Log(L.F("TableLabPage_ApplySort_030", (path), (direction)));
        }
        else
        {
            _source.ClearSort().Sort(new TableViewKeySelector(item => ((TableLabRow)item).Name.Length), direction);
            Log(L.F("TableLabPage_ApplySort_031", (direction)));
        }
        UpdateStatus();
    }

    private void MoveColumn(int delta)
    {
        var column = CurrentColumn;
        if (column is null) return;
        var index = Table.Columns.IndexOf(column);
        var target = index + delta;
        if (target < 0 || target >= Table.Columns.Count) return;
        Table.CancelEdit();
        Table.Columns.RemoveAt(index);
        Table.Columns.Insert(target, column);
        SyncColumnPicker();
        Log($"Columns.RemoveAt({index}) + Insert({target}); {column.Header}");
    }

    private void UpdateStatus()
    {
        if (!_ready || TableStatus is null) return;
        var selected = Table.SelectedItem as TableLabRow;
        var shown = HasShapingSource ? _rows.Count(r => Matches(r, FilterBox.Text.Trim(), HighOnlyToggle.IsOn)) : _rows.Count;
        var sorted = string.Join(", ", Table.Columns.Where(c => c.SortDirection != SortDirection.None).Select(c => $"{c.Header}: {c.SortDirection}"));
        TableStatus.Text = L.F("TableLabPage_UpdateStatus_032", (shown), (_rows.Count), (Table.Columns.Count), (Table.IsEditing), (selected?.Name ?? "∅"), (Table.SelectedIndex), ((sorted.Length == 0 ? "∅" : sorted)));
        if (CurrentColumn is { } column)
            ActualWidthText.Text = $"ActualWidth={column.ActualWidth:F1}px; intent={column.Width.GridUnitType} {column.Width.Value:F1}\nSortDirection={column.SortDirection}";
    }

    private void Log(string text)
    {
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {text}");
        if (_log.Count > 80) _log.RemoveRange(80, _log.Count - 80);
        if (EventLog is not null) EventLog.Text = string.Join(Environment.NewLine, _log);
    }

    public string RunSmokeTests()
    {
        ResetLab();
        var passed = new List<string>();
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException($"TableView smoke: {name}");
            passed.Add(name);
        }
        Check(Table.Columns.Count == 7 && _rows.Count == 120, L.T("TableLabPage_RunSmokeTests_033"));
        Table.Select(2);
        Check(Table.IsSelected(2) && ReferenceEquals(Table.SelectedItem, _rows[2]), "Select / SelectedItem / SelectedIndex / IsSelected");
        Table.Deselect(2);
        Check(Table.SelectedIndex == -1, "Deselect");
        Table.Select(0);
        Table.DeselectAll();
        Check(Table.SelectedItem is null, "DeselectAll");
        var column = Table.Columns[1];
        Check(Table.SortByColumn(column, SortDirection.Descending) && column.SortDirection == SortDirection.Descending, "SortByColumn");
        Check(Table.ToggleSortDirection(column) && column.SortDirection == SortDirection.None, "ToggleSortDirection / SortCycle");
        Table.SortByColumn(column, SortDirection.Ascending);
        Check(Table.ClearSort() && column.SortDirection == SortDirection.None, "ClearSort");
        _source.Filter(new TableViewPredicate(item => ((TableLabRow)item).Score >= 70)).ClearFilter();
        passed.Add("TableViewSource.Filter / ClearFilter");
        _source.GroupBy(new TableViewKeySelector(item => CategoryBucket((TableLabRow)item)));
        Table.CollapseAllGroups(); Table.ExpandAllGroups(); _source.ClearGroupBy();
        passed.Add("GroupBy / CollapseAllGroups / ExpandAllGroups / ClearGroupBy");
        _source.GroupBy(new TableViewKeySelector(item => ((TableLabRow)item).Group), new TableViewIdentitySelector(key => ((TableLabGroup)key).Id)).ClearGroupBy();
        passed.Add("GroupBy(key, identity)");
        _source.Sort(nameof(TableLabRow.Category), SortDirection.Ascending).Sort(nameof(TableLabRow.Score), SortDirection.Descending).ClearSort();
        _source.Sort(new TableViewKeySelector(item => ((TableLabRow)item).Name.Length), SortDirection.Descending).ClearSort();
        passed.Add("Sort(path) / multiple axes / Sort(key) / ClearSort");
        _rows.Add(CreateRow(_rows.Count));
        _rows.RemoveAt(_rows.Count - 1);
        passed.Add("ObservableCollection add / remove");
        Check(!Table.CommitEdit() && !Table.CancelEdit(), L.T("TableLabPage_RunSmokeTests_034"));
        ResetLab();
        var report = L.F("TableLabPage_RunSmokeTests_035", (passed.Count)) + string.Join("\n", passed);
        Log(report);
        return report;
    }

    private void Preset_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        RebuildScenario(PresetCombo.SelectedIndex);
        ApplyPresentation(); ApplyToolTips(); SyncColumnPicker(); ApplyFilter(); ApplyGroup();
        Log(L.F("TableLabPage_Preset_Changed_036", (PresetCombo.SelectedIndex), (_rows.Count)));
    }
    private void Source_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        Table.CancelEdit();
        Table.ItemsSource = HasShapingSource ? _source : _rows;
        FilterBox.IsEnabled = HighOnlyToggle.IsEnabled = GroupCombo.IsEnabled = HasShapingSource;
        if (HasShapingSource) { ApplyFilter(); ApplyGroup(); }
        Log($"ItemsSource: {(HasShapingSource ? "TableViewSource" : L.T("TableLabPage_Source_Changed_037"))}");
        UpdateStatus();
    }
    private void Filter_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Filter_Toggled(object sender, RoutedEventArgs e) => ApplyFilter();
    private void Group_Changed(object sender, SelectionChangedEventArgs e) => ApplyGroup();
    private void Presentation_Changed(object sender, RoutedEventArgs e) => ApplyPresentation();
    private void PresentationCombo_Changed(object sender, SelectionChangedEventArgs e) => ApplyPresentation();
    private void ToolTips_Changed(object sender, RoutedEventArgs e) => ApplyToolTips();
    private void Column_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _syncingColumn || ColumnCombo.SelectedIndex < 0) return;
        _inspectedColumn = Table.Columns[ColumnCombo.SelectedIndex]; SyncColumnEditor();
    }
    private void ColumnHeader_Changed(object sender, TextChangedEventArgs e)
    {
        if (!CanChangeColumn) return;
        CurrentColumn!.Header = HeaderBox.Text;
        var index = Table.Columns.IndexOf(CurrentColumn);
        _syncingColumn = true;
        ColumnCombo.Items[index] = HeaderBox.Text;
        ColumnCombo.SelectedIndex = index;
        _syncingColumn = false;
    }
    private void ColumnWidth_Changed(object sender, SelectionChangedEventArgs e) => ApplyColumnWidth();
    private void ColumnWidthValue_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args) => ApplyColumnWidth();
    private void ColumnLimits_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!CanChangeColumn || !double.IsFinite(MinWidthBox.Value) || !double.IsFinite(MaxWidthBox.Value)) return;
        var column = CurrentColumn!;
        column.MinWidth = Math.Max(0, MinWidthBox.Value);
        column.MaxWidth = Math.Max(column.MinWidth, MaxWidthBox.Value);
        UpdateStatus();
    }
    private void ColumnFlags_Changed(object sender, RoutedEventArgs e)
    {
        if (!CanChangeColumn) return;
        var column = CurrentColumn!;
        column.Visibility = ColumnVisibleToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        column.FrozenEdge = FrozenToggle.IsOn ? TableViewFrozenEdge.Leading : TableViewFrozenEdge.None;
        column.CanResize = ColumnResizeToggle.IsOn;
        column.CanSort = ColumnSortToggle.IsOn;
        column.IsReadOnly = ColumnReadOnlyToggle.IsOn;
        UpdateStatus();
    }
    private void ColumnSort_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!CanChangeColumn) return;
        CurrentColumn!.SortCycle = (TableViewSortCycle)Math.Max(0, SortCycleCombo.SelectedIndex);
        CurrentColumn.CustomSortComparer = ComparerCombo.SelectedIndex == 0 ? null : new TableLabComparer(ComparerCombo.SelectedIndex);
    }
    private void ColumnPath_Changed(object sender, TextChangedEventArgs e) { if (CanChangeColumn) CurrentColumn!.SortMemberPath = SortPathBox.Text; }
    private void ColumnTemplate_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!CanChangeColumn) return;
        CurrentColumn!.HeaderTemplate = HeaderTemplateCombo.SelectedIndex == 1 ? CellTemplateResource("IconHeader") : null;
        CurrentColumn.HeaderTemplateSelector = HeaderTemplateCombo.SelectedIndex == 2 ? new TableLabHeaderSelector { Normal = CellTemplateResource("IconHeader"), Emphasis = CellTemplateResource("EmphasisHeader") } : null;
    }
    private void SelectionMode_Changed(object sender, SelectionChangedEventArgs e)
    { if (_ready) { Table.SelectionMode = (TableViewSelectionMode)Math.Max(0, SelectionCombo.SelectedIndex); UpdateStatus(); } }
    private void Ascending_Click(object sender, RoutedEventArgs e) => ApplySort(SortDirection.Ascending);
    private void Descending_Click(object sender, RoutedEventArgs e) => ApplySort(SortDirection.Descending);
    private void ClearSort_Click(object sender, RoutedEventArgs e) { Log($"ClearSort returned {Table.ClearSort()}"); UpdateStatus(); }
    private void Cycle_Click(object sender, RoutedEventArgs e) { if (CurrentColumn is { } column) Log($"ToggleSortDirection returned {Table.ToggleSortDirection(column)}"); UpdateStatus(); }
    private void MultiSort_Click(object sender, RoutedEventArgs e)
    {
        if (!HasShapingSource) { Log(L.T("TableLabPage_MultiSort_Click_038")); return; }
        _source.ClearSort().Sort(nameof(TableLabRow.Category), SortDirection.Ascending).Sort(nameof(TableLabRow.Score), SortDirection.Descending);
        Log("Source.Sort: Category ASC primary, Score DESC tie-breaker."); UpdateStatus();
    }
    private void Expand_Click(object sender, RoutedEventArgs e) { Table.ExpandAllGroups(); Log("ExpandAllGroups()"); }
    private void Collapse_Click(object sender, RoutedEventArgs e) { Table.CollapseAllGroups(); Log("CollapseAllGroups()"); }
    private void MoveLeft_Click(object sender, RoutedEventArgs e) => MoveColumn(-1);
    private void MoveRight_Click(object sender, RoutedEventArgs e) => MoveColumn(1);
    private void AddColumn_Click(object sender, RoutedEventArgs e)
    {
        _inspectedColumn = AddTextColumn($"Name copy {++_dynamicColumnCount}", nameof(TableLabRow.Name), 180);
        ApplyToolTips(); SyncColumnPicker(); Log(L.T("TableLabPage_AddColumn_Click_039"));
    }
    private void RemoveColumn_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentColumn is not { } column) return;
        Table.CancelEdit(); Table.Columns.Remove(column); _inspectedColumn = null; SyncColumnPicker(); Log("Columns.Remove");
    }
    private int SelectionIndex => double.IsFinite(SelectIndexBox.Value) ? (int)SelectIndexBox.Value : 0;
    private void Select_Click(object sender, RoutedEventArgs e) { Table.Select(SelectionIndex); Log($"Select({SelectionIndex}); IsSelected={Table.IsSelected(SelectionIndex)}"); UpdateStatus(); }
    private void Deselect_Click(object sender, RoutedEventArgs e) { Table.Deselect(SelectionIndex); UpdateStatus(); }
    private void DeselectAll_Click(object sender, RoutedEventArgs e) { Table.DeselectAll(); UpdateStatus(); }
    private void Commit_Click(object sender, RoutedEventArgs e) { Log($"CommitEdit()={Table.CommitEdit()}"); UpdateStatus(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { Log($"CancelEdit()={Table.CancelEdit()}"); UpdateStatus(); }
    private void AddRow_Click(object sender, RoutedEventArgs e) { _rows.Add(CreateRow(_rows.Count)); Log("ObservableCollection.Add"); UpdateStatus(); }
    private void MutateRow_Click(object sender, RoutedEventArgs e)
    {
        var row = Table.SelectedItem as TableLabRow ?? _rows.FirstOrDefault();
        if (row is null) return;
        row.Score = (row.Score + 13) % 101; row.Name += " •"; row.Notes = L.T("TableLabPage_MutateRow_Click_040") + row.Notes;
        Log($"INotifyPropertyChanged: ID={row.Id}; Score={row.Score}"); UpdateStatus();
    }
    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        var row = Table.SelectedItem as TableLabRow ?? _rows.LastOrDefault();
        if (row is null) return;
        _rows.Remove(row); Log($"ObservableCollection.Remove: ID={row.Id}"); UpdateStatus();
    }
    private void Reset_Click(object sender, RoutedEventArgs e) => ResetLab();
    private void Smoke_Click(object sender, RoutedEventArgs e) { try { RunSmokeTests(); } catch (Exception ex) { Log("FAIL: " + ex.Message); } }
    private void ClearLog_Click(object sender, RoutedEventArgs e) { _log.Clear(); EventLog.Text = ""; }
    private void Layout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width, WorkspaceScroll.ActualHeight);
    }
    private void WorkspaceScroll_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyResponsiveLayout(LayoutRoot.ActualWidth, e.NewSize.Height);
    private void ApplyResponsiveLayout(double width, double viewportHeight)
    {
        if (SettingsScroll is null || EventLogSection is null) return;
        var narrow = width < 850;
        if (narrow != _narrowLayout)
        {
            if (narrow) { _logExpandedBeforeCompact = EventLogSection.IsExpanded; EventLogSection.IsExpanded = false; }
            else EventLogSection.IsExpanded = _logExpandedBeforeCompact;
            _narrowLayout = narrow;
        }
        WorkspaceScroll.VerticalScrollMode = narrow ? ScrollMode.Enabled : ScrollMode.Disabled;
        WorkspaceScroll.VerticalScrollBarVisibility = narrow ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        Workspace.Height = narrow ? Math.Max(600, viewportHeight) : viewportHeight;
        Workspace.RowDefinitions[0].Height = narrow ? new(300) : new(1, GridUnitType.Star);
        InspectorColumn.Width = narrow ? new(0) : new(330);
        InspectorRow.Height = narrow ? new(1, GridUnitType.Star) : new(0);
        Grid.SetRow(SettingsScroll, narrow ? 1 : 0);
        Grid.SetColumn(SettingsScroll, narrow ? 0 : 1);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class TableLabRow : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private string _name = "", _category = "", _notes = "";
    private int _score;
    private bool _active;
    public int Id { get; set; }
    public string Name { get => _name; set => Set(ref _name, value); }
    public string Category { get => _category; set => Set(ref _category, value); }
    public string Notes { get => _notes; set => Set(ref _notes, value); }
    public int Score
    {
        get => _score;
        set
        {
            if (!Set(ref _score, value)) return;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Score)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
        }
    }
    public bool IsActive { get => _active; set => Set(ref _active, value); }
    public DateTimeOffset Joined { get; set; }
    public TableLabGroup Group { get; set; } = new("default", L.T("TableLabPage_Text_041"));
    public string ToolTipText => $"#{Id} · {Name}\n{Category} · Score {Score}\n{Notes}";
    public bool HasErrors => Score is < 0 or > 100;
    public IEnumerable GetErrors(string? propertyName) => (string.IsNullOrEmpty(propertyName) || propertyName == nameof(Score)) && HasErrors ? new[] { L.T("TableLabPage_GetErrors_042") } : Array.Empty<string>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(property));
        PropertyChanged?.Invoke(this, new(nameof(ToolTipText)));
        return true;
    }
}

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class TableLabGroup(string id, string name)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public override string ToString() => Name;
}

public sealed class TableLabComparer(int mode) : ITableViewSortComparer
{
    public int Mode { get; } = mode;
    public int Compare(object left, object right)
    {
        var a = (TableLabRow)left;
        var b = (TableLabRow)right;
        var primary = Mode == 1 ? a.Name.Length.CompareTo(b.Name.Length) : a.Score.CompareTo(b.Score);
        return primary != 0 ? primary : StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
    }
}

public sealed class TableLabHeaderSelector : DataTemplateSelector
{
    public DataTemplate Normal { get; set; } = null!;
    public DataTemplate Emphasis { get; set; } = null!;
    protected override DataTemplate SelectTemplateCore(object item) => item?.ToString()?.Contains("Score", StringComparison.Ordinal) == true ? Emphasis : Normal;
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}

public sealed class TableLabGaugeColumn : TableViewColumn
{
    protected override FrameworkElement GenerateElementCore(object dataItem)
    {
        // A recycled row changes inherited DataContext. Never capture dataItem in a local value.
        var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 8, Margin = new(8), VerticalAlignment = VerticalAlignment.Center };
        bar.SetBinding(ProgressBar.ValueProperty, new Binding { Path = new PropertyPath(nameof(TableLabRow.Score)) });
        AutomationProperties.SetName(bar, L.T("TableLabPage_GenerateElementCore_043"));
        return bar;
    }
}
