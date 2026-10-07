# Native WinUI TableView: API and experiments

Baseline: Windows App SDK **2.5.4-experimental**. The inventory was checked against Microsoft's IDL/specification and the installed projection. Runtime findings were verified October 7, 2026. Newer `main` sources may differ from this package.

## Official documentation

- [Release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)
- [Public API namespace](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.tabular?view=windows-app-sdk-2.0-experimental)
- [TableView IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableView.idl)
- [TableViewSource IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableViewSource.idl)
- [API specification](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/docs/api-specs/TableView/TableView-spec.md)
- [Functional specification](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/docs/design-notes/TabularControls/TableView-functional-spec.md)
- [Developer specification](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/docs/design-notes/TabularControls/TableView-dev-spec.md)
- [Interactive TableView sample](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewSampleApp)
- [C#/C++ consumer samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewApp)
- [Editing implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableView_Editing.cpp)
- [Keyboard implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableView_Keyboard.cpp)

## Integration and binding

Use `Microsoft.UI.Xaml.Controls.Tabular`, with `TabularControlsResources` merged after `XamlControlsResources`. This lab includes component resources in self-contained deployment. Missing dictionaries can cause `SortIndicatorForeground` lookup failures during first layout. Declare columns explicitly.

Models used by classic Binding are `[Bindable]` and implement `INotifyPropertyChanged`. Recycled visuals bind to inherited DataContext. The custom gauge column demonstrates public `GenerateElementCore` by creating a ProgressBar bound to Score. Do not capture a row's initial value or assign a fixed DataContext to a recycled cell.

## TableView property inventory

TableView inherits Control; normal FrameworkElement/Control styling, sizing, theming, RTL, and input properties remain available. Columns is its content property.

| Property | Default / behavior | Where to try |
| --- | --- | --- |
| ItemsSource | Collection or TableViewSource; null/empty uses EmptyTemplate | Source selector; six presets |
| Columns | Mutable vector; add/remove/insert live | Columns section and commands |
| HeadersVisibility | Column | Presentation |
| GridLinesVisibility | All | Presentation |
| CanUserResizeColumns | true; user resize gate | Presentation; header gestures |
| RowBackground / AlternatingRowBackground | null; theme background / no banding | Presentation |
| EmptyTemplate | null | Empty preset |
| GroupHeaderTemplate | null; built-in header | Grouping options |
| Density | Standard | Compact/Standard/Comfortable |
| IsReadOnly | true; editing is opt-in | Editing section |
| IsEditing | Read-only editing state | Status and inspector |
| SelectionMode | Single | Selection section |
| SelectedItem / SelectedIndex | null / -1; displayed projection | Selection commands and inspector |
| CanUserSortColumns | true; user sorting gate | Sorting options |

Dependency properties expose their static `...Property` identifiers; IsEditing is an ordinary read-only property. SelectedItem/SelectedIndex are read-only dependency properties.

CommitEdit/CancelEdit return false with no editor, a veto, or failed validation/writeback. There is no public BeginEdit or current-cell API. Select/Deselect use displayed projection indices: group headers occupy positions but cannot be selected. Selection survives shaping by **object identity**; recreating row objects does not preserve it. Duplicating the same object in a source can fail fast.

## Column inventory

| Property / method | Behavior |
| --- | --- |
| Header, HeaderTemplate, HeaderTemplateSelector | Selector takes precedence over template |
| HeaderToolTip, CellToolTipBinding | Explicit opt-in; string help text reaches UI Automation |
| Width | Pixel/Auto/Star; default 120 pixels |
| MinWidth / MaxWidth / ActualWidth | Defaults 20 / infinity / 120; sizing is clamped |
| CanResize | true |
| FrozenEdge | None; Leading supported, Trailing reserved |
| Visibility | Visible |
| CanSort / SortCycle | true / AscendingDescending |
| SortMemberPath | Empty by default; TextColumn falls back to Binding.Path |
| CustomSortComparer | Takes precedence over path; must be pure and avoid re-entry |
| SortDirection | Read-only, initially None |
| IsReadOnly | false at column level; table's gate also applies |
| CellEditingTemplate | Custom editor, including for template/custom columns |
| GenerateElement / GenerateElementCore | Public generation / protected virtual customization |
| GetSortMemberPathCore | Protected virtual sort-path discovery |

Auto measures realized cells and can grow as new rows become visible; it does not premeasure 10,000 rows. Star allocates proportional viewport space after fixed columns. User resizing changes width intent to Pixel. The lab's reorder buttons mutate Columns; native drag reorder is not provided.

TextColumn has Binding and a built-in TextBlock/TextBox pair. TemplateColumn has CellTemplate and needs CellEditingTemplate for editing. Tooltips can bind to model properties and converters. Template selectors, a custom comparer, a custom header, width constraints, hidden columns, and frozen columns are all exposed by the lab.

## Source shaping

TableViewSource is sealed. From accepts collections compatible with ItemsSourceView, including C# List/ObservableCollection. Filter, GroupBy, Sort, and their Clear methods return the same source for fluent calls. Stages are independent; use ClearFilter to remove filtering overhead. Collection notifications and source operations belong on the UI thread.

Control sorting owns a single column sort and replaces source sort axes. Source Sort replaces the control axis; successive source sorts compose with the first primary and subsequent tie-breakers. Sorting the same path replaces its axis in place; None removes it. Path sorting can show a matching header indicator; anonymous key sorting clears indicators.

Grouping has one level. String, Int32, Int64, Guid, Boolean, and enum keys have built-in identities. A reference key requires an explicit identity selector, whose argument is the **group key**. Null/empty keys, empty identities, exceptions, and ambiguous collisions can fail fast. This app coalesces blank editable categories to a localized uncategorized bucket before GroupBy.

GroupInfo provides Key, ItemCount, Level (0 in this version), IsExpandable, IsExpanded, KeyText, and ItemCountText with property notifications. A group header template replaces the content while keeping the native expander and theme band. GroupHeader exposes expansion state and ToggleRequested. TableViewRow exposes read-only IsSelected; TableViewCellsPanel is the supporting panel. The row-template selector appears in metadata but is internal to normal container ownership; replacing the internal repeater is outside the demonstrated usage.

## Events and editing

| Event arguments | Members |
| --- | --- |
| TableViewSortingEventArgs | Column (null for clear), Direction, Cancel |
| TableViewSortedEventArgs | Column (null for clear), Direction |
| TableViewBeginningEditEventArgs | Item, Column, Cancel |
| TableViewCellEditEndingEventArgs | Item, Column, EditAction, Cancel |
| SelectionChangedEventArgs | AddedItems, RemovedItems |

Cancel is read synchronously after the handler. There are no asynchronous edit deferrals. Sorting veto suppresses the change, indicator, and Sorted event. Commit veto keeps the editor open. SelectedItem and the current editing cell are independent.

Native commit finds classic Binding expressions. Use TwoWay with UpdateSourceTrigger=Explicit in an editing template so Esc cancels buffered changes. A live TwoWay checkbox in a display template can modify the source immediately; cell cancellation cannot undo arbitrary display-template side effects. This is an intentional scenario in the interactive preset.

The Score model implements INotifyDataErrorInfo and accepts 0–100. Try -1 or 101, correct the value, commit, and cancel. Native validation can restore the source snapshot and retain the editor. An asynchronous error update does not introduce an async transaction contract.

## All published enum values

| Enum | Values |
| --- | --- |
| SortDirection | None=0, Ascending=1, Descending=2 |
| TableViewFrozenEdge | None=0, Leading=1, Trailing=2 (reserved) |
| TableViewSelectionMode | None=0, Single=1 |
| TableViewHeadersVisibility | None=0, Column=1; Flags |
| TableViewGridLinesVisibility | All=0, Horizontal=1, None=2, Vertical=3 |
| TableViewDensity | Compact=0, Standard=1, Comfortable=2 |
| TableViewEditAction | Commit=0, Cancel=1 |
| TableViewSortCycle | AscendingDescending=0, AscendingDescendingNone=1, DescendingAscending=2, DescendingAscendingNone=3 |

SortCycle affects repeated header activation. The cycles ending in None include an unsorted third step.

## Keyboard and accessibility

| Gesture | Action |
| --- | --- |
| Up/Down/PageUp/PageDown | Same visible column in another displayed row |
| Left/Right/Home/End | Cell navigation |
| Ctrl+Home/Ctrl+End | First/last cell of the table |
| Enter/Space on header | Sort cycle |
| Alt+Left/Alt+Right on header | Resize; Shift increases step |
| Esc during pointer resize | Restore original width |
| Double-click/double-tap/F2 | Begin editing |
| Enter / Esc in editor | Commit / cancel |
| Enter in interactive display cell | Focus its interactive child |
| Esc from interactive child | Return to table navigation |

TableViewAutomationPeer implements selection/grid/table/item-container patterns. Row peers implement selection-item; cell peers implement grid-item/table-item/value; header peers implement invoke; group headers implement expand/collapse and grid-item. Native resource styles cover theme variants. High Contrast and screen-reader behavior were not audited in this project.

After reshaping, full traversal of experimental TableView peers can encounter stale_element. The tests query authored AutomationIds directly and retry limited read-only queries after peer replacement. Mutations are not blindly repeated. Settings precede the table in the logical tree to keep direct queries usable.

## Limits of this release

No Multiple/Extended/cell-range selection, row transactions/IEditableObject contract, public BeginEdit/current-cell API, RowEditEnding/post-edit-complete event, async editing deferral, multi-level grouping, trailing frozen implementation, native drag reorder, auto-generated columns, row headers, or column virtualization is declared by this release. Reserved enum values do not imply working functionality.

## C# signatures and binding examples

The following inventory preserves the signatures examined during research. They are grouped in the same order as the sections above; class-context snippets are reference fragments.

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <TabularControlsResources xmlns="using:Microsoft.UI.Xaml.Controls.Tabular" />
</ResourceDictionary.MergedDictionaries>
```

```csharp
bool CommitEdit();
bool CancelEdit();
void Select(int index);
void Deselect(int index);
bool IsSelected(int index);
void DeselectAll();
void ExpandAllGroups();
void CollapseAllGroups();
bool SortByColumn(TableViewColumn column, SortDirection direction);
bool ToggleSortDirection(TableViewColumn column);
bool ClearSort();

event TypedEventHandler<TableView, TableViewBeginningEditEventArgs> BeginningEdit;
event TypedEventHandler<TableView, TableViewCellEditEndingEventArgs> CellEditEnding;
event TypedEventHandler<TableView, SelectionChangedEventArgs> SelectionChanged;
event TypedEventHandler<TableView, TableViewSortingEventArgs> Sorting;
event TypedEventHandler<TableView, TableViewSortedEventArgs> Sorted;
```

```csharp
object Header { get; set; }
DataTemplate HeaderTemplate { get; set; }
DataTemplateSelector HeaderTemplateSelector { get; set; }
object HeaderToolTip { get; set; }
GridLength Width { get; set; }
double MinWidth { get; set; }
double MaxWidth { get; set; }
bool CanResize { get; set; }
double ActualWidth { get; }
TableViewFrozenEdge FrozenEdge { get; set; }
Visibility Visibility { get; set; }
bool CanSort { get; set; }
TableViewSortCycle SortCycle { get; set; }
string SortMemberPath { get; set; }
ITableViewSortComparer CustomSortComparer { get; set; }
SortDirection SortDirection { get; }
bool IsReadOnly { get; set; }
DataTemplate CellEditingTemplate { get; set; }
Binding CellToolTipBinding { get; set; }

FrameworkElement GenerateElement(object dataItem);
protected virtual FrameworkElement GenerateElementCore(object dataItem);
protected virtual string GetSortMemberPathCore();
```

```csharp
public class TableViewTextColumn : TableViewColumn
{
    public TableViewTextColumn();
    public Binding Binding { get; set; }
}
public class TableViewTemplateColumn : TableViewColumn
{
    public TableViewTemplateColumn();
    public DataTemplate CellTemplate { get; set; }
    public static DependencyProperty CellTemplateProperty { get; }
}
public interface ITableViewSortComparer
{
    int Compare(object left, object right);
}
```

```csharp
public delegate bool TableViewPredicate(object item);
public delegate object TableViewKeySelector(object item);
public delegate string TableViewIdentitySelector(object item);

public static TableViewSource From(object items);
public TableViewSource Filter(TableViewPredicate predicate);
public TableViewSource GroupBy(TableViewKeySelector keySelector);
public TableViewSource GroupBy(TableViewKeySelector keySelector,
                               TableViewIdentitySelector groupIdentitySelector);
public TableViewSource Sort(string sortMemberPath, SortDirection direction);
public TableViewSource Sort(TableViewKeySelector keySelector, SortDirection direction);
public TableViewSource ClearFilter();
public TableViewSource ClearGroupBy();
public TableViewSource ClearSort();
```

```csharp
var source = TableViewSource.From(rows);
table.ItemsSource = source;
source.Filter(new TableViewPredicate(o => ((Row)o).Score >= 70));
source.GroupBy(new TableViewKeySelector(o => ((Row)o).Category));
source.Sort(nameof(Row.Category), SortDirection.Ascending)
      .Sort(nameof(Row.Score), SortDirection.Descending);
```

```csharp
source.GroupBy(new TableViewKeySelector(o => ((Row)o).Group),
               new TableViewIdentitySelector(key => ((Group)key).Id));
```

```csharp
public sealed class TableViewGroupInfo : INotifyPropertyChanged
{
    object Key { get; }
    int ItemCount { get; }
    int Level { get; }
    bool IsExpandable { get; }
    bool IsExpanded { get; }
    string KeyText { get; }
    string ItemCountText { get; }
}
public class TableViewGroupHeader : ContentControl
{
    public TableViewGroupHeader();
    public bool IsExpanded { get; set; }
    public bool IsExpandable { get; set; }
    public event TypedEventHandler<TableViewGroupHeader,
               TableViewGroupHeaderToggleRequestedEventArgs> ToggleRequested;
    public static DependencyProperty IsExpandedProperty { get; }
    public static DependencyProperty IsExpandableProperty { get; }
}
public sealed class TableViewGroupHeaderToggleRequestedEventArgs
{
    public object GroupKey { get; }
}
public class TableViewRow : Control
{
    public TableViewRow();
    public bool IsSelected { get; }
    public static DependencyProperty IsSelectedProperty { get; }
}
public class TableViewCellsPanel : Panel { public TableViewCellsPanel(); }
```

```xml
<DataTemplate x:Key="Editor">
    <TextBox Text="{Binding Notes, Mode=TwoWay, UpdateSourceTrigger=Explicit}" />
</DataTemplate>
```

[Russian research notes](ru/table-research.md).
