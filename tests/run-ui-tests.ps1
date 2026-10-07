param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'
$failed = $false
function Language([int]$Index) {
    & $cli ui invoke LanguagePicker -a $AppPid --json | Out-Null
    if ($LASTEXITCODE) { throw 'Cannot open the language picker.' }
    & $cli ui send-keys ('home ' + ('down ' * $Index) + 'enter') -a $AppPid --via send-input --json | Out-Null
    if ($LASTEXITCODE) { throw 'Cannot change the language.' }
}
try {
    # The original functional suites assert Russian sample text. Both languages
    # are exercised separately below; every run finishes in English.
    Language 1
    foreach ($suite in @('table','chart','appearance','shell','responsive')) {
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot "$suite-ui-tests.ps1") -AppPid $AppPid
        if ($LASTEXITCODE) { $failed = $true }
    }
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'localization-ui-tests.ps1') -AppPid $AppPid
    if ($LASTEXITCODE) { $failed = $true }
} finally { Language 0 }
if ($failed) { exit 1 }
