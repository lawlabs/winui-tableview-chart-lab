# Windows App SDK: обзор релиза для лаборатории

Проверено 6 октября 2026 года. Ссылка пользователя ведёт на общую страницу истории **2.0**, но её якорь указывает на **2.5.4-experimental**, выпущенный 29 сентября 2026 года. Ближайшая стабильная версия — **2.5.1**, выпущенная 16 сентября. Экспериментальный выпуск включает её изменения и добавляет перечисленные ниже экспериментальные возможности. [Microsoft Learn: release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)

## Таблицы и графики уже выпущены в Experimental

**TableView** — собственный компонент WinUI 3 в пространстве имён `Microsoft.UI.Xaml.Controls.Tabular`. Релиз предоставляет группировку, шаблоны заголовков групп, сортировку, фильтрацию, индикаторы сортировки, изменение ширины колонок мышью и клавиатурой, подсказки.

**Chart** — собственный компонент в `Microsoft.UI.Xaml.Controls.Charts`. Поддерживает декартовы графики с сериями **Line**, **Area**, **Bar**, настройкой осей, легенд, подписей и маркеров. Это функции опубликованного экспериментального выпуска, а не только предложение дизайна. [Официальный WinUI release tag](https://github.com/microsoft/microsoft-ui-xaml/releases/tag/winui3%2Frelease%2F2.5.4-experimental)

Подробное покрытие компонентов и способы экспериментов приведены в документах этой лаборатории. Источник истины для наличия API — метаданные установленного пакета и исходники соответствующего release tag.

Исполняемые лаборатории посвящены TableView и Chart. Другие изменения 2.5 Experimental описаны ниже; программная привязка `SetThemeResourceBinding` также показана в приложении.

## Остальные новые возможности 2.5 Experimental

В WinUI появились программная привязка ресурсов темы через `FrameworkElement.SetThemeResourceBinding` и конструктор `DataTemplate` с фабрикой элементов. Для рукописного ввода добавлены `InkPresenter.ActivateCustomDrying` и `InkSynchronizer`; `InkToolbar` теперь может работать непосредственно с `InkPresenter`. Исправлены ошибки размеров/синхронизации Ink и очистки недостижимых native-объектов в .NET, уменьшен рост памяти при работе с ресурсами и storyboard. [Официальный WinUI release tag](https://github.com/microsoft/microsoft-ui-xaml/releases/tag/winui3%2Frelease%2F2.5.4-experimental)

Windows AI обновления:

- `AIComputeDevice`: выбор Default/CPU/NPU для OCR; аналогичный выбор при индексировании изображений.
- Распознавание речи: фабрика модели и её параметры, 16-битные входные буферы, асинхронная остановка, дополнительные свойства событий потокового распознавания.
- Языковая модель: имя, версия, результат проверки совместимости LoRA.
- Новые причины отказа генерации: игровой режим и нехватка видеопамяти.

В stable 2.5.1 добавлены диагностические модули Windows Error Reporting для self-contained .NET MSIX и App Content Search с LAF-доступом: индексирование текста и изображений с OCR, лексический и семантический поиск. Исправления касаются NavigationView, popup/input, сочетаний клавиш, завершения XAML, CommandBar, System Composition Engine, установки runtime и Video Super Resolution. [Microsoft Learn: release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental)

## Подключение новых компонентов

Пакет опубликован в NuGet:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="2.5.4-experimental" />
```

Метапакет использует отдельные версии компонентов: например, `Microsoft.WindowsAppSDK.WinUI` имеет зависимость `>= 2.3.10-experimental`. Нельзя механически присваивать всем компонентам номер метапакета. [NuGet: 2.5.4-experimental](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.5.4-experimental)

Для TableView в `Application.Resources` нужны оба словаря:

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <TabularControlsResources xmlns="using:Microsoft.UI.Xaml.Controls.Tabular" />
</ResourceDictionary.MergedDictionaries>
```

Без `TabularControlsResources` первая раскладка TableView может завершиться `XamlParseException 0x802B000A` из-за отсутствующего `SortIndicatorForeground`; внешне это может проявиться как `0xC000027B`. Колонки требуется объявлять явно. Их отсутствие даёт строки без ячеек и заголовков. API размечен как экспериментальный: возможны предупреждения C# `CS8305` и XAML `WMC1501`. [Официальный sample README](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/TableViewSampleApp/README.md)

Официальная матрица примеров TableView содержит C# и C++, каждый в packaged и unpackaged вариантах. Для C++ обычная `{Binding}` требует доступной информации о свойствах модели; в примере используется `[bindable]`. Альтернатива для шаблонной колонки — `x:Bind`. [Официальная consumer matrix](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/TableViewApp/README.md)

Для Chart официальный C# unpackaged пример использует `XamlControlsResources`, `UseWinUI=true`, `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`. В его `App.xaml` отдельный merge `XamlChartsResources` отсутствует. Это наблюдение по примеру, не утверждение об обязательности или запрете этого словаря. [Chart csproj](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/ChartApp/ChartAppCsUnpackaged/ChartAppCsUnpackaged.csproj), [Chart App.xaml](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/Samples/ChartApp/ChartAppCsUnpackaged/App.xaml)

В этой лаборатории Chart использует типизированные массивы `string[]`, `double[]` и `DateTimeOffset[]`. Изменение точки, добавление/удаление и поток назначают `Samples.ItemsSource` новый массив. Это проверяет обновление источника реального компонента, а поведение подписки native Chart на `ObservableCollection<object>` не объявляется проверенным.

## Требования и оговорки документации

SDK распространяется независимо от Windows SDK и ОС; общая граница совместимости — Windows 10 1809. Это не означает, что любая Windows AI функция работает на любом устройстве: для неё проверяют собственные требования и состояние готовности. [Версии Windows, Windows SDK и Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/get-started/versioning-overview)

Experimental предназначен для исследования и обратной связи: API может измениться или исчезнуть. App Content Search в stable 2.5.1 требует LAF-токен, а экспериментальная версия — нет. [Release channels](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels)

У раздела 2.5 Experimental нет отдельного списка known issues для TableView или Chart. Это не подтверждение отсутствия дефектов. [Release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental#version-25-experimental-254-experimental)

В документах release tag обнаружены несогласованности:

- README TableViewSampleApp всё ещё сообщает, что TableView отсутствует в опубликованном пакете и требует локальной упаковки. Это противоречит release notes и опубликованному NuGet-выпуску. Команды сборки самого репозитория WinUI не являются инструкцией установки этой лаборатории.
- Введение [TableView API spec](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/docs/api-specs/TableView/TableView-spec.md) перечисляет сортировку, фильтрацию и группировку как запланированные, хотя release notes уже объявляют их доступными. Их наличие проверяется по релизному пакету. Остальные упоминания будущих функций нельзя считать обещанием готового API.

При расхождении концептуального текста с исполняемым пакетом лаборатория демонстрирует реально доступный API и явно отмечает ограничения.

## Полезные официальные входные точки

- [Полная история 2.x, включая stable и experimental выпуски](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=experimental).
- [WinUI release source 7b12709](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709).
- [TableView sample: настройки и сценарии](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewSampleApp).
- [TableView minimal consumer samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/TableViewApp).
- [Chart packaged/unpackaged samples](https://github.com/microsoft/microsoft-ui-xaml/tree/7b12709/Samples/ChartApp).
- [TableView публичный IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableView.idl).
- [TableViewSource публичный IDL](https://github.com/microsoft/microsoft-ui-xaml/blob/7b12709/controls/dev/TableView/TableViewSource.idl).
