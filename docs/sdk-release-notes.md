# Windows App SDK release research

Research baseline: **Windows App SDK 2.5.4-experimental**, released September 29, 2026. Microsoft's URL contains `windows-app-sdk-2-0`, but the selected anchor describes the 2.5 Experimental release. This WinUI 3 sample pins the actual NuGet package version. [Microsoft release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental).

## Native TableView and Chart

TableView ships in `Microsoft.UI.Xaml.Controls.Tabular`. It provides data shaping, grouping and group templates, sorting, indicators, column sizing with mouse and keyboard, tooltips, templates, and editing. Chart ships in `Microsoft.UI.Xaml.Controls.Charts`: Cartesian Line, Area, and Bar series, axes, legends, labels, and markers. These are published experimental components. [WinUI release tag](https://github.com/microsoft/microsoft-ui-xaml/releases/tag/winui3%2Frelease%2F2.5.4-experimental).

Read the [TableView inventory](table-research.md) and [Chart inventory](chart-research.md) for the complete component surfaces and experiments. The lab's API inspector also reads the **loaded SDK assembly**, including 55 public types across the two namespaces. Installed metadata, release sources, and runtime observations are distinguished throughout the documentation.

## Other changes in 2.5 Experimental

| Area | Changes announced by Microsoft |
| --- | --- |
| WinUI | `FrameworkElement.SetThemeResourceBinding`, a `DataTemplate` constructor accepting an element factory, native object cleanup and resource/storyboard memory fixes |
| Ink | `InkPresenter.ActivateCustomDrying`, `InkSynchronizer`, direct InkToolbar/InkPresenter integration, sizing and synchronization fixes |
| Windows AI OCR and indexing | `AIComputeDevice` selection for Default/CPU/NPU |
| Speech | Model factory/options, 16-bit input buffers, asynchronous stop, additional streaming recognition event properties |
| Language model | Model name/version and LoRA compatibility result |
| Generation failures | Additional reasons for gaming mode and insufficient GPU memory |

These changes are described in the [release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental) and [WinUI release tag](https://github.com/microsoft/microsoft-ui-xaml/releases/tag/winui3%2Frelease%2F2.5.4-experimental). Executable labs focus on TableView and Chart. The overview page also demonstrates the new `SetThemeResourceBinding`; the AI features are documented, with no model download or AI execution in this sample.

The release incorporates stable 2.5.1 changes: Windows Error Reporting diagnostic modules for self-contained .NET MSIX, App Content Search with text/image OCR indexing and lexical/semantic search, plus fixes for NavigationView, popup/input, shortcuts, XAML shutdown, CommandBar, composition, runtime installation, and Video Super Resolution. Stable App Content Search requires Limited Access Feature access. Consult Microsoft's release notes for feature-specific prerequisites.

## Integrating the components

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="2.5.4-experimental" />
```

The metapackage's component versions can differ: do not assign its version to every dependency. [Published NuGet package](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.5.4-experimental).

TableView needs the component resources alongside standard WinUI resources:

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <TabularControlsResources xmlns="using:Microsoft.UI.Xaml.Controls.Tabular" />
</ResourceDictionary.MergedDictionaries>
```

Missing Tabular resources can cause a first-layout XAML error for `SortIndicatorForeground`. Declare columns explicitly. This sample deploys the new DLLs and PRI dictionaries with `WindowsAppSDKSelfContained=true`. Experimental `WMC1501` warnings are expected. [Microsoft TableView sample](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/TableViewSampleApp/README.md).

Microsoft publishes C# and C++ packaged/unpackaged TableView consumers. Classic C++ binding needs model property metadata, such as `[bindable]`; template columns can also use compiled binding. [Consumer matrix](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/TableViewApp/README.md).

Microsoft's Chart sample enables WinUI and self-contained deployment. Its App.xaml uses standard XamlControlsResources without an explicit XamlChartsResources merge. This is an observation about that sample's configuration. [Chart project](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/ChartApp/ChartAppCsUnpackaged/ChartAppCsUnpackaged.csproj), [Chart resources](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/ChartApp/ChartAppCsUnpackaged/App.xaml).

## Documentation boundaries

Experimental APIs may change or disappear. The SDK's overall Windows 10 1809 compatibility does not guarantee availability of every Windows AI feature on every device. [Release channels](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels), [versioning overview](https://learn.microsoft.com/en-us/windows/apps/get-started/versioning-overview).

Some source documentation predates the published package: the TableView sample README still discusses local packaging, and the API spec introduction describes sorting/filtering/grouping as planned. The released package contains these APIs; other planned features must be checked against release metadata. No separate TableView/Chart known-issues list appears in the selected release section, which does not prove an absence of defects.

## Official source entry points

- [Release source snapshot: 7b12709](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709)
- [TableView interactive samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewSampleApp)
- [TableView minimal consumers](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewApp)
- [Chart samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/ChartApp)
- [TableView IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableView.idl)
- [TableViewSource IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableViewSource.idl)

[Russian research notes](ru/sdk-release-notes.md).
