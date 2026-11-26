[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\ITDigitacao.Legacy.Test\ITDigitacao.Legacy.Test.csproj'
$msbuild = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
if (-not $msbuild) { throw 'MSBuild não encontrado. Use Microsoft Build Tools com ferramentas desktop .NET e suporte de compilação WPF.' }
$frameworkDir = $env:FrameworkPathOverride
if ($frameworkDir) {
    $frameworkDir = [IO.Path]::GetFullPath($frameworkDir)
    if (-not (Test-Path (Join-Path $frameworkDir 'mscorlib.dll'))) { throw "FrameworkPathOverride não contém mscorlib.dll: $frameworkDir" }
} else {
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if (-not $programFilesX86) { $programFilesX86 = $env:ProgramFiles }
    $frameworkDir = Join-Path $programFilesX86 'Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5'
    if (-not (Test-Path (Join-Path $frameworkDir 'mscorlib.dll'))) { throw 'Referências .NET Framework 3.5 não encontradas via FrameworkPathOverride nem no caminho local padrão.' }
}
$winfxTargets = $env:WinFXTargetsPath
if (-not $winfxTargets -or -not (Test-Path -LiteralPath $winfxTargets -PathType Leaf)) {
    throw 'WinFXTargetsPath não definido ou inválido. O projeto requer os targets de build WPF do MSBuild.'
}
$buildArgs = @(
    $project
    '/t:Rebuild'
    '/p:Configuration=Release'
    '/p:Platform=x86'
    "/p:FrameworkPathOverride=$frameworkDir"
    "/p:WinFXTargetsPath=$winfxTargets"
    '/nologo'
)
& $msbuild.Source @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build Legacy falhou: $LASTEXITCODE" }
$built = Join-Path $root 'src\ITDigitacao.Legacy.Test\bin\x86\Release\ITDigitacao.Legacy.Test.exe'
if (-not (Test-Path $built)) { throw 'Build terminou sem produzir o executável esperado.' }
if ($OutputDirectory) { New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null; Copy-Item $built $OutputDirectory -Force }
Write-Host "Executável CLR 2.0/.NET 3.5 x86: $built"
