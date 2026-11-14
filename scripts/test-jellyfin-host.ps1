param(
    [string]$ServerExecutable = 'C:/Program Files/Jellyfin/Server/jellyfin.exe',
    [string]$Version = '3.1.0.3',
    [int]$Port = 18097,
    [string]$ThemePath = '',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root ('artifacts/host-test-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new directory for the isolated server.' }
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw 'Test port already has a listener.' }
$package = Join-Path $root "artifacts/Spesh.Base-$Version.zip"
if (-not (Test-Path -LiteralPath $package)) { throw 'Build the candidate package first.' }
$data = Join-Path $output 'data'
$config = Join-Path $output 'config'
$cache = Join-Path $output 'cache'
$logs = Join-Path $output 'logs'
$plugin = Join-Path $data "plugins/Spesh Base_$Version"
$pluginConfig = Join-Path $data 'plugins/configurations'
New-Item -ItemType Directory -Force $plugin,$pluginConfig,$config,$cache,$logs | Out-Null
Expand-Archive -LiteralPath $package -DestinationPath $plugin
@{guid='7d374fa1-67cd-4de8-9f93-fd825c14b502';name='Spesh Base';version=$Version;targetAbi='12.4.0.0';status='Active'} | ConvertTo-Json | Set-Content (Join-Path $plugin 'meta.json')
@"
<NetworkConfiguration><InternalHttpPort>$Port</InternalHttpPort><InternalHttpsPort>18921</InternalHttpsPort><EnableHttps>false</EnableHttps><EnableIPv4>true</EnableIPv4><EnableIPv6>false</EnableIPv6><AutoDiscovery>false</AutoDiscovery><EnableUPnP>false</EnableUPnP><EnableRemoteAccess>false</EnableRemoteAccess><LocalNetworkAddresses><string>127.0.0.1</string></LocalNetworkAddresses></NetworkConfiguration>
"@ | Set-Content (Join-Path $config 'network.xml')
'<PluginConfiguration><EnableSettingsSync>true</EnableSettingsSync><SeerrEnabled>false</SeerrEnabled><PushRelayEnabled>false</PushRelayEnabled><GamesEnabled>false</GamesEnabled><AnimeMarkersEnabled>false</AnimeMarkersEnabled><SmartSearchEnabled>false</SmartSearchEnabled></PluginConfiguration>' | Set-Content (Join-Path $pluginConfig 'Spesh.Base.xml')
$base = "http://127.0.0.1:$Port"
$process = $null
$headers = @{}
try {
    $arguments = @('--datadir',('"' + $data + '"'),'--configdir',('"' + $config + '"'),'--cachedir',('"' + $cache + '"'),'--logdir',('"' + $logs + '"'),'--nowebclient','--nonetchange','--service')
    $process = Start-Process -FilePath $ServerExecutable -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'stdout.log') -RedirectStandardError (Join-Path $output 'stderr.log')
    $deadline = (Get-Date).AddSeconds(55)
    do {
        try {
            $candidate = Invoke-RestMethod "$base/System/Info/Public" -TimeoutSec 2
            $startup = Invoke-WebRequest "$base/Startup/User" -TimeoutSec 2 -SkipHttpErrorCheck
            if ($candidate.Version -and $startup.StatusCode -eq 200 -and $startup.Headers['Content-Type'] -match 'application/json') { $info = $candidate; break }
        } catch {}
        Start-Sleep -Milliseconds 500
        if ($process.HasExited) { throw 'Isolated server exited; inspect its local logs.' }
    } while ((Get-Date) -lt $deadline)
    if (-not $info) { throw 'Isolated server did not become ready.' }
    if ($info.Version -ne '12.4.0') { throw 'Expected Jellyfin 12.4.0.' }
    $password = [Guid]::NewGuid().ToString('N') + '!Spesh42'
    Invoke-RestMethod "$base/Startup/User" | Out-Null
    Invoke-RestMethod "$base/Startup/User" -Method Post -ContentType 'application/json' -Body (@{Name='spesh-smoke';Password=$password} | ConvertTo-Json) | Out-Null
    Invoke-RestMethod "$base/Startup/Complete" -Method Post | Out-Null
    $headers.Authorization = 'MediaBrowser Client="Spesh Base Tests", Device="isolated-host", DeviceId="spesh-smoke", Version="1.0"'
    $auth = Invoke-RestMethod "$base/Users/AuthenticateByName" -Method Post -Headers $headers -ContentType 'application/json' -Body (@{Username='spesh-smoke';Pw=$password} | ConvertTo-Json)
    $headers['X-Emby-Token'] = $auth.AccessToken
    $headers.Authorization += ', Token="' + $auth.AccessToken + '"'
    $plugins = Invoke-RestMethod "$base/Plugins" -Headers $headers
    $plugins | Select-Object Name,Id,Version,Status | ConvertTo-Json | Set-Content (Join-Path $output 'plugins.json')
    $loaded = @($plugins | Where-Object { [guid]$_.Id -eq [guid]'7d374fa1-67cd-4de8-9f93-fd825c14b502' -and $_.Name -eq 'Spesh Base' -and $_.Status -eq 'Active' })
    if ($loaded.Count -ne 1) { throw 'Spesh Base did not load as an active plugin.' }
    $ping = Invoke-RestMethod "$base/Spesh/Ping" -Headers $headers
    $legacyPing = Invoke-RestMethod "$base/Moonfin/Ping" -Headers $headers
    if (-not $ping.installed -or $ping.version -ne $Version -or $legacyPing.version -ne $Version -or $ping.pushSupported) { throw 'Plugin ping contract mismatch.' }
    $unauthenticated = Invoke-WebRequest "$base/Spesh/Ping" -SkipHttpErrorCheck
    if ($unauthenticated.StatusCode -ne 401) { throw 'Plugin ping must require authentication.' }
    $settings = @{settings=@{schemaVersion=2;syncEnabled=$true;global=@{visualTheme='custom';customThemeId='spesha-style';hideWatchedInRecentRows=$true};tv=@{hideWatchedInRecentRows=$false}};clientId='spesh-smoke'}
    $saved = Invoke-RestMethod "$base/Spesh/Settings" -Headers $headers -Method Post -ContentType 'application/json' -Body ($settings | ConvertTo-Json -Depth 10)
    $read = Invoke-RestMethod "$base/Moonfin/Settings" -Headers $headers
    if (-not $saved.success -or $read.global.customThemeId -ne 'spesha-style' -or $read.tv.hideWatchedInRecentRows -ne $false) { throw 'Settings did not round-trip between routes.' }
    $themeVerified = $false
    if ($ThemePath) {
        $theme = Get-Content -LiteralPath $ThemePath -Raw
        $themeId = ($theme | ConvertFrom-Json).id
        Invoke-RestMethod "$base/Spesh/Admin/Themes" -Headers $headers -Method Post -ContentType 'application/json' -Body $theme | Out-Null
        $fetched = Invoke-RestMethod "$base/Moonfin/Themes/$themeId" -Headers $headers
        if ($fetched.id -ne $themeId) { throw 'Theme did not round-trip.' }
        $themeVerified = $true
    }
    $page = Invoke-WebRequest "$base/web/ConfigurationPage?name=Spesh%20Base" -Headers $headers
    if ($page.Content -notmatch '<title>Spesh Base</title>') { throw 'Admin configuration page not available.' }
    Invoke-RestMethod "$base/System/Shutdown" -Method Post -Headers $headers | Out-Null
    if (-not $process.WaitForExit(10000)) { throw 'Isolated server did not shut down cleanly.' }
    $process = Start-Process -FilePath $ServerExecutable -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'restart-stdout.log') -RedirectStandardError (Join-Path $output 'restart-stderr.log')
    $restarted = $false
    $deadline = (Get-Date).AddSeconds(55)
    do {
        try {
            $restartPing = Invoke-RestMethod "$base/Spesh/Ping" -Headers $headers -TimeoutSec 2
            if ($restartPing.installed) { $restarted = $true; break }
        } catch {}
        if ($process.HasExited) { throw 'Isolated server exited during restart.' }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    if (-not $restarted) { throw 'Isolated server did not recover after restart.' }
    $persisted = Invoke-RestMethod "$base/Spesh/Settings" -Headers $headers
    if ($persisted.global.customThemeId -ne 'spesha-style' -or $persisted.tv.hideWatchedInRecentRows -ne $false) { throw 'Settings did not survive restart.' }
    if ($ThemePath) {
        $persistedTheme = Invoke-RestMethod "$base/Spesh/Themes/$themeId" -Headers $headers
        if ($persistedTheme.id -ne $themeId) { throw 'Uploaded theme did not survive restart.' }
    }
    $report = @{ServerVersion=$info.Version;PluginVersion=$ping.version;PluginLoaded=$true;AuthenticatedRoutes=$true;UnauthenticatedDenied=$true;SettingsRoundTrip=$true;ThemeRoundTrip=$themeVerified;AdminPage=$true;RestartPersistence=$true;ProductionChanged=$false}
    $report | ConvertTo-Json | Set-Content (Join-Path $output 'report.json')
    $report | ConvertTo-Json
} finally {
    if ($process -and -not $process.HasExited) {
        if ($headers['X-Emby-Token']) { try { Invoke-RestMethod "$base/System/Shutdown" -Method Post -Headers $headers | Out-Null } catch {} }
        if (-not $process.WaitForExit(10000)) { Stop-Process -Id $process.Id -Force }
    }
}
