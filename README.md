# WinUI TableView & Chart Lab

[![Windows build](https://github.com/lawlabs/winui-tableview-chart-lab/actions/workflows/build.yml/badge.svg)](https://github.com/lawlabs/winui-tableview-chart-lab/actions/workflows/build.yml)
[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Windows App SDK](https://img.shields.io/badge/Windows_App_SDK-2.5.4--experimental-8b5cf6.svg)](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.5.4-experimental)

An interactive **WinUI 3** sample for the new native **TableView** and **Chart** components in **Windows App SDK 2.5.4-experimental**. Explore tables, Cartesian charts, live data, editing, grouping, axes, and the public SDK API in a real C# / XAML desktop application.

**English is the first-run default.** Switch English/Russian in the window header; the choice is remembered. Primary documentation and screenshots are English. [Russian README / Русская документация](README.ru.md).

## Why this sample exists

**Microsoft has just introduced its own native WinUI 3 TableView and Chart controls.** They became publicly available in **Windows App SDK 2.5.4-experimental, released on September 29, 2026**: TableView provides tabular data presentation, and Chart provides Cartesian line, area, and bar charts. These are **experimental SDK components**, rather than a stable release. See the [official Microsoft release announcement](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental).

Before this release, these Microsoft-provided TableView and Chart controls were not available as built-in public WinUI 3 components in the shipped Windows App SDK. Developers filled that gap with community libraries, separately distributed toolkit controls, commercial suites, or their own table and chart implementations. This lab makes the new SDK controls easy to explore; the [earlier WinUI 3 ecosystem](#earlier-winui-3-table-and-chart-libraries) below gives useful historical context.

## Screenshots

Click any image to open the full-resolution PNG. These are real captures of the running English WinUI application with synthetic sample data.

<a href="docs/screenshots/chart-mixed-dark.png"><img src="docs/screenshots/chart-mixed-dark.png" width="100%" alt="Dark WinUI Chart lab with blue LineSeries, coral AreaSeries, violet BarSeries, a legend, and live settings" title="Native WinUI Chart: mixed Line, Area, and Bar series"></a>

*Native WinUI Chart in Dark: Line, Area, and Bar series share a categorical X axis and editable data sources.*

<table>
<tr>
<td width="50%"><a href="docs/screenshots/table-star-light.png"><img src="docs/screenshots/table-star-light.png" width="100%" alt="English WinUI 3 TableView · Star sizing: Proportional columns, templates, and filtered rows." title="TableView · Star sizing"></a><p><b>TableView · Star sizing</b><br>Proportional columns, templates, and filtered rows.</p></td>
<td width="50%"><a href="docs/screenshots/table-grouped-dark.png"><img src="docs/screenshots/table-grouped-dark.png" width="100%" alt="English WinUI 3 TableView · Grouping: Category groups, themed headers, and expansion." title="TableView · Grouping"></a><p><b>TableView · Grouping</b><br>Category groups, themed headers, and expansion.</p></td>
</tr>
<tr>
<td width="50%"><a href="docs/screenshots/chart-lines-light.png"><img src="docs/screenshots/chart-lines-light.png" width="100%" alt="English WinUI 3 Chart · Lines: Blue and coral lines with circle markers and numeric labels." title="Chart · Lines"></a><p><b>Chart · Lines</b><br>Blue and coral lines with circle markers and numeric labels.</p></td>
<td width="50%"><a href="docs/screenshots/chart-area-dark.png"><img src="docs/screenshots/chart-area-dark.png" width="100%" alt="English WinUI 3 Chart · Areas: Translucent blue and coral fills on readable dark axes." title="Chart · Areas"></a><p><b>Chart · Areas</b><br>Translucent blue and coral fills on readable dark axes.</p></td>
</tr>
<tr>
<td width="50%"><a href="docs/screenshots/chart-bars-light.png"><img src="docs/screenshots/chart-bars-light.png" width="100%" alt="English WinUI 3 Chart · Bars: Native Revenue and Cost BarSeries with editable values." title="Chart · Bars"></a><p><b>Chart · Bars</b><br>Native Revenue and Cost BarSeries with editable values.</p></td>
<td width="50%"><a href="docs/screenshots/chart-dates-light.png"><img src="docs/screenshots/chart-dates-light.png" width="100%" alt="English WinUI 3 Chart · Dates: Real DateTimeAxis with English calendar labels." title="Chart · Dates"></a><p><b>Chart · Dates</b><br>Real DateTimeAxis with English calendar labels.</p></td>
</tr>
<tr>
<td width="50%"><a href="docs/screenshots/table-interactive-light.png"><img src="docs/screenshots/table-interactive-light.png" width="100%" alt="English WinUI 3 TableView · Interactive templates: Row checkboxes, expanders, and a custom gauge column." title="TableView · Interactive templates"></a><p><b>TableView · Interactive templates</b><br>Row checkboxes, expanders, and a custom gauge column.</p></td>
<td width="50%"><a href="docs/screenshots/chart-dual-axis-dark.png"><img src="docs/screenshots/chart-dual-axis-dark.png" width="100%" alt="English WinUI 3 Chart · Two Y axes: Line series assigned to independent left and right axes." title="Chart · Two Y axes"></a><p><b>Chart · Two Y axes</b><br>Line series assigned to independent left and right axes.</p></td>
</tr>
</table>

<details>
<summary>API inspector screenshot</summary>
<p><a href="docs/screenshots/api-inspector-light.png"><img src="docs/screenshots/api-inspector-light.png" width="100%" alt="English API inspector showing live TableView Density, enum editor, restore action, and method signatures" title="Explore and edit the loaded WinUI SDK API"></a></p>
<p>Inspect real tables, charts, columns, series, axes, and Samples objects; export the type catalog.</p>
</details>

## Run the WinUI 3 sample

Use Windows with Developer Mode enabled, **.NET SDK 10.0.401**, and a working WinUI development environment with Windows SDK 10.0.26100.0. Visual Studio with WinUI/.NET desktop tooling is the easiest route. The verified configuration is **packaged x64 Debug**; other architectures and Release trimming have not been validated.

```powershell
git clone https://github.com/lawlabs/winui-tableview-chart-lab.git
cd winui-tableview-chart-lab
.\BuildAndRun.ps1
```

Or open `SdkComponentLab.slnx` in Visual Studio. The script restores dependencies, closes this sample's previous instance, builds, registers the development package, and launches the native window. Its NuGet dependency supplies **WinApp CLI 0.7.1**, without a global CLI update. Use `-Diagnostics` for attached launch diagnostics.

`WindowsAppSDKSelfContained=true` deploys new DLL/PRI resources. Experimental XAML `WMC1501` warnings are expected. The source project name remains `SdkComponentLab`; the displayed product name is **WinUI TableView & Chart Lab**.

## Explore TableView

| Area | Experiments |
| --- | --- |
| Scenarios | Mixed columns, Auto, Star, interactive templates, 10,000 rows, empty source |
| Data shaping | Filter/ClearFilter; property/computed/reference-identity groups; group templates; expand/collapse; column/path/delegate sorting; multiple source sort keys; custom comparer; sorting veto |
| Columns | Text/template/custom gauge; Width/Min/Max/ActualWidth; visibility; Leading frozen; live add/remove/reorder; header templates/selectors; tooltips |
| Editing | F2/double-click; Enter/Esc; CommitEdit/CancelEdit; begin/commit veto; Score validation; CellEditingTemplate |
| Selection | Single/None; Select/Deselect/DeselectAll/IsSelected; projection indices and selected model |
| Presentation | Density, grid lines, banding, headers, read-only gates, RTL, empty/group templates, event log |

Try **Score = -1 or 101**, edit Notes to compare Enter and Esc, sort using headers, and group by category. Settings scroll on the right; narrow windows place them below the table.

## Explore native WinUI charts

| Area | Experiments |
| --- | --- |
| Scenarios | Line, Area, Bar, mixed series, numeric categories, dates, two Y axes, horizontal bars, empty data, 200 points |
| Data | Add/remove/change points and series; selected-point editor; typed Samples sources; stream with a 30-point window |
| Series | Visibility, title, Stroke/Fill, thickness, dash styles, labels/markers and brushes, every MarkerShape, point overrides, Y-axis assignment |
| Axes | CategoryAxis sorting; LinearAxis Y ranges/spacing; DateTimeAxis formats/intervals/bounds; labels, ticks, lines, and grids |
| Appearance | Light/Dark/System; semantic WinUI brushes and vivid palette; fill alpha; dynamic theme resource refresh |

Additional Chart commands are in the toolbar's `...` menu. Changes reach live native objects. The app owns editable data and replaces Samples.ItemsSource with typed arrays after changes.

## Languages and API inspector

Select **English** or **Русский** in the header. Switching language reloads sample labs/data, preserving navigation and theme. Strings use matching MRT Core `.resw` resources, XAML `x:Uid`, and ResourceLoader; first-run English is independent of the OS display language.

The inspector searches live TableView/Chart properties, edits supported simple values, restores originals, and optionally includes inherited WinUI properties. Collections/templates are exercised through lab controls; methods/events are listed as signatures. The [exported public API catalog](docs/public-api.txt) contains **55 public types** from both namespaces.

## Research and release boundaries

This is an experimental SDK exploration sample. TableView currently supports Single selection and Leading frozen columns. Headers sort one column; source sorting can compose keys. Reorder buttons are sample behavior. Native Chart declares Cartesian Line/Area/Bar; pie/3D, stacking, and built-in zoom/pan are absent from this release API.

The tested SDK rejects LinearAxis on LineSeries.XAxis with `0x80070057`. Numeric X scenarios use equally spaced string categories and expose a separate restriction probe. LinearAxis Y and DateTimeAxis X work. Fill opacity is normalized into Color.A. Research notes distinguish published APIs from runtime observations.

- [SDK release overview and requirements](docs/sdk-release-notes.md)
- [Complete TableView API research and official sources](docs/table-research.md)
- [Complete Chart API research and official sources](docs/chart-research.md)
- [Verification: 79 passing UI/API checks](docs/verification.md)
- [Official Microsoft release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)

## Earlier WinUI 3 table and chart libraries

WinUI 3 tables, data grids, and charts already existed through the wider .NET component ecosystem before Microsoft's September 2026 experimental SDK release. The following selection includes free open-source libraries, commercial products, and historical components with documented WinUI 3 support. Links point to the maintainers' repositories, documentation, and licensing information. **Reviewed October 7, 2026.**

### Free and open source

| Library | WinUI 3 components and capabilities | License / availability |
| --- | --- | --- |
| [w-ahmad / WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView) | Community table control built on ListView: column generation, editing, sorting, grouping, filtering, cell selection, and copying. | Free, open source; [MIT](https://github.com/w-ahmad/WinUI.TableView/blob/main/LICENSE.md). |
| [LiveCharts2](https://github.com/Live-Charts/LiveCharts2) | Charts, gauges, and maps, with a [WinUI integration and examples](https://livecharts.dev/docs/winui/list). | Free MIT core; optional paid performance/features and support through [Backers packages](https://livecharts.dev/home/buy). |
| [ScottPlot](https://github.com/ScottPlot/ScottPlot) | Scientific and interactive plotting, including line, scatter, and bar plots; the [WinUI quickstart](https://scottplot.net/quickstart/winui/) uses `ScottPlot.WinUI` and `WinUIPlot`. | Free, open source; [MIT](https://github.com/ScottPlot/ScottPlot/blob/main/LICENSE). |

**Two different TableView controls:** the community `WinUI.TableView` project above is independent of Microsoft's new `Microsoft.UI.Xaml.Controls.Tabular.TableView`. They have different APIs and implementations; the sample in this repository uses the Microsoft SDK component.

### Commercial suites, including conditional free licensing

| Suite | WinUI 3 tables and charts | License / availability |
| --- | --- | --- |
| [Syncfusion WinUI](https://www.syncfusion.com/winui-controls/datagrid) | DataGrid for editing, sorting, filtering, grouping, and summaries; [Cartesian charts](https://help.syncfusion.com/winui/cartesian-charts/overview) and [circular charts](https://help.syncfusion.com/winui/circular-charts/overview). | Proprietary commercial license. A free [Community License](https://www.syncfusion.com/products/communitylicense) is available to eligible individuals and organizations; eligibility and registration are required. |
| [Telerik UI for WinUI](https://www.telerik.com/winui) | DataGrid plus [Cartesian, pie, and polar charts](https://www.telerik.com/winui/documentation/controls/radchart/getting-started). | Commercial, paid through DevCraft bundles; a time-limited evaluation trial is available. |
| [MESCIUS ComponentOne WinUI](https://developer.mescius.com/componentone/winui-controls) | FlexGrid for tabular editing, sorting, filtering, and grouping; FlexChart for multiple chart types. | Commercial developer licensing in the WinUI & MAUI edition; an evaluation trial is available. |

### Historical toolkit and vendor components

| Component | What it provided | Current status |
| --- | --- | --- |
| [Windows Community Toolkit DataGrid 7.x](https://www.nuget.org/packages/CommunityToolkit.WinUI.UI.Controls.DataGrid/7.1.2) | A separately distributed MIT DataGrid package for WinUI 3, `CommunityToolkit.WinUI.UI.Controls.DataGrid`, with editable tabular data. | Legacy 7.x package. Microsoft's [archived DataGrid documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/archive/windows/datagrid) says DataGrid is not included in the WinUI 3 controls in Toolkit 8.0 and later. It was not a built-in Windows App SDK control. |
| [DevExpress WinUI](https://supportcenter.devexpress.com/ticket/details/t1084596/winui-data-grid-bind-to-a-collection-of-columns-specified-in-a-viewmodel) | A vendor suite with Data Grid and charts, formerly offered under a [free proprietary WinUI license](https://supportcenter.devexpress.com/ticket/details/t1173515/candlestick-charts-in-winui3). | Discontinued and no longer available to new customers; DevExpress identifies **23.2.6** as the last official release. Included here for historical context. |

This catalog documents the earlier ecosystem. The interactive TableView and Chart experiments in this app exercise Microsoft's new Windows App SDK APIs. Library capabilities, licenses, and maintenance status are documented by their respective maintainers at the links above.

## Validate and contribute

```powershell
.\tests\repository-checks.ps1
.\tests\run-ui-tests.ps1 -AppPid <PID from BuildAndRun>
```

UI tests run sequentially against the existing window on an interactive Windows desktop. Generated development artifacts are ignored; curated English screenshots and recorded reports are in `docs/`. GitHub Actions checks resources/documentation and compiles the WinUI project on Windows.

See [CONTRIBUTING.md](CONTRIBUTING.md). Contributions to native WinUI TableView scenarios, Chart experiments, accessibility, localization, and documentation are welcome.

## License

[MIT](LICENSE). Microsoft Windows App SDK dependencies retain their own licenses. All sample records and chart values are synthetic.
