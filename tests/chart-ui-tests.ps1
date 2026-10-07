[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][int]$AppPid,
    [string]$WinAppPath = (Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'chart-ui-results')
)

# Exercises the already-running app. It does not build, deploy, launch or stop processes.
# PASS means the UI/API assertion succeeded. PNGs require visual review of native rendering.
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $WinAppPath -PathType Leaf)) { throw "WinApp 0.7.1 not found: $WinAppPath" }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$script:results = [System.Collections.Generic.List[object]]::new()
$script:screenshotIndex = 0

function Ui {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $nativeOutput = & $WinAppPath ui @Arguments -a $AppPid --json 2>&1
    $nativeCode = $LASTEXITCODE
    if ($nativeCode -ne 0) { throw "winapp ui $($Arguments -join ' ') exited $nativeCode`: $(($nativeOutput | Out-String).Trim())" }
}
function Read-UiJson {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $nativeOutput = & $WinAppPath ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw "winapp ui query failed: $nativeOutput" }
    return (($nativeOutput | Out-String) | ConvertFrom-Json)
}
function Get-NumberBoxEditor {
    param([string]$Selector)
    # Inspect just the known NumberBox. Its Spinner peer has no ValuePattern/focus;
    # the inner InputBox Edit peer exposes the actual text and stable runtime selector.
    $document = Read-UiJson @('inspect', $Selector, '-d', '3')
    $roots = @()
    if ($document.elements) { $roots += @($document.elements) }
    if ($document.window.elements) { $roots += @($document.window.elements) }
    foreach ($window in @($document.windows)) { if ($window.elements) { $roots += @($window.elements) } }
    function Find-EditChild {
        param([object[]]$Nodes)
        foreach ($node in $Nodes) {
            if ($node.type -in @('Edit', 'TextBox') -and $node.selector) { return [string]$node.selector }
            if ($node.children) {
                $found = Find-EditChild -Nodes @($node.children)
                if ($found) { return $found }
            }
        }
        return $null
    }
    $editor = Find-EditChild -Nodes $roots
    if (-not $editor) { throw "No editable InputBox child found in NumberBox '$Selector'." }
    return $editor
}
function Read-UiText {
    param([string]$Selector)
    $nativeOutput = & $WinAppPath ui get-value $Selector -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw "get-value $Selector failed: $nativeOutput" }
    $document = ($nativeOutput | Out-String) | ConvertFrom-Json
    if ($null -ne $document.text) { return [string]$document.text }
    if ($null -ne $document.value) { return [string]$document.value }
    throw "Unrecognized UI value for $Selector`: $($document | ConvertTo-Json -Depth 5 -Compress)"
}
function Assert-Number {
    param([string]$Selector, [double]$Expected)
    $editor = Get-NumberBoxEditor $Selector
    $actualText = Read-UiText $editor
    $normalized = $actualText.Trim() -replace '[\u00A0\u202F ]', '' -replace ',', '.'
    $actual = 0.0
    if (-not [double]::TryParse($normalized, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$actual)) { throw "Expected numeric $Expected; actual '$actualText'" }
    if ([math]::Abs($actual - $Expected) -gt 0.00001) { throw "Expected numeric $Expected; actual '$actualText'" }
}
function Set-Number {
    param([string]$Selector, [string]$Value)
    # NumberBox commits its inner TextBox on focus loss. UIA SetValue alone can leave old Value.
    $editor = Get-NumberBoxEditor $Selector
    Ui @('focus', $editor)
    Ui @('send-keys', 'ctrl+vk=0x41', '--via', 'send-input')
    $expected = [double]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture)
    $localizedInput = $expected.ToString('0.#############', [Globalization.CultureInfo]::CurrentCulture)
    Ui @('send-keys', $localizedInput, '--verbatim', '--via', 'send-input')
    Ui @('send-keys', 'tab', '--via', 'send-input')
    Assert-Number $editor $expected
}
function Assert-Contains {
    param([string]$Selector, [string]$Value)
    Ui @('wait-for', $Selector, '--value', $Value, '--contains', '-t', '4000')
}
function Assert-Value {
    param([string]$Selector, [string]$Value)
    Ui @('wait-for', $Selector, '--value', $Value, '-t', '3000')
}
function Command {
    param([string]$Selector)
    try { Ui @('invoke', $Selector) }
    catch {
        # CommandBar secondary commands are materialized when its native overflow is open.
        Ui @('invoke', 'MoreButton')
        Ui @('invoke', $Selector)
    }
}
function Set-Combo {
    param([string]$Selector, [int]$Index, [string]$Expected = '')
    Ui @('invoke', $Selector)
    $keys = 'home'
    for ($step = 0; $step -lt $Index; $step++) { $keys += ' down' }
    $keys += ' enter'
    Ui @('send-keys', $keys, '--via', 'send-input')
    if ($Expected) { Assert-Value $Selector $Expected }
}
function Set-Toggle {
    param([string]$Selector, [bool]$On)
    Ui @('invoke', $Selector, '--action', $(if ($On) { 'toggle-on' } else { 'toggle-off' }))
    Assert-Value $Selector $(if ($On) { 'On' } else { 'Off' })
}
function Expand-Section {
    param([string]$Selector)
    Ui @('invoke', $Selector, '--action', 'expand')
}
function Baseline {
    Set-Combo 'ChartPreset' 0 'Линии • категории'
    Command 'ChartReset'
    Assert-Contains 'ChartStatus' '2 серии · 7 точек'
    Ui @('scroll', 'ChartSettingsScroll', '--to', 'top')
}
function Capture-State {
    param([string]$Name)
    $script:screenshotIndex++
    $path = Join-Path $OutputDirectory ('{0:00}-{1}.png' -f $script:screenshotIndex, $Name)
    # Let the compositor present the state after the preceding UIA change.
    Start-Sleep -Milliseconds 350
    Ui @('screenshot', '-o', $path)
}
function Test-UI {
    param([string]$Name, [scriptblock]$Action)
    $started = Get-Date
    try {
        & $Action
        $script:results.Add([pscustomobject]@{ name = $Name; status = 'PASS'; seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 2) })
        Write-Host "PASS $Name"
    }
    catch {
        $script:results.Add([pscustomobject]@{ name = $Name; status = 'FAIL'; detail = $_.Exception.Message; seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 2) })
        Write-Host "FAIL $Name`: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Test-UI 'Navigate to the native Chart and restore baseline' {
    Ui @('invoke', 'NavCharts')
    Ui @('wait-for', 'ChartPreset', '-t', '5000')
    Baseline
    Ui @('wait-for', 'NativeChart', '-t', '3000')
    Capture-State 'baseline-lines'
}
Test-UI 'Native contract smoke suite passes all ten scenarios' {
    Command 'ChartSmoke'
    Assert-Contains 'ChartStatus' 'Пройдено 10 сценариев'
}

$presets = @(
    @{ index = 0; label = 'Линии • категории'; name = 'lines'; series = 2; count = 7 },
    @{ index = 1; label = 'Площадь • категории'; name = 'area'; series = 2; count = 7 },
    @{ index = 2; label = 'Столбцы • категории'; name = 'bar'; series = 2; count = 7 },
    @{ index = 3; label = 'Смешанные серии'; name = 'mixed'; series = 3; count = 7 },
    @{ index = 4; label = 'Числовые категории X'; name = 'numeric'; series = 2; count = 7 },
    @{ index = 5; label = 'Даты • день'; name = 'dates'; series = 2; count = 7 },
    @{ index = 6; label = 'Две оси Y'; name = 'two-y'; series = 2; count = 7 },
    @{ index = 7; label = 'Горизонтальные столбцы'; name = 'horizontal'; series = 2; count = 7 },
    @{ index = 8; label = 'Пустой источник'; name = 'empty'; series = 2; count = 0 },
    @{ index = 9; label = '200 точек'; name = '200-points'; series = 2; count = 200 }
)
foreach ($presetEntry in $presets) {
    $entry = $presetEntry
    Test-UI "Preset $($entry.name): native configuration and screenshot" {
        Set-Combo 'ChartPreset' $entry.index $entry.label
        Command 'ChartReset'
        Assert-Contains 'ChartStatus' "$($entry.series) серии · $($entry.count) точек"
        if ($entry.index -eq 4 -or $entry.index -eq 9) { Assert-Contains 'ChartStatus' 'без непрерывного масштаба' }
        Ui @('wait-for', 'NativeChart', '-t', '3000')
        Capture-State "preset-$($entry.name)"
    }
}

Test-UI 'Add and remove push new typed Samples sources' {
    Baseline
    Command 'ChartAddPoint'
    Assert-Contains 'ChartStatus' 'Добавлена точка 8'
    Command 'ChartRemovePoint'
    Assert-Contains 'ChartStatus' 'Осталось 7 точек'
    Capture-State 'add-remove-point'
}
Test-UI 'Randomize refreshes native series values' {
    Command 'ChartRandomize'
    Assert-Contains 'ChartStatus' 'типизированные источники Samples обновлены'
    Capture-State 'randomized'
}
Test-UI 'Stream starts, updates native samples and stops' {
    Set-Toggle 'ChartStream' $true
    Assert-Contains 'ChartStatus' 'Поток: окно'
    Capture-State 'stream-running'
    Set-Toggle 'ChartStream' $false
    Assert-Contains 'ChartStatus' 'Поток остановлен'
}
Test-UI 'Streaming a large source trims it to the documented thirty points' {
    Set-Combo 'ChartPreset' 9 '200 точек'
    Set-Toggle 'ChartStream' $true
    Assert-Contains 'ChartStatus' 'Поток: окно 30 точек'
    Set-Toggle 'ChartStream' $false
    Capture-State 'stream-large-source'
}
Test-UI 'Legend title and visibility update the native Chart' {
    Baseline
    Ui @('set-value', 'ChartLegendTitle', 'UIA легенда')
    Assert-Value 'ChartLegendTitle' 'UIA легенда'
    Set-Toggle 'ChartShowLegend' $false
    Capture-State 'legend-hidden'
    Set-Toggle 'ChartShowLegend' $true
    Capture-State 'legend-title'
}
Test-UI 'Series visibility and type preserve the shared data' {
    Set-Toggle 'ChartSeriesVisible' $false
    Capture-State 'first-series-hidden'
    Set-Toggle 'ChartSeriesVisible' $true
    Set-Combo 'ChartSeriesType' 1 'Площадь'
    Assert-Contains 'ChartStatus' 'AreaSeries'
    Capture-State 'series-type-area'
    Set-Combo 'ChartSeriesType' 0 'Линия'
    Assert-Contains 'ChartStatus' 'LineSeries'
}
Test-UI 'Series collections can be extended and reduced' {
    Baseline
    Command 'ChartAddSeries'
    Assert-Value 'ChartSeriesSelect' '3. Серия 3'
    Capture-State 'third-series'
    Command 'ChartRemoveSeries'
    Assert-Contains 'ChartStatus' 'Серий: 2'
}
Test-UI 'Stroke, labels and markers expose every declared style family' {
    Baseline
    Set-Number 'ChartStrokeThickness' '4'
    Set-Combo 'ChartStrokeBrush' 1 'SystemFillColorSuccessBrush'
    Set-Combo 'ChartDashStyle' 3 'DashDot'
    Set-Toggle 'ChartShowLabels' $true
    Set-Toggle 'ChartShowMarkers' $true
    Set-Combo 'ChartMarkerShape' 2 'Diamond'
    Set-Combo 'ChartLabelBrush' 3 'TextFillColorPrimaryBrush'
    Set-Combo 'ChartMarkerBrush' 2 'SystemFillColorCautionBrush'
    Capture-State 'line-labels-markers'
}
Test-UI 'Area fill selects its own brush and opacity' {
    Set-Combo 'ChartSeriesType' 1 'Площадь'
    Set-Combo 'ChartFillBrush' 2 'SystemFillColorCautionBrush'
    Set-Number 'ChartFillOpacity' '0.5'
    Capture-State 'area-fill'
}
Test-UI 'Category sorting and axis presentation settings reach native axes' {
    Baseline
    Expand-Section 'ChartSection1'
    Set-Combo 'ChartCategorySortKey' 1 'Value'
    Set-Combo 'ChartCategorySortOrder' 1 'Descending'
    Set-Toggle 'ChartXTickMarks' $false
    Set-Toggle 'ChartXTickLabels' $false
    Set-Combo 'ChartXGridLines' 1 'Основная'
    Ui @('set-value', 'ChartXLabel', 'UIA ось X')
    Capture-State 'category-axis'
    Set-Toggle 'ChartXTickMarks' $true
    Set-Toggle 'ChartXTickLabels' $true
}
Test-UI 'Linear axis nullable bounds and spacing are editable' {
    Expand-Section 'ChartSection2'
    Set-Number 'ChartYMinValue' '0'
    Set-Number 'ChartYMaxValue' '150'
    Set-Number 'ChartYSpacingValue' '25'
    Set-Combo 'ChartYGridLines' 2 'Второстепенная'
    Capture-State 'linear-axis-bounds'
}
Test-UI 'Changing axis kinds replaces native axes and converts X source types' {
    Set-Combo 'ChartXAxisKind' 1 'Числа (категории)'
    Assert-Contains 'ChartStatus' 'Тип оси X'
    Assert-Contains 'ChartStatus' 'без непрерывного масштаба'
    Capture-State 'axis-converted-numeric'
    Expand-Section 'ChartSection1'
    Set-Combo 'ChartXAxisKind' 2 'Даты'
    Expand-Section 'ChartSection1'
    Set-Combo 'ChartDateInterval' 2 'Week'
    Ui @('set-value', 'ChartDateFormat', 'shortdate')
    Capture-State 'axis-converted-dates'
}
Test-UI 'LinearAxis on X reports the verified SDK restriction without changing the graph' {
    Expand-Section 'ChartSection1'
    Ui @('invoke', 'ChartProbeLinearX')
    Assert-Contains 'ChartStatus' 'SDK отклоняет LinearAxis для X'
    Assert-Contains 'ChartStatus' 'HRESULT 0x80070057'
    Ui @('wait-for', 'NativeChart', '-t', '3000')
    Capture-State 'linear-x-restriction'
}
Test-UI 'Point editor changes a real native data point' {
    Baseline
    Expand-Section 'ChartSection4'
    Set-Number 'ChartPointIndex' '2'
    Ui @('set-value', 'ChartPointX', 'UIA категория')
    Set-Number 'ChartPointY' '123'
    Ui @('invoke', 'ChartApplyPoint')
    Assert-Contains 'ChartStatus' 'Изменена точка 2 серии 1'
    Assert-Value 'ChartPointX' 'UIA категория'
    Assert-Number 'ChartPointY' 123
    Capture-State 'point-edited'
}
Test-UI 'Per-point label and marker overrides reach native maps and can be removed' {
    Expand-Section 'ChartSection5'
    Set-Toggle 'ChartLabelOverrideEnabled' $true
    Ui @('set-value', 'ChartLabelOverrideText', 'UIA выбранная точка')
    Set-Combo 'ChartLabelOverrideBrush' 0 'AccentFillColorDefaultBrush'
    Set-Toggle 'ChartMarkerOverrideEnabled' $true
    Set-Combo 'ChartMarkerOverrideShape' 3 'Triangle'
    Set-Combo 'ChartMarkerOverrideBrush' 1 'SystemFillColorSuccessBrush'
    Ui @('invoke', 'ChartApplyOverrides')
    Assert-Contains 'ChartStatus' 'подписей 1, маркеров 1'
    Capture-State 'point-overrides'
    Ui @('invoke', 'ChartClearOverrides')
    Assert-Contains 'ChartStatus' 'Переопределения выбранной серии очищены'
}
Test-UI 'Empty data can be restored with a new point' {
    Command 'ChartClearData'
    Assert-Contains 'ChartStatus' 'Источник пуст'
    Capture-State 'cleared-source'
    Command 'ChartAddPoint'
    Assert-Contains 'ChartStatus' 'Добавлена точка'
    Capture-State 'single-point'
}
Test-UI 'Return to a usable baseline' {
    Baseline
    Capture-State 'final-baseline'
}

$resultPath = Join-Path $OutputDirectory 'chart-ui-results.json'
$script:results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resultPath -Encoding utf8
$passed = @($script:results | Where-Object status -eq 'PASS').Count
$failed = @($script:results | Where-Object status -eq 'FAIL').Count
Write-Host "Chart UI results: PASS=$passed FAIL=$failed"
Write-Host "Report: $resultPath"
Write-Host 'Visual review of native lines/areas/bars, date axes, labels and clipping remains required.'
if ($failed -gt 0) { exit 1 }
exit 0
