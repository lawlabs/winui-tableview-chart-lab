param([Parameter(Mandatory)][int]$AppPid,[switch]$TablesOnly)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$outputDirectory = Join-Path (Split-Path $PSScriptRoot) 'docs\screenshots'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
function Ui([string[]]$Arguments) {
    $output = & $cli ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output | Out-String) }
    ($output | Out-String) | ConvertFrom-Json
}
function Act([string[]]$Arguments) { $null = Ui $Arguments }
function Combo([string]$Id,[int]$Index) {
    Act @('invoke',$Id)
    Act @('send-keys',('home ' + ('down ' * $Index) + 'enter'),'--via','send-input')
}
function Capture([string]$Name) {
    Act @('focus','LanguagePicker')
    Start-Sleep -Milliseconds 500
    $frame = (Ui @('inspect','-d','0')).windows[0]
    Act @('screenshot','-w',[string]$frame.hwnd,'-o',(Join-Path $outputDirectory ($Name + '.png')))
    Write-Host "Captured $Name"
}
function SeriesNames([string[]]$Names) {
    for ($index=0; $index -lt $Names.Count; $index++) {
        Combo 'ChartSeriesSelect' $index
        Act @('set-value','ChartSeriesTitle',$Names[$index])
    }
    Combo 'ChartSeriesSelect' 0
    Act @('scroll','ChartSettingsScroll','--to','top')
}
function Number([string]$Id,[double]$Value) {
    $tree = Ui @('inspect',$Id,'-d','3')
    $edit = @($tree.windows[0].elements[0].children | Where-Object type -eq 'Edit')[0]
    Act @('focus',$edit.selector)
    Act @('send-keys','ctrl+vk=0x41','--via','send-input')
    Act @('send-keys',$Value.ToString([Globalization.CultureInfo]::InvariantCulture),'--verbatim','--via','send-input')
    Act @('send-keys','tab','--via','send-input')
}
function PrepareTable([bool]$Interactive) {
    $state = (Ui @('get-property','TableLogSection','-p','ExpandCollapseState')).properties.ExpandCollapseState
    if ($state -eq 'Expanded') {
        # Auto invoke prefers ExpandCollapse. A real Space toggles the header.
        Act @('focus','TableLogSection'); Act @('send-keys','space','--via','send-input')
    }
    Combo 'TableGroup' 0
    Act @('set-value','TableFilter','Sample record')
    Act @('invoke','TableColumnSection')
    $weights = if ($Interactive) { @(40,180,125,65,90,180,160,120,150,100) } else { @(0.4,1.6,1.2,0.6,1.0,1.2,1.0) }
    $hidden = if ($Interactive) { @(0,4,5,6) } else { @(6) }
    for ($index=0; $index -lt $weights.Count; $index++) {
        Combo 'TableColumn' $index
        Combo 'TableWidthMode' $(if ($Interactive) { 0 } else { 2 })
        Number 'TableWidthValue' $weights[$index]
        if ($index -in $hidden) { Act @('invoke','TableColumnVisible') }
        if ($index -eq 4) { Act @('set-value','TableColumnHeader','Progress') }
        if ($index -eq 5) { Act @('set-value','TableColumnHeader','Notes') }
        if ($Interactive -and $index -eq 7) { Act @('set-value','TableColumnHeader','Status') }
        if ($Interactive -and $index -eq 8) { Act @('set-value','TableColumnHeader','Details') }
        if ($Interactive -and $index -eq 9) { Act @('set-value','TableColumnHeader','Gauge') }
    }
    Act @('focus','TableColumnSection'); Act @('send-keys','space','--via','send-input')
    Act @('scroll','TableSettingsScroll','--to','top')
}
Combo 'LanguagePicker' 0
Act @('wait-for','LanguagePicker','--value','English','-t','3000')
if (-not $TablesOnly) {
Act @('invoke','NavCharts'); Combo 'ChartPreset' 3; Combo 'ThemePicker' 2
SeriesNames @('Observed','Forecast','Volume')
Capture 'chart-mixed-dark'
Combo 'ChartPreset' 0; Combo 'ThemePicker' 1
SeriesNames @('Actual','Target')
Act @('scroll-into-view','ChartShowLabels'); Act @('invoke','ChartShowLabels')
Act @('scroll','ChartSettingsScroll','--to','top')
Capture 'chart-lines-light'
Combo 'ChartPreset' 1; Combo 'ThemePicker' 2
SeriesNames @('North','South')
Capture 'chart-area-dark'
Combo 'ChartPreset' 2; Combo 'ThemePicker' 1
SeriesNames @('Revenue','Cost')
Capture 'chart-bars-light'
Combo 'ChartPreset' 5
SeriesNames @('Measured','Baseline')
Capture 'chart-dates-light'
Combo 'ChartPreset' 6; Combo 'ThemePicker' 2
SeriesNames @('Volume','Ratio')
Capture 'chart-dual-axis-dark'
}
Act @('invoke','NavTable'); Combo 'ThemePicker' 1; Act @('invoke','TableReset'); Combo 'TablePreset' 2
PrepareTable $false
Capture 'table-star-light'
Combo 'ThemePicker' 2; Combo 'TableGroup' 1
Capture 'table-grouped-dark'
Combo 'ThemePicker' 1; Act @('invoke','TableReset'); Combo 'TablePreset' 3
PrepareTable $true
Capture 'table-interactive-light'
Act @('invoke','NavApi'); Combo 'ApiTarget' 0
Act @('set-value','ApiSearch','Density')
Capture 'api-inspector-light'
Act @('invoke','NavCharts'); Combo 'ChartPreset' 3; Combo 'ThemePicker' 2
SeriesNames @('Observed','Forecast','Volume')
