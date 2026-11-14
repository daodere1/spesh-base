param([string]$Version = '3.1.0.5', [int]$Port = 18197)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ($Port -lt 1024 -or $Port -gt 65534) { throw 'Choose an unprivileged port with room for the fixture on port + 1.' }
$mockPort = $Port + 1
if (Get-NetTCPConnection -LocalPort $mockPort -State Listen -ErrorAction SilentlyContinue) { throw 'Sonarr fixture port is already in use.' }
$run = Join-Path $root ('artifacts/sonarr-host-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $run | Out-Null
$requestsPath = Join-Path $run 'requests.json'
$fixture = Join-Path $root 'Jellyfin/tests/fixtures/sonarr-calendar-server.cjs'
$node = (Get-Command node -ErrorAction Stop).Source
$process = Start-Process -FilePath $node -ArgumentList @(('"' + $fixture + '"'), $mockPort, ('"' + $requestsPath + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'fixture.stdout.log') -RedirectStandardError (Join-Path $run 'fixture.stderr.log')
try {
    & (Join-Path $PSScriptRoot 'test-jellyfin-host.ps1') -Version $Version -Port $Port -DirectSonarrTestUrl "http://127.0.0.1:$mockPort/sonarr" -OutputDirectory (Join-Path $run 'host')
    $requests = Get-Content $requestsPath -Raw | ConvertFrom-Json
    if ($requests.Count -ne 2) { throw 'Expected one Sonarr fetch per server start; same-window route aliases must share cache.' }
    $start = [DateTime]::UtcNow.Date.AddDays(-14).ToString('yyyy-MM-dd')
    $end = [DateTime]::UtcNow.Date.AddDays(90).ToString('yyyy-MM-dd')
    foreach ($request in $requests) {
        if ($request -ne "/sonarr/api/v3/calendar?start=$start&end=$end&includeSeries=true&includeEpisodeImages=true&unmonitored=true") { throw 'Calendar request did not obey window and query policy.' }
    }
    Write-Output 'Verified direct Sonarr without Seerr, API-key header, URL base, UTC time, authentication, cache, clamping, and restart.'
    Write-Output "Report: $(Join-Path $run 'host/report.json')"
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
}
