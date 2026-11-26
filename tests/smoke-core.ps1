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
Assert-That ($repository.Categories.Count -eq 10 -and $repository.Activities.Count -eq 50) 'Unexpected catalog totals.'
foreach ($category in $repository.Categories) {
    Assert-That ($repository.Find('', $null, $category.Id).Count -eq 5) 'Unexpected category count.'
    foreach ($level in 'Iniciante', 'Intermediário', 'Avançado') {
        $count = $repository.Find('', $level, $category.Id).Count
        Assert-That ($count -ge 1 -and $count -le 2) 'A difficulty is missing.'
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

$repeated = [ITDigitacao.Core.TypingEngine]::new('ab')
$repeated.Apply('ab') | Out-Null
$repeated.ApplyEdit('abb', 1, $true) | Out-Null
Assert-That ($repeated.CorrectAttempts -eq 3 -and $repeated.CurrentErrors -eq 1) 'Repeated characters obscured an insertion position.'
$repeated.ApplyEdit('ab', 1, $false) | Out-Null
Assert-That ($repeated.Complete -and $repeated.CorrectAttempts -eq 3) 'Undo counted as new typing.'

$longActivity = $repository.Activities | Sort-Object WordCount -Descending | Select-Object -First 1
$longEngine = [ITDigitacao.Core.TypingEngine]::new($longActivity.Text)
$longEngine.Apply($longActivity.Text.Substring(0, $longActivity.Text.Length - 1)) | Out-Null
Assert-That (-not $longEngine.Complete -and $longEngine.Progress -lt 100) 'An incomplete long text was accepted.'
$longEngine.Apply($longActivity.Text) | Out-Null
Assert-That ($longActivity.WordCount -ge 160 -and $longEngine.Complete -and $longEngine.CurrentErrors -eq 0) 'Long text did not complete correctly.'

$manualClock = [TestClock]::new()
$manualSession = [ITDigitacao.Core.SessionManager]::new(5, $manualClock)
$manualSession.Open($repository.Activities[0]) | Out-Null
$manualSession.Apply('X')
$manualClock.Now = [TimeSpan]::FromSeconds(45)
$manualSession.End()
$manualClock.Now = [TimeSpan]::FromMinutes(12)
$manualSession.Apply('C')
Assert-That ($manualSession.Finished -and $manualSession.Elapsed.TotalSeconds -eq 45 -and
    $manualSession.TotalErrors -eq 1 -and $manualSession.Engine.InputText -eq 'X') 'Manual end did not freeze time and input.'

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
Assert-That ([ITDigitacao.Core.SessionAwards]::Stars(99,100) -eq 0) 'Stars were awarded below 100 attempts.'
Assert-That ([ITDigitacao.Core.SessionAwards]::Stars(100,79) -eq 1 -and
    [ITDigitacao.Core.SessionAwards]::Stars(100,80) -eq 2 -and
    [ITDigitacao.Core.SessionAwards]::Stars(100,95) -eq 3) 'Star thresholds failed.'
Assert-That ([ITDigitacao.Core.SessionAwards]::FirstMission(1) -and
    [ITDigitacao.Core.SessionAwards]::SuperPractitioner(3) -and
    -not [ITDigitacao.Core.SessionAwards]::PerfectAim(99,100)) 'Achievement thresholds failed.'
$preferenceFile = Join-Path ([IO.Path]::GetTempPath()) (([Guid]::NewGuid().ToString()) + '.json')
try {
    $defaults = [ITDigitacao.Core.Preferences]::Load($preferenceFile)
    Assert-That ($defaults.FontPoints -eq 14 -and -not $defaults.Sounds) 'Default preferences are incorrect.'
    $defaults.FontPoints = 28
    $defaults.Animations = $false
    $defaults.Sounds = $true
    $defaults.Save($preferenceFile)
    $saved = [ITDigitacao.Core.Preferences]::Load($preferenceFile)
    Assert-That ($saved.FontPoints -eq 28 -and -not $saved.Animations -and $saved.Sounds) 'Preferences did not persist.'
    ([ITDigitacao.Core.Preferences]::new()).Save($preferenceFile)
    Assert-That ([ITDigitacao.Core.Preferences]::Load($preferenceFile).Animations) 'Restoring defaults did not persist.'
    [IO.File]::WriteAllText($preferenceFile,'{')
    Assert-That ([ITDigitacao.Core.Preferences]::Load($preferenceFile).FontPoints -eq 14) 'Invalid preferences did not fall back.'
} finally { Remove-Item -LiteralPath $preferenceFile -ErrorAction SilentlyContinue }
Write-Host 'Core smoke test passed: 50 texts, 10 categories, Unicode, edits, metrics and all six session durations.'
