# TableView — исследование Windows App SDK 2.5.4-Experimental

Проверено 6 октября 2026. Выпуск от 29 сентября 2026 впервые публикует эти типы в public WinMD. Реализация лаборатории скомпилирована с пакетом `Microsoft.WindowsAppSDK 2.5.4-experimental`; ниже перечислены APIs этого семейства, сверенные с официальным IDL и API spec. Исходники ветки `main` могут содержать исправления после выпуска; наличие API дополнительно проверяет сборка и встроенный инспектор установленной сборки.

## Официальные источники

- [Release notes 2.5.4-Experimental](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)
- [TableView API spec / практические рекомендации](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/api-specs/TableView/TableView-spec.md)
- [TableView.idl — точный контракт](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView.idl)
- [TableViewSource.idl — преобразования данных](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableViewSource.idl)
- [Functional spec](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/TabularControls/TableView-functional-spec.md)
- [Developer spec](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/TabularControls/TableView-dev-spec.md)
- [Полная лаборатория Microsoft](https://github.com/microsoft/microsoft-ui-xaml/tree/main/Samples/TableViewSampleApp)
- [Packaged / unpackaged C# и C++ минимальные приложения Microsoft](https://github.com/microsoft/microsoft-ui-xaml/tree/main/Samples/TableViewApp)
- [API reference namespace](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.tabular?view=windows-app-sdk-2.0-experimental)
- [GenerateElementCore C# signature](https://learn.microsoft.com/zh-cn/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.tabular.tableviewcolumn.generateelementcore?view=windows-app-sdk-2.0-experimental)
- [Keyboard implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView_Keyboard.cpp)
- [Editing implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView_Editing.cpp)
- [Editing gestures](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView_EditingInput.cpp)
- [Column implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView_Columns.cpp)
- [Theme resources](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TableView/TableView_themeresources.xaml)

Новые компоненты также находятся через WinApp 0.7.1 `find-ui`: `gallery-tableview-1`, `gallery-tableview-2`, `gallery-tableview-5`. Глобальный WinApp в этой машине старее; новый CLI установлен в NuGet `Microsoft.Windows.SDK.BuildTools.WinApp/0.7.1/tools/win-x64`.

## Подключение

Namespace: `Microsoft.UI.Xaml.Controls.Tabular`, XAML: `xmlns:tabular="using:Microsoft.UI.Xaml.Controls.Tabular"`.

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <TabularControlsResources xmlns="using:Microsoft.UI.Xaml.Controls.Tabular" />
</ResourceDictionary.MergedDictionaries>
```

`TabularControlsResources` необходим: стандартный заголовок разрешает `SortIndicatorForeground` из этой dictionary. Без него можно получить ошибку активации. Тип `Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsTabularXamlMetaDataProvider` также входит в SDK; generated XAML metadata provider приложения связывается с provider нового компонента при сборке обычного WinUI приложения.

Модели C# для classic `{Binding}` должны быть доступны XAML metadata / custom property provider. В лаборатории модели помечены `[Microsoft.UI.Xaml.Data.Bindable]`, свойства mutable и реализуют `INotifyPropertyChanged`. Шаблоны остаются реактивными при recycling: они используют наследуемый `DataContext`, не запоминают переданный при создании объект.

## TableView

Наследуется от `Microsoft.UI.Xaml.Controls.Control`, `[Experimental]`, unsealed. `Columns` — content property. Стандартные свойства `Control`/`FrameworkElement` доступны дополнительно: `FontSize`, `FontFamily`, `Foreground`, `Background`, `BorderBrush`, `BorderThickness`, `Padding`, `CornerRadius`, `FlowDirection`, `Style`, `Template`, `RequestedTheme`, `IsEnabled`, размеры и XAML input events.

| Свойство C# | Значение по умолчанию / поведение |
|---|---|
| `object ItemsSource { get; set; }` | Принимает обычную коллекцию или `TableViewSource`; null/empty показывает EmptyTemplate |
| `IList<TableViewColumn> Columns { get; }` | WinRT `IVector<TableViewColumn>` с observable backing; live Add/Remove/Insert |
| `TableViewHeadersVisibility HeadersVisibility { get; set; }` | `Column` |
| `TableViewGridLinesVisibility GridLinesVisibility { get; set; }` | `All` |
| `bool CanUserResizeColumns { get; set; }` | true; master gate пользовательского resize |
| `Brush RowBackground { get; set; }` | null сохраняет фон темы |
| `Brush AlternatingRowBackground { get; set; }` | null отключает banding |
| `DataTemplate EmptyTemplate { get; set; }` | null |
| `DataTemplate GroupHeaderTemplate { get; set; }` | null: built-in header |
| `TableViewDensity Density { get; set; }` | Standard |
| `bool IsReadOnly { get; set; }` | true: editing opt-in |
| `bool IsEditing { get; }` | editor открыт / открывается / закрывается |
| `TableViewSelectionMode SelectionMode { get; set; }` | Single |
| `object SelectedItem { get; }` | null, coherent с SelectedIndex |
| `int SelectedIndex { get; }` | −1; индекс displayed projection |
| `bool CanUserSortColumns { get; set; }` | true; programmatic sort работает при false |

Все DP имеют одноимённую статическую `DependencyProperty ...Property`, кроме `IsEditing`. `ItemsSource`, `Columns`, visibility/gridlines, banding, templates, density, readonly, selection и sort gates — DP; SelectedItem/SelectedIndex get-only DP.

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

Программное начало editing отсутствует. Commit/Cancel возвращают false без editor, при veto или неуспешной validation/writeback. Публичного current-cell API в этом релизе нет.

`Select`/`Deselect` индексируют displayed projection: group headers занимают индексы, но не выбираются. Selection сохраняется при reshaping через object identity; это не stable key модели. Re-creating row objects не сохраняет selection. Один и тот же object дважды в источнике вызывает fail-fast.

## TableViewColumn

Наследуется от `DependencyObject`, unsealed; constructor доступен для custom columns; content property `Header`.

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
Binding CellToolTipBinding { get; set; }  // CLR property, Binding передаётся unevaluated

FrameworkElement GenerateElement(object dataItem);
protected virtual FrameworkElement GenerateElementCore(object dataItem);
protected virtual string GetSortMemberPathCore();
```

Defaults: `Width=120 pixels`, `MinWidth=20`, `MaxWidth=+∞`, `ActualWidth=120`, `CanResize=true`, `Visibility=Visible`, `FrozenEdge=None`, `CanSort=true`, `SortCycle=AscendingDescending`, `SortDirection=None`, `IsReadOnly=false`.

`HeaderTemplateSelector` имеет приоритет над `HeaderTemplate`. Tooltip null/empty выключен, автоматического показа tooltip для clipped cell нет: это явный opt-in. Cell tooltip binding вычисляется относительно row model, может использовать converter и даёт automation HelpText для string content.

`GridLength.Pixel` — заданная ширина с Min/Max clamp. `Auto` — самая широкая реализованная ячейка, растёт по мере реализации новых строк в данном dataset. Это не полный предварительный проход по 10 000 строкам. `Star` — пропорциональная доля viewport после fixed columns, тоже clamped. Пользовательский resize переводит intent в Pixel. В этом релизе есть leading pinning; значение Trailing опубликовано, но зарезервировано.

SortMemberPath по умолчанию пустой. TextColumn использует `Binding.Path.Path` при пустом SortMemberPath. Базовый столбец без path или CustomSortComparer не имеет выражаемого sort key. Comparer приоритетнее path. Несколько столбцов могут иметь одинаковый path: индикатор keyed по column identity.

GenerateElementCore должен возвращать visual с реактивными bindings, не local DataContext и не статическую копию `dataItem`. Лаборатория содержит `TableLabGaugeColumn`, который создаёт ProgressBar и binding Score: это проверяет реальное наследование публичного базового column API.

DP fields: `HeaderProperty`, `HeaderTemplateProperty`, `HeaderTemplateSelectorProperty`, `HeaderToolTipProperty`, `WidthProperty`, `MinWidthProperty`, `MaxWidthProperty`, `CanResizeProperty`, `ActualWidthProperty`, `FrozenEdgeProperty`, `VisibilityProperty`, `IsReadOnlyProperty`, `CellEditingTemplateProperty`, `CanSortProperty`, `SortCycleProperty`, `SortMemberPathProperty`, `SortDirectionProperty`. `Binding`, `CellToolTipBinding`, `CustomSortComparer` передаются как CLR/WinRT property surfaces.

### Text / template columns

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

TextColumn создаёт TextBlock и built-in TextBox editor. Editor переносит Path/converter/fallback/source selectors, но принудительно использует TwoWay и Explicit для writeback на commit. CellEditingTemplate может заменить built-in editor. TemplateColumn без editing template не editable. Custom column edit hooks не опубликованы: используйте CellEditingTemplate.

## TableViewSource и delegates

Класс sealed; методов нет для наследования/собственного источника. From принимает коллекции, совместимые с ItemsSourceView: `IVector<object>`, `IObservableVector<object>`, `IBindableVector`, `IIterable<object>`, `IBindableIterable`. В C# подходят List/ObservableCollection.

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

Методы возвращают тот же source для fluent chain. Filter, GroupBy и Sort — независимые стадии; заменять несменившиеся stage на каждый ввод не нужно. ClearFilter удаляет stage, pass-everything predicate оставляет ненужный обход. Null items/predicate/keySelector и пустой path дают `E_INVALIDARG`. Source и collection notifications привязаны к UI thread.

```csharp
var source = TableViewSource.From(rows);
table.ItemsSource = source;
source.Filter(new TableViewPredicate(o => ((Row)o).Score >= 70));
source.GroupBy(new TableViewKeySelector(o => ((Row)o).Category));
source.Sort(nameof(Row.Category), SortDirection.Ascending)
      .Sort(nameof(Row.Score), SortDirection.Descending);
```

**Sorting ownership:** control SortByColumn выполняет single-column sort и заменяет source axes. Source Sort заменяет control axis; несколько successive source axes compose, первый primary, следующие tie-breaker. Повторный sort того же path заменяет axis на месте. `SortDirection.None` удаляет axis. Path form поддерживает те же dotted paths/indexers, что binding resolver; TableView может сопоставить path столбцу и показать glyph. KeySelector form anonymous: glyphs очищаются, поскольку нет named property. Comparer должен быть чистым и не re-enter control.

**Grouping:** один уровень. Значимые built-in identity types: String, Int32, Int64, Guid, Boolean, enum. Null, empty string, reference key без explicit identity selector, empty returned identity, identity-selector exception и ambiguous accidental identity collision вызывают fail-fast. Blank categories надо заранее coalesce в реальный bucket вроде `(нет)`. Для reference group keys:

```csharp
source.GroupBy(new TableViewKeySelector(o => ((Row)o).Group),
               new TableViewIdentitySelector(key => ((Group)key).Id));
```

Identity delegate получает **group key**, а не row item. Лаборатория отдельно демонстрирует этот вариант.

Category — редактируемое поле. Поэтому Category GroupBy в лаборатории преобразует null/empty/whitespace в `(без категории)` перед передачей в SDK. Это предотвращает fail-fast из-за неразрешимой пустой group identity при редактировании строки. Smoke grouping использует тот же selector.

## Group headers и служебные визуальные типы

```csharp
public sealed class TableViewGroupInfo : INotifyPropertyChanged
{
    object Key { get; }
    int ItemCount { get; }
    int Level { get; }                  // в v1 всегда 0
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

GroupInfo является DataContext/content GroupHeaderTemplate и обновляется in-place через PropertyChanged. Шаблон меняет контент области, штатный expander и themed band сохраняются. Клик по всей header band, клавиатура и UIA ExpandCollapse toggles поддерживаются.

TableViewRowTemplateSelector фигурирует в release API list, но официальный main IDL маркирует его MUX_INTERNAL и app-facing use не предполагает: owning table назначает контейнеры. Самостоятельная замена внутреннего repeater/row template выходит за обычный supported usage.

## Events и аргументы

| Тип | Свойства |
|---|---|
| `TableViewSortingEventArgs` | `TableViewColumn Column {get;}` (null для clear), `SortDirection Direction {get;}`, `bool Cancel {get;set;}` |
| `TableViewSortedEventArgs` | `TableViewColumn Column {get;}` (null для clear), `SortDirection Direction {get;}` |
| `TableViewBeginningEditEventArgs` | `object Item {get;}`, `TableViewColumn Column {get;}`, `bool Cancel {get;set;}` |
| `TableViewCellEditEndingEventArgs` | `object Item {get;}`, `TableViewColumn Column {get;}`, `TableViewEditAction EditAction {get;}`, `bool Cancel {get;set;}` |
| `SelectionChangedEventArgs` | стандартные `AddedItems`, `RemovedItems` |

Cancel читается синхронно после return handler, deferral / async validation отсутствуют. Sorting.Cancel suppresses change, glyph and Sorted event. CellEditEnding.Cancel при commit удерживает editor открытым. Изменение row selection и current editing cell независимы; не предполагайте SelectedItem равным args.Item.

### Буферизация editing и validation

```xml
<DataTemplate x:Key="Editor">
    <TextBox Text="{Binding Notes, Mode=TwoWay, UpdateSourceTrigger=Explicit}" />
</DataTemplate>
```

Editor должен использовать classic Binding: штатный commit обнаруживает `GetBindingExpression`; compiled x:Bind недоступен таким способом. Explicit buffering делает Esc предсказуемым. Interactive display template с TwoWay checkbox/date picker может сознательно менять source сразу; эти side effects control отменить не может.

Есть проверка `INotifyDataErrorInfo` для свойства text column Binding.Path, rollback source snapshot и удержание редактора при invalid commit. В лаборатории Score допускает 0–100; пробуйте −1, 101, корректировку и Esc. При асинхронной смене ошибок после синхронного commit новая transaction/deferral модель не появляется.

## Enums — все опубликованные значения

| Enum | Значения |
|---|---|
| `SortDirection` | None=0, Ascending=1, Descending=2 |
| `TableViewFrozenEdge` | None=0, Leading=1, Trailing=2 (reserved) |
| `TableViewSelectionMode` | None=0, Single=1 |
| `TableViewHeadersVisibility` | None=0, Column=1 ([Flags]) |
| `TableViewGridLinesVisibility` | All=0, Horizontal=1, None=2, Vertical=3 |
| `TableViewDensity` | Compact=0, Standard=1, Comfortable=2 |
| `TableViewEditAction` | Commit=0, Cancel=1 |
| `TableViewSortCycle` | AscendingDescending=0, AscendingDescendingNone=1, DescendingAscending=2, DescendingAscendingNone=3 |

SortCycle controls repeated header activation, не программные SortByColumn/Source.Sort. `*None` добавляет unsorted third step.

## Клавиатура и UI Automation

| Жест | Действие |
|---|---|
| Up/Down/PageUp/PageDown | та же видимая колонка в другой отображаемой строке |
| Left/Right/Home/End | навигация по cells |
| Ctrl+Home/Ctrl+End | первая/последняя cell всей таблицы |
| Enter/Space на header | sort direction cycle |
| Alt+Left/Alt+Right на header | resize; Shift увеличивает шаг |
| Escape при drag resize | восстановить width до начала drag |
| Double click/double tap, F2 | начать cell editor |
| Enter в editor | commit |
| Escape в editor | cancel; editor controls сохраняют свой первый Esc |
| Enter в interactive display cell | focus интерактивного контрола шаблона |
| Escape из interactive display content | вернуться к grid navigation |

Pointer selection применяется на release, а не press, для mouse/touch/pen. Keyboard navigation подавлена во время editing. Ctrl+navigation позволяет двигать фокус отдельно от selection; headers и группы имеют самостоятельную keyboard activation.

| Peer | Constructor | UIA patterns |
|---|---|---|
| `TableViewAutomationPeer` | `(TableView owner)` | ISelectionProvider, IGridProvider, ITableProvider, IItemContainerProvider |
| `TableViewRowAutomationPeer` | `(TableViewRow owner)` | ISelectionItemProvider |
| `TableViewCellAutomationPeer` | `(FrameworkElement cell, TableViewRow row, TableViewColumn column, int columnIndex)` | IGridItemProvider, ITableItemProvider, IValueProvider |
| `TableViewColumnHeaderAutomationPeer` | `(TableView owner, TableViewColumn column)` | IInvokeProvider |
| `TableViewGroupHeaderAutomationPeer` | `(TableViewGroupHeader owner)` | IExpandCollapseProvider, IGridItemProvider |

Все peers унаследуют FrameworkElementAutomationPeer. Text ValuePattern и grid coordinates доступны клиентам accessibility/UI tests. Tooltip strings публикуются как HelpText; custom name/LabeledBy поддерживаются. Cell value updates после успешного commit отправляют change notifications существующим peers. Ресурсы и styles обеспечивают Light/Dark/High Contrast.

Особенность UI QA этого экспериментального выпуска: после reshape/reset полный обход table peers может вернуть `stale_element`. WinApp `invoke --action ...` выполняет такой строгий полный обход для доказательства уникальности даже exact AutomationId. UI tests используют обычный invoke по уникальным авторским IDs; Expand/Toggle предварительно читают текущее состояние, поэтому сохраняют идемпотентность. Settings расположен первым в логическом дереве при прежнем `Grid.Column=1`; это даёт раннему FindFirst доступ к настройкам без прохода через TableView. Read-only queries ограниченно повторяются после peer replacement; mutations не повторяются вслепую, непрошедшие assertions остаются FAIL.

## Сопоставление лаборатории с API

- Сценарии: смешанные widths, all Auto, all Star, interactive checkbox/expander/custom column, 10 000 строк, EmptyTemplate.
- Источники: ObservableCollection напрямую / explicit TableViewSource.
- Shaping: поиск + score predicate, string key, computed key, reference key + IdentitySelector, expand/collapse all, clear filter/group.
- Sorting: control/path/key forms, source multi-key, 4 cycles, custom comparer, click-to-sort gates, отмена Sorting и events log.
- Columns: Header, HeaderTemplate, HeaderTemplateSelector, SortMemberPath, comparer, Width Auto/Star/Pixel, Min/Max/ActualWidth, Visibility, CanResize/CanSort/IsReadOnly, Leading frozen, live Add/Remove/Insert.
- Selection: None/Single, Select/Deselect/DeselectAll/IsSelected, SelectedItem/Index, grouping projection indices.
- Editing: built-in text, explicit-buffer template editor, validation, BeginningEdit/CellEditEnding cancel toggles, CommitEdit/CancelEdit и IsEditing.
- Presentation: GridLines, Headers, Density, row banding, tooltips, empty/group templates, RTL.
- Observable updates: Add, Remove, mutate properties; editor and row identity сохраняют штатное поведение SDK.
- API inspector: реальные TableView, TableViewSource, columns, selected row и realized GroupInfo objects.
- Smoke checks: native API calls with selection assertions, sort-state assertions, shaping/grouping/identity/multi-axis invocation, collection mutation, Commit/Cancel no-editor results.

## Что отсутствует в этом релизе

Нет Multiple/Extended/cell-range selection, row editing/row transactions/IEditableObject contract, public BeginEdit/current-cell API, RowEditEnding, post-edit-complete event, async edit deferrals, multi-level grouping, trailing frozen implementation, drag reorder API, auto-generated columns, row headers и column virtualization. Buttons порядка в лаборатории вызывают mutable Columns и честно обозначены как действия приложения. Не следует представлять отсутствующие функции как реализованные SDK.
