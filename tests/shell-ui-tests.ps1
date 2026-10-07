param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$artifactDirectory = Join-Path (Split-Path $PSScriptRoot) 'artifacts'
$results = [Collections.Generic.List[object]]::new()
function Ui([AllowEmptyString()][string[]]$Arguments) {
    $output = & $cli ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw "winapp ui $($Arguments -join ' '): $($output | Out-String)" }
}
function Check([string]$Name, [scriptblock]$Action) {
    try { & $Action; $results.Add(@{name=$Name;status='PASS'}); Write-Host "PASS $Name" }
    catch { $results.Add(@{name=$Name;status='FAIL';detail=$_.Exception.Message}); Write-Host "FAIL $Name : $($_.Exception.Message)" }
}
function Combo([string]$Id, [int]$Index) {
    Ui @('invoke',$Id)
    Ui @('send-keys', ('home ' + ('down ' * $Index) + 'enter'), '--via','send-input')
}
function Contains([string]$Id,[string]$Value) { Ui @('wait-for',$Id,'--value',$Value,'--contains','-t','3000') }
function Capture([string]$Name) { Ui @('screenshot','-o',(Join-Path $artifactDirectory $Name)) }

Check 'Inspector edits live table density' {
    Ui @('invoke','NavTable'); Ui @('invoke','TableReset')
    Ui @('invoke','NavApi'); Ui @('wait-for','ApiTarget','-t','3000')
    Combo 'ApiTarget' 0
    Ui @('set-value','ApiSearch','Density')
    Combo 'ApiEnumValue' 0
    Ui @('invoke','ApiApply')
    Contains 'ApiResult' 'Density = Compact'
    Ui @('invoke','NavTable'); Ui @('invoke','TablePresentationSection')
    Contains 'TableDensity' 'Compact'
}
Check 'Inspector restores original value' {
    Ui @('invoke','NavApi'); Ui @('wait-for','ApiRestore','-p','IsEnabled','--value','True','-t','3000'); Ui @('invoke','ApiRestore')
    Contains 'ApiResult' 'исходное значение восстановлено'
    Ui @('invoke','NavTable'); Contains 'TableDensity' 'Standard'
}
Check 'Inspector handles an empty search result' {
    Ui @('invoke','NavApi'); Ui @('set-value','ApiSearch','NO_PROPERTY_6543')
    Ui @('wait-for','ApiApply','-p','IsEnabled','--value','False','-t','3000')
    Ui @('set-value','ApiSearch','Density')
    Capture 'api-inspector.png'
}
Check 'Public API catalog exports both namespaces' {
    Ui @('invoke','ApiCatalog'); Ui @('wait-for','PrimaryButton','-t','3000')
    Ui @('invoke','PrimaryButton')
    $deadline = (Get-Date).AddSeconds(5)
    do {
        Start-Sleep -Milliseconds 100
        $catalog = Get-Clipboard -Raw
    } while (-not ($catalog.StartsWith('Windows App SDK 2.5.4-experimental') -and $catalog.Contains('Microsoft.UI.Xaml.Controls.Charts.Chart')) -and (Get-Date) -lt $deadline)
    if (-not ($catalog.Contains('Microsoft.UI.Xaml.Controls.Tabular.TableView') -and $catalog.Contains('Microsoft.UI.Xaml.Controls.Charts.Chart'))) { throw 'The generated API catalog was not copied.' }
    Set-Content -LiteralPath (Join-Path $artifactDirectory 'public-api.txt') -Value $catalog -Encoding utf8
}
Check 'Dark theme reaches the chart page' {
    Combo 'ThemePicker' 2
    Contains 'ThemePicker' 'Тёмная'
    Ui @('invoke','NavCharts'); Ui @('wait-for','NativeChart','-t','3000')
    Capture 'chart-dark.png'
}
Check 'Light theme reaches the table page' {
    Combo 'ThemePicker' 1
    Contains 'ThemePicker' 'Светлая'
    Ui @('invoke','NavTable'); Ui @('invoke','TableReset')
    Capture 'table-light.png'
}
Check 'System theme and overview are restored' {
    Combo 'ThemePicker' 0
    Ui @('invoke','NavOverview'); Ui @('wait-for','ReleaseLink','-t','3000')
    Capture 'overview.png'
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $artifactDirectory 'shell-ui-results.json') -Encoding utf8
if (@($results | Where-Object {$_.status -eq 'FAIL'}).Count) { exit 1 }
