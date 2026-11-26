[CmdletBinding()]
param([string]$FrameworkPathOverride)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $FrameworkPathOverride) { $FrameworkPathOverride = $env:FrameworkPathOverride }
if (-not $FrameworkPathOverride) {
    $pf = ${env:ProgramFiles(x86)}
    if (-not $pf) { $pf = $env:ProgramFiles }
    $FrameworkPathOverride = Join-Path $pf 'Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5'
}
$FrameworkPathOverride = [IO.Path]::GetFullPath($FrameworkPathOverride)
if (-not (Test-Path (Join-Path $FrameworkPathOverride 'mscorlib.dll'))) { throw "Missing .NET 3.5 reference assemblies at $FrameworkPathOverride" }
$msbuild = Get-Command MSBuild.exe -ErrorAction Stop
$project = Join-Path $root 'src\ITDigitacao.Legacy.Core\ITDigitacao.Core.csproj'
& $msbuild.Source $project /t:Rebuild /p:Configuration=Release /p:Platform=x86 "/p:FrameworkPathOverride=$FrameworkPathOverride" /nologo
if ($LASTEXITCODE -ne 0) { throw "Legacy Core build failed: $LASTEXITCODE" }
$output = Join-Path $root 'src\ITDigitacao.Legacy.Core\bin\x86\Release\ITDigitacao.Legacy.Core.dll'
if (-not (Test-Path $output)) { throw 'MSBuild did not produce the Legacy Core assembly.' }
Write-Host "Built .NET 3.5 x86 Core: $output"
