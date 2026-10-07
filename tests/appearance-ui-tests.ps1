param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$outputDirectory = Join-Path (Split-Path $PSScriptRoot) 'artifacts\appearance'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
function Ui([AllowEmptyString()][string[]]$Arguments) {
    $output = & $cli ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw "ui $($Arguments -join ' '): $($output | Out-String)" }
    ($output | Out-String) | ConvertFrom-Json
}
function Act([AllowEmptyString()][string[]]$Arguments) { $null = Ui $Arguments }
function Combo([string]$Id, [int]$Index) {
    Act @('invoke',$Id)
    Act @('send-keys', ('home ' + ('down ' * $Index) + 'enter'), '--via','send-input')
}
function NativeTarget([string]$Expected,[int]$StepsFromEnd) {
    Act @('invoke','ApiTarget')
    Act @('send-keys',('end ' + ('up ' * $StepsFromEnd) + 'enter'),'--via','send-input')
    Contains 'ApiTarget' $Expected
}
function Contains([string]$Id,[string]$Value) { Act @('wait-for',$Id,'--value',$Value,'--contains','-t','3000') }
function Number([string]$Id,[double]$Value) {
    $tree = Ui @('inspect',$Id,'-d','3')
    $edit = @($tree.windows[0].elements[0].children | Where-Object type -eq 'Edit')[0]
    if (-not $edit.selector) { throw "NumberBox $Id has no InputBox" }
    Act @('focus',$edit.selector)
    Act @('send-keys','ctrl+vk=0x41','--via','send-input')
    Act @('send-keys',$Value.ToString([Globalization.CultureInfo]::CurrentCulture),'--verbatim','--via','send-input')
    Act @('send-keys','tab','--via','send-input')
}
function Capture([string]$Name) {
    Start-Sleep -Milliseconds 350
    $frame = (Ui @('inspect','-d','0')).windows[0]
    Act @('screenshot','-w',[string]$frame.hwnd,'-o',(Join-Path $outputDirectory ($Name + '.png')))
}
function Check([string]$Name, [scriptblock]$Action) {
    try { & $Action; $results.Add(@{name=$Name;status='PASS'}); Write-Host "PASS $Name" }
    catch { $results.Add(@{name=$Name;status='FAIL';detail=$_.Exception.Message}); Write-Host "FAIL $Name : $($_.Exception.Message)" }
}
Check 'Prepare predictable native area and inspector targets' {
    Act @('invoke','NavTable'); Act @('invoke','TableReset')
    Act @('invoke','NavCharts'); Combo 'ChartPreset' 1
    Contains 'ChartStatus' '2 серии · 7 точек'
}
foreach ($entry in @(@{value=0.0;alpha='#00';name='transparent'},@{value=0.5;alpha='#80';name='half'},@{value=1.0;alpha='#FF';name='opaque'})) {
    Check "Area opacity $($entry.value) reaches native Color.A" {
        Number 'ChartFillOpacity' $entry.value
        Capture ('area-' + $entry.name)
        Act @('invoke','NavApi'); NativeTarget 'Series [0] AreaSeries' 4
        Act @('set-value','ApiSearch','Fill')
        $color = [string](Ui @('get-value','ApiValue')).text
        if (-not $color.StartsWith($entry.alpha)) { throw "Expected alpha $($entry.alpha); got $color" }
        Act @('invoke','NavCharts')
    }
}
Check 'Bar opacity reaches native Color.A' {
    Combo 'ChartSeriesType' 2
    Number 'ChartFillOpacity' 0.5
    Capture 'bar-half'
    Act @('invoke','NavApi'); NativeTarget 'Series [0] BarSeries' 4
    Act @('set-value','ApiSearch','Fill')
    $color = [string](Ui @('get-value','ApiValue')).text
    if (-not $color.StartsWith('#80')) { throw "Expected bar alpha #80; got $color" }
    Act @('invoke','NavCharts')
}
Check 'Dark theme preserves half-opacity and visible axes' {
    Combo 'ThemePicker' 2
    Contains 'ThemePicker' 'Тёмная'
    Capture 'chart-dark-half'
    $tree = Ui @('inspect','ChartFillOpacity','-d','3')
    $edit = @($tree.windows[0].elements[0].children | Where-Object type -eq 'Edit')[0]
    $valueText = [string](Ui @('get-value',$edit.selector)).text
    $value = [double]::Parse(($valueText -replace ',','.'), [Globalization.CultureInfo]::InvariantCulture)
    if ([math]::Abs($value - 0.5) -gt 0.001) { throw "Theme lost displayed opacity: $valueText" }
    Act @('invoke','NavApi'); NativeTarget 'Series [0] BarSeries' 4
    Act @('set-value','ApiSearch','Fill')
    $color = [string](Ui @('get-value','ApiValue')).text
    if (-not $color.StartsWith('#80')) { throw "Theme lost bar alpha: $color" }
    NativeTarget 'Axis [0]' 6
    Act @('set-value','ApiSearch','TickLabelBrush')
    $tickColor = [string](Ui @('get-value','ApiValue')).text
    if (-not $tickColor.EndsWith('FFFFFF',[StringComparison]::OrdinalIgnoreCase)) { throw "Dark axis tick text should be white; got $tickColor" }
    Act @('invoke','NavCharts')
    Capture 'chart-dark-half'
}
Check 'System theme and usable line preset restored' {
    Combo 'ThemePicker' 0
    Act @('invoke','NavCharts')
    Combo 'ChartPreset' 0
    Capture 'final-lines'
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputDirectory 'appearance-ui-results.json') -Encoding utf8
if (@($results | Where-Object status -eq 'FAIL').Count) { exit 1 }
