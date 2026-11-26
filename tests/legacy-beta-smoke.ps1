$ErrorActionPreference='Stop'
$core=(Resolve-Path 'src/ITDigitacao.Legacy.Core/bin/x86/Release/ITDigitacao.Legacy.Core.dll').Path
Add-Type -Path $core
$catalogPath=(Resolve-Path 'src/ITDigitacao.Legacy.App/atividades.json').Path
$stream=[IO.File]::OpenRead($catalogPath)
try {
 $repository=[ITDigitacao.Core.ActivityRepository]::Load($stream)
 if($repository.Categories.Count -ne 10){throw "Expected 10 categories, got $($repository.Categories.Count)"}
 if($repository.Activities.Count -ne 50){throw "Expected 50 activities, got $($repository.Activities.Count)"}
 if(!$stream.CanRead){throw 'ActivityRepository.Load closed its caller-owned stream.'}
 if($repository.Find('tecnologia',$null,$null).Count -lt 1){throw 'Catalog search failed.'}
} finally {$stream.Dispose()}
# Build Portuguese strings from Unicode code points so PowerShell 5.1 ANSI fallback cannot alter the test data.
$cedilla=[char]0x00E7; $tildeA=[char]0x00E3
$expected='a'+$cedilla+$tildeA+'o cora'+$cedilla+$tildeA+'o'
$typed='a'+$cedilla+$tildeA+'o cora'
$engine=[Activator]::CreateInstance([ITDigitacao.Core.TypingEngine],[object[]]@($expected))
$engine.Apply($typed)
# CurrentCorrect counts matching Unicode text elements; the entered prefix has 9 elements.
if($engine.Entered -ne 9){throw "Typing input should contain 9 text elements, got $($engine.Entered)"}
if($engine.CurrentCorrect -ne 9){throw "Typing engine accent handling failed: $($engine.CurrentCorrect)"}
if($engine.CurrentErrors -ne 0){throw "Typing engine reported unexpected errors: $($engine.CurrentErrors)"}
if($engine.Progress -le 0){throw 'Typing progress did not advance.'}
Write-Host 'Legacy beta smoke checks passed: catalog counts, stream ownership, search, accented typing and progress.'
