param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$Diagnostics
)
$ErrorActionPreference = 'Stop'
$projectFile = Join-Path $PSScriptRoot 'SdkComponentLab.csproj'
& dotnet restore $projectFile -p:Platform=x64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$packageRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\packages' }
# Project dependency restores this tool; no machine-wide CLI update is needed.
$cliPath = Join-Path $packageRoot 'microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
if (-not (Test-Path -LiteralPath $cliPath)) { throw "WinApp CLI from the restored project was not found: $cliPath" }
foreach ($appProcess in @(Get-Process -Name SdkComponentLab -ErrorAction SilentlyContinue)) {
    if ($appProcess.Path -and $appProcess.Path.StartsWith($PSScriptRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        $null = $appProcess.CloseMainWindow()
        if (-not $appProcess.WaitForExit(3000)) { Stop-Process -Id $appProcess.Id }
    }
}
$launchArguments = @('run', $projectFile, '-c', $Configuration, '--arch', 'x64')
if ($Diagnostics) { $launchArguments += '--debug-output' } else { $launchArguments += @('--detach', '--json') }
& $cliPath @launchArguments
exit $LASTEXITCODE
