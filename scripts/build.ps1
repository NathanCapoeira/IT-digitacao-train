[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Find-MSBuild {
    $programFiles = ${env:ProgramFiles(x86)}
    if (-not $programFiles) {
        $programFiles = $env:ProgramFiles
    }

    $vswhere = Join-Path $programFiles 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
        $vswhere = (Get-Command vswhere.exe -CommandType Application -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty Source)
    }

    if ($vswhere) {
        $located = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild `
            -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
        if ($LASTEXITCODE -eq 0 -and $located -and (Test-Path -LiteralPath $located -PathType Leaf)) {
            return $located
        }
    }

    $command = Get-Command MSBuild.exe -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($command) {
        return $command.Source
    }

    throw 'MSBuild was not found. Install Visual Studio Build Tools 2022 with the .NET desktop build tools workload.'
}

$buildRoot = $null
$stage = $null
try {
    if ($env:OS -ne 'Windows_NT') {
        throw 'This WPF solution requires Windows and Visual Studio Build Tools.'
    }

    $root = Split-Path -Parent $PSScriptRoot
    $solution = Join-Path $root 'ITDigitacao.sln'
    if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
        throw "Solution not found: $solution"
    }

    $msbuild = Find-MSBuild
    $programFiles = ${env:ProgramFiles(x86)}
    if (-not $programFiles) {
        $programFiles = $env:ProgramFiles
    }
    $referenceRoot = Join-Path $programFiles 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
    foreach ($file in 'mscorlib.dll', 'PresentationFramework.dll', 'RedistList\FrameworkList.xml') {
        if (-not (Test-Path -LiteralPath (Join-Path $referenceRoot $file) -PathType Leaf)) {
            throw 'Install the .NET Framework 4.8 Developer Pack or targeting pack. The runtime alone is insufficient.'
        }
    }

    $buildId = [Guid]::NewGuid().ToString('N')
    $buildRoot = Join-Path ([IO.Path]::GetTempPath()) "IT-Digitacao-$buildId"
    $binaryDir = Join-Path $buildRoot 'bin'
    $intermediateDir = Join-Path $buildRoot 'obj'
    New-Item -ItemType Directory -Path $buildRoot | Out-Null

    $buildArguments = @(
        $solution
        '/t:Rebuild'
        '/p:Configuration=Release'
        '/p:Platform=x86'
        "/p:OutDir=$binaryDir/"
        "/p:IntermediateOutputPath=$intermediateDir/"
        '/nologo'
    )
    Write-Host "MSBuild: $msbuild"
    & $msbuild @buildArguments
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -ne 0) {
        throw "MSBuild failed with exit code $buildExitCode."
    }

    $distributionFiles = 'IT-Digitacao.exe', 'IT-Digitacao.exe.config'
    foreach ($file in $distributionFiles) {
        $output = Join-Path $binaryDir $file
        if (-not (Test-Path -LiteralPath $output -PathType Leaf) -or
            (Get-Item -LiteralPath $output).Length -eq 0) {
            throw "Expected output is missing or empty: $output"
        }
    }

    $stage = Join-Path $root ".dist-stage-$buildId"
    $distribution = Join-Path $root 'dist'
    New-Item -ItemType Directory -Path $stage | Out-Null
    foreach ($file in $distributionFiles) {
        Copy-Item -LiteralPath (Join-Path $binaryDir $file) -Destination $stage
    }
    if (Test-Path -LiteralPath $distribution) {
        Remove-Item -LiteralPath $distribution -Recurse -Force
    }
    Move-Item -LiteralPath $stage -Destination $distribution

    Write-Host "Executable: $(Join-Path $distribution 'IT-Digitacao.exe')"
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
finally {
    foreach ($directory in $buildRoot, $stage) {
        if ($directory -and (Test-Path -LiteralPath $directory)) {
            Remove-Item -LiteralPath $directory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
