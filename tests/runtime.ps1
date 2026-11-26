param(
    [Parameter(Mandatory = $true)][string]$Extracted,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [switch]$RealSessionOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $Extracted).Path
$program = Join-Path $directory 'IT-Digitacao.exe'
$os = Get-CimInstance Win32_OperatingSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1 -ExpandProperty Name
$ramMiB = [Math]::Round($os.TotalVisibleMemorySize / 1024)
$launches = @()
$firewallNames = @()
$firewallEnforced = $false

if (-not $RealSessionOnly) {
try {
    $profiles = @(Get-NetConnectionProfile)
    $active = @($profiles | ForEach-Object {
        if ($_.NetworkCategory -eq 'DomainAuthenticated') { 'Domain' }
        else { $_.NetworkCategory.ToString() }
    } | Sort-Object -Unique)
    if ($active.Count -gt 0 -and @(Get-NetFirewallProfile -Name $active | Where-Object { -not $_.Enabled }).Count -eq 0) {
        foreach ($direction in 'Inbound','Outbound') {
            $name = "IT Digitação $direction $([Guid]::NewGuid())"
            New-NetFirewallRule -DisplayName $name -Direction $direction -Program $program -Action Block -Profile Any | Out-Null
            $firewallNames += $name
        }
        $firewallEnforced = $true
    }

    foreach ($index in 1..3) {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $process = Start-Process -FilePath $program -WorkingDirectory ([IO.Path]::GetTempPath()) -PassThru
        try {
            while ($watch.Elapsed.TotalSeconds -lt 30) {
                $process.Refresh()
                if ($process.HasExited) { throw "The extracted executable exited with code $($process.ExitCode)." }
                if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
                Start-Sleep -Milliseconds 50
            }
            if ($process.MainWindowHandle -eq [IntPtr]::Zero -or $process.MainWindowTitle -notlike 'IT Digitação*') {
                throw "Launch $index did not open the product window."
            }
            $milliseconds = $watch.ElapsedMilliseconds
            Start-Sleep -Seconds 2
            $process.Refresh()
            if ($process.HasExited) { throw "Launch $index did not remain open." }
            $launches += [ordered]@{
                Trial = $index; MainWindowMs = $milliseconds
                WorkingSetMiB = [Math]::Round($process.WorkingSet64 / 1MB, 2)
                PrivateMiB = [Math]::Round($process.PrivateMemorySize64 / 1MB, 2)
            }
            if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(8000) -or $process.ExitCode -ne 0) {
                throw "Launch $index did not close normally."
            }
        }
        finally {
            if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
            $process.Dispose()
        }
    }
}
finally {
    foreach ($name in $firewallNames) { Remove-NetFirewallRule -DisplayName $name }
}
}

$elapsed = $null
if ($RealSessionOnly) {
    $module = Join-Path $directory 'ITDigitacao.Core.dll'
    [Reflection.Assembly]::LoadFrom($module) | Out-Null
    $stream = [IO.File]::OpenRead((Join-Path $directory 'atividades.json'))
    try { $catalog = [ITDigitacao.Core.ActivityRepository]::Load($stream) } finally { $stream.Dispose() }
    $session = [ITDigitacao.Core.SessionManager]::new(5, [ITDigitacao.Core.SessionClock]::new())
    if (-not $session.Open($catalog.Activities[0]) -or $session.Started) { throw 'Clock started before input.' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $session.Apply($session.Activity.Text.Substring(0, 1))
    while (-not $session.Finished -and $watch.Elapsed.TotalSeconds -lt 310) {
        Start-Sleep -Milliseconds 200
        $session.Tick()
    }
    $elapsed = $watch.ElapsedMilliseconds
    if (-not $session.Finished -or $elapsed -lt 299900 -or $session.Remaining -ne [TimeSpan]::Zero) {
        throw 'Real five-minute session did not time out correctly.'
    }
    $session.Apply('late input')
    if ($session.Engine.Entered -ne 1) { throw 'Expired session accepted input.' }
}

$evidence = [ordered]@{
    Environment = $os.Caption; OSVersion = $os.Version; Architecture = $os.OSArchitecture
    CPU = $cpu; VisibleRamMiB = $ramMiB
    OfflineExecutableLaunch = if ($RealSessionOnly) { 'Não executado nesta etapa' } elseif ($firewallEnforced) { 'Aprovado: inbound and outbound blocked for extracted exe' }
        else { 'Bloqueado: firewall profile not enforced' }
    Launches = $launches; RealSessionMs = $elapsed
}
$evidence | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
$metrics = ($launches | ForEach-Object { "$($_.MainWindowMs) ms / $($_.WorkingSetMiB) MiB / $($_.PrivateMiB) MiB" }) -join '; '
$summary = "$($os.Caption) $($os.Version), $ramMiB MiB RAM, $cpu. Startup / working set / private: $metrics. Real-session ms: $elapsed. Offline: $($evidence.OfflineExecutableLaunch)."
Write-Host "::notice title=Runtime validation::$summary"
if ($env:GITHUB_STEP_SUMMARY) { "- $summary" | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY }
