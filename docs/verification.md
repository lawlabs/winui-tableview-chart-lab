# Verification report

Verified **October 7, 2026** with Windows App SDK 2.5.4-experimental, .NET SDK 10.0.401, and WinApp CLI 0.7.1. Build: packaged x64 Debug with self-contained Windows App SDK. The interactive desktop used 200% display scaling.

## Build and launch

BuildAndRun.ps1 restored, built, registered the development package, and launched a responsive native window. There were **zero build errors** and four expected WMC1501 experimental XAML warnings. New DLL/PRI resources are included in deployment.

English was confirmed on first launch after adding localization. Both languages then passed switch tests. Language preference persists; changing it reloads sample data/settings while preserving the selected navigation section and theme.

## Current results

| Suite | Result | Coverage | Recorded report |
| --- | --- | --- | --- |
| TableView | 25 PASS, 0 FAIL, 0 SKIP | Six presets; 10,000 rows; filters/groups/sorting/veto; selection; F2/Enter/Esc; column mutation; presentation | [JSON](validation/table-ui-results.json) |
| Chart | 29 PASS, 0 FAIL | Ten presets; CRUD; stream/30-point window; brushes/styles/markers/labels; axes; point editor/overrides; numeric X restriction | [JSON](validation/chart-ui-results.json) |
| Appearance | 7 PASS, 0 FAIL | Area alpha 0/0.5/1, Bar 0.5; native ARGB; dark tick labels; retained opacity | [JSON](validation/appearance-ui-results.json) |
| Shell and inspector | 7 PASS, 0 FAIL | Live property edit/restore; empty search; API catalog export; navigation and three themes | [JSON](validation/shell-ui-results.json) |
| Responsive layout | 5 PASS, 0 FAIL | About 770x610 DIP; visible table; collapsed log; reachable settings; chart workspace; wide restoration | [JSON](validation/responsive-ui-results.json) |
| Localization | 6 PASS, 0 FAIL | English navigation/messages; Russian switch; preserved navigation/theme; English recovery | [JSON](validation/localization-ui-results.json) |

Total: **79 passing UI/API checks**. The original functional suites assert Russian sample text; the wrapper selects Russian for those suites and restores English. Localization cases exercise both versions separately. These checks cover selected contracts and interactions, not every possible property combination.

Repository checks also validate matching resource sets, nonempty English/Russian strings, format arguments, source resource references, and local documentation links. GitHub Actions performs these checks and a Windows build; UI automation requires an interactive desktop and runs locally.

## Rendering review

Real English screenshots are curated in the [README gallery](../README.md#screenshots). Test captures also cover opacity, dark axes, and compact layouts. Long first-row notes intentionally exercise variable row height. Narrow windows scroll the workspace; graph and settings need not fit simultaneously.

High Contrast, screen readers, ARM64/x86, Release trimming, and clean-machine deployment were **not tested**. The 10,000-row scenario demonstrates functionality without measuring FPS or memory.

## Experimental SDK observations

- LinearAxis assigned to LineSeries.XAxis is rejected with 0x80070057. Numeric X scenarios use equally spaced string categories; LinearAxis Y and DateTimeAxis X work.
- Samples updates use newly assigned typed arrays. Direct ObservableCollection<object> observation did not produce expected rendering in this build.
- Native Area fill honors Color.A rather than Brush.Opacity in the tested case; the sample normalizes alpha.
- Resolved theme-probe brush changes trigger axis/series color refreshes. Dark tick labels remain white and readable.
- Blank editable TableView group keys are normalized before GroupBy to avoid a native identity failure.
- TableView UIA full traversal can return stale peers. Chart's peer can report zero bounds despite visible rendering. Tests use direct IDs and viewport checks plus image review.

Read [TableView research](table-research.md) and [Chart research](chart-research.md) for details. The [catalog exported from the loaded SDK](public-api.txt) lists 55 public types and their properties, methods, events, and enums. Inspector methods/events are reference entries; corresponding operations are exposed by lab commands.

## Reproduce

```powershell
.\BuildAndRun.ps1
.\tests\repository-checks.ps1
.\tests\run-ui-tests.ps1 -AppPid <PID from launch>
.\tests\capture-readme-screenshots.ps1 -AppPid <PID from launch>
```

Run UI suites sequentially against the same existing window. The screenshot script expects a wide window and deliberately leaves the English mixed Chart scenario open in Dark.
