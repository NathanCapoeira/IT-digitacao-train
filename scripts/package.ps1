[CmdletBinding()]
param(
    [string]$Distribution,
    [string]$Output,
    [string]$Branch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$distributionPath = (Resolve-Path -LiteralPath $Distribution).Path
$outputPath = [IO.Path]::GetFullPath($Output)
$commit = git -C $root rev-parse --verify HEAD
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve the source commit.' }
if ($env:GITHUB_SHA -and $commit -ne $env:GITHUB_SHA) { throw 'Checkout does not match the workflow commit.' }
if (-not $Branch) {
    $Branch = git -C $root rev-parse --abbrev-ref HEAD
    if ($LASTEXITCODE -ne 0 -or $Branch -eq 'HEAD') { throw 'Supply the source branch for a detached checkout.' }
}

$version = '2.0.0-rc.1'
$exeName = 'IT-Digitacao.exe'
$required = @($exeName, "$exeName.config", 'ITDigitacao.Core.dll', 'atividades.json')
$files = @((Get-ChildItem -LiteralPath $distributionPath -Force).Name)
if (Compare-Object ($required | Sort-Object) ($files | Sort-Object)) { throw 'Distribution has unexpected or missing files.' }
$exe = Join-Path $distributionPath $exeName
$info = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($info.ProductVersion -ne $version -or $info.FileVersion -ne '2.0.0.0' -or
    $info.ProductName -ne 'IT Digitação' -or $info.CompanyName -ne 'Instituto de Transformação') {
    throw 'Product metadata does not match the release candidate.'
}

$reader = [IO.BinaryReader]::new([IO.File]::OpenRead($exe))
try {
    if ($reader.ReadUInt16() -ne 0x5A4D) { throw 'File is not an executable.' }
    $reader.BaseStream.Position = 0x3C
    $reader.BaseStream.Position = $reader.ReadInt32()
    if ($reader.ReadUInt32() -ne 0x4550 -or $reader.ReadUInt16() -ne 0x014C) { throw 'Executable is not PE x86.' }
}
finally { $reader.Dispose() }
Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
public static class NativeIcon {
    [DllImport("shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode)]
    public static extern uint Count(string file, int index, System.IntPtr large, System.IntPtr small, uint icons);
}
'@
if ([NativeIcon]::Count($exe, -1, [IntPtr]::Zero, [IntPtr]::Zero, 0) -eq 0) { throw 'Executable does not contain an icon.' }

$stage = Join-Path ([IO.Path]::GetTempPath()) ("it-portable-" + [Guid]::NewGuid())
$extracted = Join-Path $outputPath 'extracted'
$publisher = Join-Path $outputPath 'publish'
if ((Test-Path -LiteralPath $extracted) -or (Test-Path -LiteralPath $publisher)) { throw 'Choose a clean package output folder.' }
New-Item -ItemType Directory -Path $stage, $publisher | Out-Null
try {
    foreach ($name in $required) { Copy-Item -LiteralPath (Join-Path $distributionPath $name) -Destination $stage }
    foreach ($name in 'LEIA-ME.txt','REQUISITOS.txt') {
        Copy-Item -LiteralPath (Join-Path $root "docs\distribuicao\$name") -Destination $stage
    }
    $utc = (Get-Item -LiteralPath $exe).LastWriteTimeUtc.ToString('o')
    $buildInfo = [ordered]@{
        Product = 'IT Digitação'; Version = $version; Commit = $commit; Branch = $Branch; BuildUtc = $utc
        Configuration = 'Release'; Architecture = 'x86'; Framework = '.NET Framework 4.8'
        Project = 'src/ITDigitacao.App/ITDigitacao.App.csproj'; Executable = $exeName
        DistributionBytesBeforeBuildInfo = (Get-ChildItem -LiteralPath $stage -File | Measure-Object Length -Sum).Sum
        Workflow = $env:GITHUB_WORKFLOW; WorkflowRunId = $env:GITHUB_RUN_ID
    }
    $buildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'BUILD-INFO.json') -Encoding UTF8

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipName = "IT-Digitacao-$version-Portable-x86.zip"
    $zip = Join-Path $publisher $zipName
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $extracted)
    $originals = @(Get-ChildItem -LiteralPath $stage -File)
    $recovered = @(Get-ChildItem -LiteralPath $extracted -Force)
    if (Compare-Object ($originals.Name | Sort-Object) ($recovered.Name | Sort-Object)) { throw 'ZIP contents differ from staging.' }
    foreach ($original in $originals) {
        if ((Get-FileHash -LiteralPath $original.FullName).Hash -ne
            (Get-FileHash -LiteralPath (Join-Path $extracted $original.Name)).Hash) { throw "Extraction changed $($original.Name)." }
    }
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash *$zipName" | Set-Content -LiteralPath (Join-Path $publisher 'SHA256SUMS.txt') -Encoding ascii
    "Package SHA-256: $hash" | Write-Host
    if ($env:GITHUB_STEP_SUMMARY) {
        "- SHA-256: $hash" | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
        Write-Host "::notice title=SHA-256::$hash"
    }
}
finally { Remove-Item -LiteralPath $stage -Recurse -Force }
