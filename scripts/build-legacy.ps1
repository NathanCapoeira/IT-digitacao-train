[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\ITDigitacao.Legacy.Test\ITDigitacao.Legacy.Test.csproj'
$msbuild = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
if (-not $msbuild) { throw 'MSBuild não encontrado. Use Microsoft Build Tools com ferramentas desktop .NET e suporte de compilação WPF.' }
$frameworkDir = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5'
if (-not (Test-Path $frameworkDir)) { throw 'Referências do .NET Framework 3.5 não encontradas. Instale-as apenas na máquina de desenvolvimento.' }
& $msbuild.Source $project /t:Rebuild /p:Configuration=Release /p:Platform=x86 /nologo
if ($LASTEXITCODE -ne 0) { throw "Build Legacy falhou: $LASTEXITCODE" }
$built = Join-Path $root 'src\ITDigitacao.Legacy.Test\bin\x86\Release\ITDigitacao.Legacy.Test.exe'
if (-not (Test-Path $built)) { throw 'Build terminou sem produzir o executável esperado.' }
if ($OutputDirectory) { New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null; Copy-Item $built $OutputDirectory -Force }
Write-Host "Executável CLR 2.0/.NET 3.5 x86: $built"
