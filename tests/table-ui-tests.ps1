[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][int]$AppPid,
    [string]$WinAppPath = (Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.7.1\tools\win-x64\winapp.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'table-ui-results'),
    # Optional stable cell selector discovered by the caller. Empty uses the table's keyboard navigation.
    [string]$FirstNameCellSelector = '',
    [switch]$SkipKeyboardEditing
)

# Run against the already-running app. No build, deploy, restart, inspect-all, or process termination.
# UIA inspect-all currently encounters stale_element in experimental TableView peers.
# Direct AutomationId selectors remain useful; combos use real Home/Down/Enter input.
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $WinAppPath -PathType Leaf)) { throw "WinApp 0.7.1 not found: $WinAppPath" }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$script:results = [System.Collections.Generic.List[object]]::new()
$script:screenshotIndex = 0
$script:editingAvailable = -not $SkipKeyboardEditing

function Invoke-Ui {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string[]]$Arguments)
    # Refresh read-only queries after a layout/peer replacement. Do not blindly replay mutations.
    $canRetryRead = $Arguments[0] -in @('get-value', 'get-property', 'wait-for', 'inspect')
    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        $nativeOutput = & $WinAppPath ui @Arguments -a $AppPid --json 2>&1
        $nativeCode = $LASTEXITCODE
        $textOutput = ($nativeOutput | Out-String).Trim()
        if ($nativeCode -eq 0) { return $textOutput }
        if ($canRetryRead -and $textOutput.Contains('stale_element') -and $attempt -lt 2) {
            Write-Host "Retry read after UIA peer replacement: $($Arguments -join ' ')" -ForegroundColor Yellow
            Start-Sleep -Milliseconds 250
            continue
        }
        throw "winapp ui $($Arguments -join ' ') exited $nativeCode`: $textOutput"
    }
}
function Ui {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string[]]$Arguments)
    [void](Invoke-Ui -Arguments $Arguments)
}
function Read-UiText {
    param([string]$Selector)
    $document = Invoke-Ui -Arguments @('get-value', $Selector) | ConvertFrom-Json
    if ($null -ne $document.text) { return [string]$document.text }
    if ($null -ne $document.value) { return [string]$document.value }
    throw "get-value '$Selector' returned an unknown schema: $($document | ConvertTo-Json -Depth 5 -Compress)"
}
function Assert-UiContains {
    param([string]$Selector, [string]$Value)
    Ui @('wait-for', $Selector, '--value', $Value, '--contains', '-t', '4000')
}
function Assert-UiValue {
    param([string]$Selector, [string]$Value)
    Ui @('wait-for', $Selector, '--value', $Value, '-t', '3000')
}
function Assert-Rows {
    param([int]$Visible, [int]$Total)
    # Number formatting follows desktop culture; normalize ordinary/thin/NBSP and commas.
    $status = Read-UiText 'TableStatus'
    $normalized = $status -replace '[\s,\u00A0\u202F]', ''
    if (-not $normalized.Contains("Строки$Visible/$Total")) { throw "Expected rows $Visible/$Total; actual: $status" }
}
function Set-Combo {
    param([string]$Selector, [int]$Index, [string]$Expected)
    Ui @('invoke', $Selector)
    $keys = 'home'
    for ($step = 0; $step -lt $Index; $step++) { $keys += ' down' }
    $keys += ' enter'
    Ui @('send-keys', $keys, '--via', 'send-input')
    if ($Expected) { Assert-UiValue $Selector $Expected }
}
function Set-Toggle {
    param([string]$Selector, [bool]$On)
    $expected = if ($On) { 'On' } else { 'Off' }
    # Explicit --action forces WinApp to walk the entire ControlView to prove uniqueness.
    # Experimental table peers may be stale outside this control; use exact ID + current state.
    if ((Read-UiText $Selector) -ne $expected) { Ui @('invoke', $Selector) }
    Assert-UiValue $Selector $expected
}
function Get-NumberBoxEditor {
    param([string]$Selector)
    # NumberBox exposes RangeValue, not ValuePattern, and is not itself focusable.
    # Inspect only this known settings control, never the experimental table tree.
    $document = Invoke-Ui -Arguments @('inspect', $Selector, '-d', '3') | ConvertFrom-Json
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
function Set-NumberBoxByKeyboard {
    param([string]$Selector, [string]$Value)
    $editor = Get-NumberBoxEditor $Selector
    Ui @('focus', $editor)
    # A raw virtual key is independent of the user's Russian/English keyboard layout.
    Ui @('send-keys', 'ctrl+vk=0x41', '--via', 'send-input')
    Ui @('send-keys', $Value, '--verbatim', '--via', 'send-input')
    Ui @('send-keys', 'tab', '--via', 'send-input')
    Assert-UiValue $editor $Value
}
function Expand-Section {
    param([string]$Name)
    $document = Invoke-Ui -Arguments @('get-property', $Name, '-p', 'ExpandCollapseState') | ConvertFrom-Json
    $state = [string]$document.properties.ExpandCollapseState
    if ($state -notin @('Expanded', 'Collapsed', 'PartiallyExpanded')) { throw "Unknown ExpandCollapseState for $Name`: '$state'" }
    if ($state -ne 'Expanded') { Ui @('invoke', $Name) }
    Ui @('wait-for', $Name, '-p', 'ExpandCollapseState', '--value', 'Expanded', '-t', '3000')
}
function Capture-State {
    param([string]$Name)
    $script:screenshotIndex++
    $fileName = '{0:00}-{1}.png' -f $script:screenshotIndex, $Name
    Ui @('screenshot', '-o', (Join-Path $OutputDirectory $fileName))
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
function Skip-UI {
    param([string]$Name, [string]$Reason)
    $script:results.Add([pscustomobject]@{ name = $Name; status = 'SKIP'; detail = $Reason })
    Write-Host "SKIP $Name`: $Reason" -ForegroundColor Yellow
}
function Start-NameEdit {
    Expand-Section 'TableEditSection'
    Ui @('set-value', 'TableSelectIndex', '0')
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=0'
    if ($FirstNameCellSelector) {
        Ui @('focus', $FirstNameCellSelector)
        Ui @('send-keys', 'f2', '--via', 'send-input')
    }
    else {
        Ui @('focus', 'LabTable')
        # ID is the first column; the editable Name column follows it.
        Ui @('send-keys', 'ctrl+home right f2', '--via', 'send-input')
    }
    Assert-UiContains 'TableStatus' 'IsEditing=True'
}

Test-UI 'Navigate to TableView and restore baseline' {
    Ui @('invoke', 'NavTable')
    Ui @('wait-for', 'TableReset', '-t', '4000')
    Ui @('invoke', 'TableReset')
    Assert-Rows 120 120
    Assert-UiContains 'TableStatus' 'столбцы 7'
    Capture-State 'baseline'
}
Test-UI 'Native smoke suite passes' {
    Ui @('invoke', 'TableSmoke')
    Assert-UiContains 'TableEventLog' 'PASS · TableView: 13 проверок'
}

$presets = @(
    @{ index = 0; label = 'Смешанные столбцы · 120 строк'; rows = 120; columns = 7; name = 'mixed' },
    @{ index = 1; label = 'Все ширины Auto'; rows = 120; columns = 7; name = 'auto' },
    @{ index = 2; label = 'Все ширины Star'; rows = 120; columns = 7; name = 'star' },
    @{ index = 3; label = 'Интерактивные шаблоны'; rows = 120; columns = 10; name = 'interactive' },
    @{ index = 4; label = '10 000 строк · виртуализация'; rows = 10000; columns = 7; name = '10000' },
    @{ index = 5; label = 'Пустая таблица'; rows = 0; columns = 7; name = 'empty' }
)
foreach ($presetEntry in $presets) {
    $entry = $presetEntry
    Test-UI "Preset $($entry.name): collection and column counts" {
        Set-Combo 'TablePreset' $entry.index $entry.label
        Assert-Rows $entry.rows $entry.rows
        Assert-UiContains 'TableStatus' "столбцы $($entry.columns)"
        if ($entry.rows -eq 0) { Ui @('wait-for', 'Таблица пуста', '-t', '3000') }
        Capture-State "preset-$($entry.name)"
    }
}

Test-UI 'Filter produces empty projection then restores all rows' {
    Ui @('invoke', 'TableReset')
    Ui @('set-value', 'TableFilter', 'UIA_NO_MATCH_8F4916')
    Assert-Rows 0 120
    Ui @('wait-for', 'Таблица пуста', '-t', '3000')
    Ui @('set-value', 'TableFilter', '')
    Assert-Rows 120 120
}
Test-UI 'High score filter changes displayed row count' {
    Set-Toggle 'TableHighOnly' $true
    $text = Read-UiText 'TableStatus'
    if ($text -match 'Строки 120/120' -or $text -match 'Строки 0/120') { throw "Filter did not retain a proper subset: $text" }
    Set-Toggle 'TableHighOnly' $false
    Assert-Rows 120 120
}
Test-UI 'Observable add and remove update projection counts' {
    Ui @('invoke', 'TableAddRow')
    Assert-Rows 121 121
    Ui @('invoke', 'TableRemoveRow')
    Assert-Rows 120 120
}
Test-UI 'Grouping excludes headers from selection and expansion restores data rows' {
    Set-Combo 'TableGroup' 1 'Category · строковый ключ'
    Assert-UiContains 'TableEventLog' 'GroupBy: 1'
    Expand-Section 'TableEditSection'
    Ui @('set-value', 'TableSelectIndex', '0')
    Ui @('invoke', 'TableDeselectAll')
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=-1'
    Ui @('invoke', 'TableCollapseAll')
    Ui @('set-value', 'TableSelectIndex', '1')
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=-1'
    Ui @('invoke', 'TableExpandAll')
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=1'
    Capture-State 'grouped-expanded'
    Set-Combo 'TableGroup' 0 'Без группировки'
    Ui @('invoke', 'TableDeselectAll')
}
Test-UI 'Computed and reference-identity grouping stages accept their keys' {
    Set-Combo 'TableGroup' 2 'Диапазон Score · вычисляемый ключ'
    Assert-UiContains 'TableEventLog' 'GroupBy: 2'
    Set-Combo 'TableGroup' 3 'Объект + IdentitySelector'
    Assert-UiContains 'TableEventLog' 'GroupBy: 3'
    Assert-Rows 120 120
    Set-Combo 'TableGroup' 0 'Без группировки'
}
Test-UI 'Sort buttons update the native column direction' {
    Expand-Section 'TableSortSection'
    Ui @('invoke', 'TableSortAsc')
    Assert-UiContains 'TableStatus' 'Имя: Ascending'
    Ui @('invoke', 'TableSortDesc')
    Assert-UiContains 'TableStatus' 'Имя: Descending'
    Ui @('invoke', 'TableCycleSort')
    Assert-UiContains 'TableStatus' 'sort glyph: ∅'
}
Test-UI 'Sorting.Cancel veto preserves the previous applied direction' {
    Ui @('invoke', 'TableSortAsc')
    Set-Toggle 'TableVetoSort' $true
    Ui @('invoke', 'TableSortDesc')
    Assert-UiContains 'TableEventLog' 'Cancel=True'
    Assert-UiContains 'TableStatus' 'Имя: Ascending'
    Set-Toggle 'TableVetoSort' $false
    Ui @('invoke', 'TableClearSort')
    Assert-UiContains 'TableStatus' 'sort glyph: ∅'
}
Test-UI 'Source path sort attributes a header; anonymous sort clears header indicators' {
    Set-Combo 'TableSortVia' 1 'Source.Sort(path, direction)'
    Ui @('invoke', 'TableSortDesc')
    Assert-UiContains 'TableStatus' 'Имя: Descending'
    Set-Combo 'TableSortVia' 2 'Source.Sort(keySelector, direction)'
    Ui @('invoke', 'TableSortAsc')
    Assert-UiContains 'TableStatus' 'sort glyph: ∅'
    Ui @('invoke', 'TableMultiSort')
    Assert-UiContains 'TableEventLog' 'Category ASC primary, Score DESC tie-breaker'
    Ui @('invoke', 'TableClearSort')
    Set-Combo 'TableSortVia' 0 'TableView.SortByColumn'
}
Test-UI 'Select, Deselect, DeselectAll and None mode update native selection' {
    Expand-Section 'TableEditSection'
    Ui @('set-value', 'TableSelectIndex', '2')
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=2'
    Ui @('invoke', 'TableDeselect')
    Assert-UiContains 'TableStatus' 'projection index=-1'
    Ui @('invoke', 'TableSelect')
    Ui @('invoke', 'TableDeselectAll')
    Assert-UiContains 'TableStatus' 'projection index=-1'
    Set-Combo 'TableSelectionMode' 0 'None'
    Ui @('invoke', 'TableSelect')
    Assert-UiContains 'TableStatus' 'projection index=-1'
    Set-Combo 'TableSelectionMode' 1 'Single'
}

if ($SkipKeyboardEditing) {
    Skip-UI 'F2 / Esc cancellation' 'Caller requested -SkipKeyboardEditing; native editing is not claimed as tested.'
    Skip-UI 'F2 / Enter commit' 'Caller requested -SkipKeyboardEditing; native editing is not claimed as tested.'
}
else {
    Test-UI 'F2 opens a text editor; Esc preserves the original row value' {
        Ui @('invoke', 'TableReset')
        try { Start-NameEdit } catch { $script:editingAvailable = $false; throw }
        Ui @('send-keys', 'ctrl+vk=0x41', '--via', 'send-input')
        Ui @('send-keys', 'UIA_CANCELLED_VALUE', '--verbatim', '--via', 'send-input')
        Ui @('send-keys', 'esc', '--via', 'send-input')
        Assert-UiContains 'TableStatus' 'IsEditing=False'
        Assert-UiContains 'TableStatus' 'Анна Смирнова 1'
        Assert-UiContains 'TableEventLog' 'Cancel; Cancel=False'
        Capture-State 'edit-cancelled'
    }
    if ($script:editingAvailable) {
        Test-UI 'F2 opens a text editor; Enter writes through to the selected model' {
            Start-NameEdit
            Ui @('send-keys', 'ctrl+vk=0x41', '--via', 'send-input')
            Ui @('send-keys', 'UIA_COMMITTED_VALUE', '--verbatim', '--via', 'send-input')
            Ui @('send-keys', 'enter', '--via', 'send-input')
            Assert-UiContains 'TableStatus' 'IsEditing=False'
            Assert-UiContains 'TableStatus' 'UIA_COMMITTED_VALUE'
            Assert-UiContains 'TableEventLog' 'Commit; Cancel=False'
            Capture-State 'edit-committed'
        }
    }
    else { Skip-UI 'F2 / Enter commit' 'The preceding cell-focus/edit opening assertion failed. Supply -FirstNameCellSelector with a stable peer selector to retry.' }
}

Test-UI 'Dynamic column add/remove updates the native Columns collection' {
    Ui @('invoke', 'TableReset')
    Expand-Section 'TableColumnSection'
    Ui @('invoke', 'TableAddColumn')
    Assert-UiContains 'TableStatus' 'столбцы 8'
    Ui @('invoke', 'TableRemoveColumn')
    Assert-UiContains 'TableStatus' 'столбцы 7'
}
Test-UI 'Column header editing reaches the applied sort indicator' {
    Ui @('invoke', 'TableReset')
    Expand-Section 'TableColumnSection'
    Ui @('set-value', 'TableColumnHeader', 'UIA колонка')
    Expand-Section 'TableSortSection'
    Ui @('invoke', 'TableSortAsc')
    Assert-UiContains 'TableStatus' 'UIA колонка: Ascending'
    Ui @('invoke', 'TableClearSort')
}
Test-UI 'Column width modes, Leading frozen and reorder settings' {
    Expand-Section 'TableColumnSection'
    Set-Combo 'TableWidthMode' 1 'Auto'
    Set-Combo 'TableWidthMode' 2 'Star'
    Set-Combo 'TableWidthMode' 0 'Pixel'
    Set-NumberBoxByKeyboard 'TableWidthValue' '210'
    # Verify the native column's resolved width through the laboratory's live readout.
    Ui @('wait-for', 'ActualWidth=210', '--root', 'TableColumnSection', '--type', 'Text', '--value', 'ActualWidth=210', '--contains', '-t', '4000')
    Set-Toggle 'TableFrozen' $false
    Set-Toggle 'TableFrozen' $true
    Ui @('invoke', 'TableColumnRight')
    Assert-UiContains 'TableEventLog' 'Columns.RemoveAt'
    Ui @('invoke', 'TableColumnLeft')
    Capture-State 'column-settings'
}
Test-UI 'Density presets and header/read-only gates are reachable' {
    Expand-Section 'TablePresentationSection'
    Set-Combo 'TableDensity' 0 'Compact'
    Set-Combo 'TableDensity' 2 'Comfortable'
    Set-Combo 'TableDensity' 1 'Standard'
    Set-Combo 'TableGridLines' 3 'Vertical'
    Set-Toggle 'TableHeaders' $false
    Set-Toggle 'TableHeaders' $true
    Set-Toggle 'TableToolTips' $false
    Set-Toggle 'TableToolTips' $true
    Expand-Section 'TableEditSection'
    Set-Toggle 'TableReadOnly' $true
    Set-Toggle 'TableReadOnly' $false
    Capture-State 'presentation-settings'
}
Test-UI 'Raw ObservableCollection mode disables external shaping inputs' {
    Set-Combo 'TableSource' 1 'ObservableCollection напрямую'
    Ui @('wait-for', 'TableFilter', '-p', 'IsEnabled', '--value', 'False', '-t', '3000')
    Set-Combo 'TableSource' 0 'TableViewSource · все преобразования'
    Ui @('wait-for', 'TableFilter', '-p', 'IsEnabled', '--value', 'True', '-t', '3000')
}
Test-UI 'Restore a usable baseline after the test run' {
    Ui @('invoke', 'TableReset')
    Assert-Rows 120 120
    Assert-UiContains 'TableStatus' 'столбцы 7'
    Capture-State 'final-baseline'
}

$resultPath = Join-Path $OutputDirectory 'table-ui-results.json'
$script:results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $resultPath -Encoding utf8
$passed = @($script:results | Where-Object status -eq 'PASS').Count
$failed = @($script:results | Where-Object status -eq 'FAIL').Count
$skipped = @($script:results | Where-Object status -eq 'SKIP').Count
Write-Host "Table UI results: PASS=$passed FAIL=$failed SKIP=$skipped"
Write-Host "Report: $resultPath"
Write-Host 'Screenshots still require visual review for clipping, overlap and theme correctness.'
if ($failed -gt 0) { exit 1 }
exit 0
