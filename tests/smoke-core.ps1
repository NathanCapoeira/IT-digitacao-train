param([Parameter(Mandatory = $true)][string]$Distribution)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-That([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$folder = (Resolve-Path -LiteralPath $Distribution).Path
$dll = Join-Path $folder 'ITDigitacao.Core.dll'
[Reflection.Assembly]::LoadFrom($dll) | Out-Null
Add-Type -ReferencedAssemblies $dll -TypeDefinition @'
using System;
using ITDigitacao.Core;
public sealed class TestClock : IMonotonicClock {
    public TimeSpan Now { get; set; }
}
'@

$file = [IO.File]::OpenRead((Join-Path $folder 'atividades.json'))
try { $repository = [ITDigitacao.Core.ActivityRepository]::Load($file) } finally { $file.Dispose() }
Assert-That ($repository.Categories.Count -eq 10 -and $repository.Activities.Count -eq 30) 'Unexpected catalog totals.'
foreach ($category in $repository.Categories) {
    Assert-That ($repository.Find('', $null, $category.Id).Count -eq 3) 'Unexpected category count.'
    foreach ($level in 'Iniciante', 'Intermediário', 'Avançado') {
        Assert-That ($repository.Find('', $level, $category.Id).Count -eq 1) 'A difficulty is missing.'
    }
}
Assert-That ($repository.Find('CIENCIAS', 'Avançado', $null).Count -eq 1) 'Combined filters failed.'
Assert-That ($repository.Find('O CEU E AZUL','Iniciante',$null).Count -eq 1) 'Accent-insensitive title search failed.'
Assert-That ($repository.Find('impossívelzz', $null, $null).Count -eq 0) 'Query should have no results.'
$invalid = [IO.MemoryStream]::new([Text.Encoding]::UTF8.GetBytes('{'))
try {
    $rejected = $false
    try { [ITDigitacao.Core.ActivityRepository]::Load($invalid) | Out-Null }
    catch { $rejected = $_.Exception.InnerException -is [IO.InvalidDataException] }
    Assert-That $rejected 'Malformed catalog was not rejected.'
} finally { $invalid.Dispose() }

$lineFeed = [string][char]10
$sample = 'ÁÀÂÃÉÊÍÓÔÕÚÇ ç, 42!' + $lineFeed + 'linha'
$engine = [ITDigitacao.Core.TypingEngine]::new($sample)
$engine.Apply('X') | Out-Null
$engine.Apply('') | Out-Null
$engine.Apply($sample.Normalize([Text.NormalizationForm]::FormD).Replace($lineFeed,[Environment]::NewLine)) | Out-Null
Assert-That ($engine.Complete -and $engine.ErrorAttempts -eq 1 -and $engine.Progress -eq 100) 'Unicode, line endings, or error history failed.'
$engine.Apply($sample + '!') | Out-Null
Assert-That (-not $engine.Complete -and $engine.CurrentErrors -eq 1) 'Excess input counted as complete.'
$editing = [ITDigitacao.Core.TypingEngine]::new('ação')
$editing.Apply('aão') | Out-Null
$editing.Apply('ação') | Out-Null
Assert-That ($editing.Complete -and $editing.CorrectAttempts -eq 2) 'Middle insertion duplicated committed attempts.'
$editing.Apply('aão') | Out-Null
Assert-That (-not $editing.Complete -and $editing.CorrectAttempts -eq 2) 'Deletion was counted as a new attempt.'

foreach ($minutes in 5,10,15,20,25,30) {
    $clock = [TestClock]::new()
    $session = [ITDigitacao.Core.SessionManager]::new($minutes, $clock)
    $first = $repository.Activities[0]
    $second = $repository.Activities[1]
    $session.Open($first) | Out-Null
    $clock.Now = [TimeSpan]::FromMinutes(2)
    $session.Tick()
    Assert-That (-not $session.Started -and $session.Remaining.TotalMinutes -eq $minutes -and $null -eq $session.Accuracy) 'Clock started before typing.'
    Assert-That (-not $session.CompleteActivity()) 'Incomplete activity was accepted.'
    $session.Apply('§')
    $session.Apply('')
    $session.Apply($first.Text)
    Assert-That ($session.Started -and $session.TotalErrors -eq 1 -and $session.CorrectedErrors -eq 1 -and
        $session.Accuracy -lt 100 -and $null -eq $session.WordsPerMinute) 'Accuracy, speed guard, or corrections failed.'
    $clock.Now += [TimeSpan]::FromMinutes(1)
    $speed = $session.WordsPerMinute
    Assert-That ([Math]::Abs($speed - $session.Engine.CurrentCorrect / 5.0) -lt 0.01) 'PPM formula does not match its documented denominator.'
    Assert-That ($session.CompleteActivity() -and -not $session.CompleteActivity()) 'Completion was not idempotent.'
    $session.Open($first) | Out-Null
    $session.Apply($first.Text)
    Assert-That ($session.WordsPerMinute -eq $speed -and $session.CompleteActivity() -and $session.CompletedCount -eq 1) 'Repeated text inflated totals.'
    $session.Open($second) | Out-Null
    Assert-That ($session.Remaining.TotalMinutes -eq $minutes - 1) 'Activity change reset the clock.'
    $clock.Now += [TimeSpan]::FromMinutes($minutes - 1)
    $session.Tick()
    Assert-That ($session.Finished -and $session.Remaining -eq [TimeSpan]::Zero -and -not $session.Open($first)) 'Automatic end failed.'
    $session.Apply('x')
    Assert-That ($session.Engine.Entered -eq 0) 'Ended session accepted input.'
    $retry = [ITDigitacao.Core.SessionManager]::new($minutes, $clock)
    $retry.End()
    Assert-That ($retry.Finished -and $retry.CompletedCount -eq 0 -and $null -eq $retry.Accuracy) 'New or manual session did not reset metrics.'
}
Write-Host 'Core smoke test passed: 30 texts, 10 categories, Unicode, edits, metrics and all six session durations.'
