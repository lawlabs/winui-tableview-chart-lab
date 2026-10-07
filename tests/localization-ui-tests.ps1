param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$outputDirectory = Join-Path (Split-Path $PSScriptRoot) 'artifacts\localization'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
function Ui([string[]]$Arguments) {
    $output = & $cli ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output | Out-String) }
}
function Combo([string]$Id,[int]$Index) {
    Ui @('invoke',$Id)
    Ui @('send-keys',('home ' + ('down ' * $Index) + 'enter'),'--via','send-input')
}
function Contains([string]$Id,[string]$Value) { Ui @('wait-for',$Id,'--value',$Value,'--contains','-t','4000') }
function Check([string]$Name,[scriptblock]$Action) {
    try { & $Action; $results.Add(@{name=$Name;status='PASS'}); Write-Host "PASS $Name" }
    catch { $results.Add(@{name=$Name;status='FAIL';detail=$_.Exception.Message}); Write-Host "FAIL $Name : $($_.Exception.Message)" }
}
Check 'English navigation and table messages' {
    Combo 'LanguagePicker' 0
    Contains 'NavOverview' 'SDK overview'
    Ui @('invoke','NavTable'); Ui @('invoke','TableReset')
    Contains 'TableStatus' 'Rows 120/120'
}
Check 'English chart scenario and dynamic message' {
    Ui @('invoke','NavCharts'); Combo 'ChartPreset' 0
    Contains 'ChartStatus' '2 series'
    Contains 'ChartStatus' '7 points'
}
Check 'English inspector navigation' {
    Ui @('invoke','NavApi'); Ui @('wait-for','ApiSearch','-t','3000')
    Ui @('set-value','ApiSearch','Density')
    Ui @('wait-for','ApiApply','-t','3000')
}
Check 'Switch to Russian preserves chart navigation and dark theme' {
    Ui @('invoke','NavCharts'); Combo 'ThemePicker' 2
    Combo 'LanguagePicker' 1
    Contains 'ThemePicker' 'Тёмная'
    Contains 'ChartStatus' '2 серии'
    Contains 'ChartStatus' '7 точек'
}
Check 'Russian table messages and English recovery' {
    Ui @('invoke','NavTable'); Ui @('invoke','TableReset')
    Contains 'TableStatus' 'Строки 120/120'
    Combo 'LanguagePicker' 0
    Contains 'ThemePicker' 'Dark'
    Contains 'TableStatus' 'Rows 120/120'
}
Check 'English chart and system theme restored' {
    Combo 'ThemePicker' 0
    Contains 'ThemePicker' 'System'
    Ui @('invoke','NavCharts'); Combo 'ChartPreset' 3
    Contains 'ChartStatus' '3 series'
    Contains 'LanguagePicker' 'English'
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputDirectory 'localization-ui-results.json') -Encoding utf8
if (@($results | Where-Object status -eq 'FAIL').Count) { exit 1 }
