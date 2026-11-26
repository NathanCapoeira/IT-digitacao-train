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
$engine=New-Object ITDigitacao.Core.TypingEngine 'ação coração'
$engine.Apply('ação cora')
if($engine.CurrentCorrect -ne 9){throw "Typing engine accent handling failed: $($engine.CurrentCorrect)"}
if($engine.Progress -le 0){throw 'Typing progress did not advance.'}
Write-Host 'Legacy beta smoke checks passed: catalog counts, stream ownership, search, accented typing and progress.'
