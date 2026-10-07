param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$outputDirectory = Join-Path (Split-Path $PSScriptRoot) 'artifacts\responsive'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()
function Ui([string[]]$Arguments) {
    $output = & $cli ui @Arguments -a $AppPid --json 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output | Out-String) }
    ($output | Out-String) | ConvertFrom-Json
}
function Act([string[]]$Arguments) { $null = Ui $Arguments }
function Check([string]$Name,[scriptblock]$Action) {
    try { & $Action; $results.Add(@{name=$Name;status='PASS'}); Write-Host "PASS $Name" }
    catch { $results.Add(@{name=$Name;status='FAIL';detail=$_.Exception.Message}); Write-Host "FAIL $Name : $($_.Exception.Message)" }
}
if (-not ('LabWindowSizing' -as [type])) {
    Add-Type 'using System; using System.Runtime.InteropServices; public static class LabWindowSizing { [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context); [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags); }'
}
$frame = (Ui @('inspect','-d','0')).windows[0]
$bounds = $frame.elements[0]
function Resize([int]$Width,[int]$Height) {
    $previousDpi = [LabWindowSizing]::SetThreadDpiAwarenessContext([IntPtr](-4))
    try {
        if (-not [LabWindowSizing]::SetWindowPos([IntPtr]$frame.hwnd,[IntPtr]::Zero,$bounds.x,$bounds.y,$Width,$Height,0x14)) { throw 'SetWindowPos failed' }
    } finally { $null = [LabWindowSizing]::SetThreadDpiAwarenessContext($previousDpi) }
    Start-Sleep -Milliseconds 400
}
function Capture([string]$Name) { Act @('screenshot','-w',[string]$frame.hwnd,'-o',(Join-Path $outputDirectory ($Name+'.png'))) }
try {
    Check 'Small window retains a visible native table and collapsed log' {
        Act @('invoke','NavTable'); Act @('invoke','TableReset')
        Resize ([int](770*$frame.scale)) ([int](610*$frame.scale))
        Act @('scroll','TableWorkspaceScroll','--to','top')
        Start-Sleep -Milliseconds 400
        $window = (Ui @('inspect','-d','0')).windows[0].elements[0]
        if ($window.width -gt 800*$frame.scale) { throw 'Window did not resize' }
        $table = (Ui @('inspect','LabTable','-d','0')).windows[0].elements[0]
        if ($table.isOffscreen -or $table.height -lt 150*$frame.scale) { throw 'Table is not usable in the compact layout' }
        $log = (Ui @('get-property','TableLogSection','-p','ExpandCollapseState')).properties.ExpandCollapseState
        if ($log -ne 'Collapsed') { throw "Compact log state: $log" }
        Capture 'table-narrow'
    }
    Check 'Table settings remain reachable by scrolling' {
        Act @('scroll','TableWorkspaceScroll','--to','bottom')
        Act @('scroll','TableSettingsScroll','--to','top')
        Start-Sleep -Milliseconds 400
        $setting = (Ui @('inspect','TableSource','-d','0')).windows[0].elements[0]
        if ($setting.isOffscreen) { throw 'Table settings are offscreen after scrolling' }
        Capture 'table-narrow-settings'
    }
    Check 'Small window retains Chart and its visible workspace; capture rendering' {
        Resize $bounds.width $bounds.height
        Act @('invoke','NavCharts')
        Resize ([int](770*$frame.scale)) ([int](610*$frame.scale))
        Act @('scroll','ChartWorkspaceScroll','--to','top')
        Start-Sleep -Milliseconds 400
        $chart = (Ui @('inspect','NativeChart','-d','0')).windows[0].elements[0]
        $viewport = (Ui @('inspect','ChartWorkspaceScroll','-d','0')).windows[0].elements[0]
        Capture 'chart-narrow'
        # The preview Chart's NamedContainerAutomationPeer reports a zero rectangle
        # even when it renders. Assert its presence and the real viewport, then review PNG.
        if ($chart.automationId -ne 'NativeChart' -or $viewport.isOffscreen -or $viewport.height -lt 100*$frame.scale) { throw 'Chart or its compact viewport is missing' }
    }
    Check 'Chart settings remain reachable by scrolling' {
        Act @('scroll','ChartWorkspaceScroll','--to','bottom')
        Act @('scroll','ChartSettingsScroll','--to','top')
        Start-Sleep -Milliseconds 400
        $setting = (Ui @('inspect','ChartShowLegend','-d','0')).windows[0].elements[0]
        if ($setting.isOffscreen) { throw 'Chart settings are offscreen after scrolling' }
        Capture 'chart-narrow-settings'
    }
} finally { Resize $bounds.width $bounds.height }
Check 'Wide layout restored and usable' {
    Act @('invoke','NavTable')
    $table = (Ui @('inspect','LabTable','-d','0')).windows[0].elements[0]
    if ($table.isOffscreen -or $table.width -lt 500*$frame.scale) { throw 'Wide table did not return' }
    Capture 'table-restored'
    Act @('invoke','NavCharts')
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputDirectory 'responsive-ui-results.json') -Encoding utf8
if (@($results | Where-Object status -eq 'FAIL').Count) { exit 1 }
