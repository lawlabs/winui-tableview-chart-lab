$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
function Resources([string]$Language) {
    [xml]$document = Get-Content -LiteralPath (Join-Path $root "Strings/$Language/Resources.resw") -Raw
    $map = @{}
    foreach ($entry in $document.root.data) {
        if ($map.ContainsKey($entry.name)) { throw "Duplicate $Language key: $($entry.name)" }
        if ([string]::IsNullOrWhiteSpace($entry.value)) { throw "Empty $Language key: $($entry.name)" }
        $map[$entry.name] = [string]$entry.value
    }
    return $map
}
$english = Resources 'en-US'
$russian = Resources 'ru-RU'
if (Compare-Object @($english.Keys | Sort-Object) @($russian.Keys | Sort-Object)) { throw 'The resource key sets differ.' }
foreach ($key in $english.Keys) {
    if ($english[$key] -match '[а-яА-ЯёЁ]') { throw "Russian text in English resource: $key" }
    # Validate composite-format syntax and argument identity in translated messages.
    foreach ($map in @($english,$russian)) {
        $null = [Text.CompositeFormat]::Parse($map[$key])
    }
    $enArgs = @([regex]::Matches($english[$key], '(?<!\{)\{(\d+)(?:[,}:])') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    $ruArgs = @([regex]::Matches($russian[$key], '(?<!\{)\{(\d+)(?:[,}:])') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    if (($enArgs -join ',') -ne ($ruArgs -join ',')) { throw "Translation placeholder mismatch: $key" }
}
$sourceFiles = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $_.Extension -in '.cs','.xaml' -and $_.FullName -notmatch '[\\/](bin|obj|artifacts|\.git)[\\/]'
}
foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, 'L\.(?:T|F)\("([^"]+)"')) {
        $key = $match.Groups[1].Value.Replace('/','.')
        if (-not $english.ContainsKey($key)) { throw "Missing resource $key in $($file.Name)" }
    }
    foreach ($match in [regex]::Matches($content, 'x:Uid="([^"]+)"')) {
        $prefix = $match.Groups[1].Value + '.'
        if (-not @($english.Keys | Where-Object { $_.StartsWith($prefix,[StringComparison]::Ordinal) }).Count) { throw "Missing x:Uid resources: $prefix" }
    }
}
$documents = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.md' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|\.git)[\\/]' }
foreach ($file in $documents) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    # Markdown links and HTML gallery attributes. Ignore remote URLs and anchors.
    $targets = @([regex]::Matches($content, '\]\(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value })
    $targets += @([regex]::Matches($content, '(?:href|src)="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    foreach ($target in $targets) {
        if ($target -match '^(https?://|#|mailto:)') { continue }
        $path = [Uri]::UnescapeDataString(($target -split '#')[0])
        if (-not (Test-Path -LiteralPath (Join-Path $file.DirectoryName $path))) { throw "Broken local link in $($file.FullName): $target" }
    }
}
Write-Host "PASS: $($english.Count) bilingual resources, format arguments, source references, and documentation links."
