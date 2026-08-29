<#
.SYNOPSIS
    Sets up IIS hosting for EduConnect on this machine. Safe to re-run (idempotent):
    re-running redeploys the app WITHOUT touching the database or uploaded files.

.DESCRIPTION
    Must run in an ELEVATED PowerShell:
        powershell -ExecutionPolicy Bypass -File .\Setup-IIS.ps1

    By default the site binds to 127.0.0.1 only (this laptop). To make it reachable
    from other devices on your network, run with -LanAccess — and change the admin
    password first, because the demo credentials are public knowledge:
        powershell -ExecutionPolicy Bypass -File .\Setup-IIS.ps1 -LanAccess
#>

[CmdletBinding()]
param(
    [string]$SiteName    = 'EduConnect',
    [string]$AppPoolName = 'EduConnect',
    [string]$SitePath    = 'C:\inetpub\EduConnect',
    [string]$StagingPath = '',   # resolved below — $PSScriptRoot is not set during param binding in PS 5.1
    [int]   $Port        = 80,
    [int]   $HttpsPort   = 443,
    [string]$Environment = 'Production',
    [string]$HostName    = 'educonnect',   # browse https://educonnect on this machine ('' to skip)
    [switch]$LanAccess
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'   # Invoke-WebRequest is ~10x faster without the progress bar
try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch {}

$scriptDir = Split-Path -Parent $PSCommandPath
if (-not $StagingPath) { $StagingPath = Join-Path $scriptDir 'publish' }

$logFile = Join-Path $scriptDir 'iis-setup.log'
Start-Transcript -Path $logFile -Force | Out-Null

function Write-Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }

# Runs a native executable safely under $ErrorActionPreference='Stop' (stderr output
# from native tools must not throw), captures output, and enforces the exit code.
function Invoke-Native {
    param(
        [Parameter(Mandatory)] [string]  $FilePath,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [int[]] $AllowedExitCodes = @(0),
        [switch]$IgnoreErrors
    )
    $eap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @Arguments 2>&1
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $eap
    }
    if (-not $IgnoreErrors -and $AllowedExitCodes -notcontains $code) {
        $text = ($output | Out-String).Trim()
        throw "'$FilePath $($Arguments -join ' ')' failed with exit code ${code}: $text"
    }
    return $output
}

try {
    # ---------- 0. Preconditions ----------
    $isElevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isElevated) {
        throw 'This script must run in an elevated PowerShell (Run as administrator).'
    }
    if (-not (Test-Path (Join-Path $StagingPath 'EduConnect.dll'))) {
        throw "Publish output not found at '$StagingPath'. Run: dotnet publish EduConnect.csproj -c Release -o publish"
    }

    $bindAddress = if ($LanAccess) { '*' } else { '127.0.0.1' }

    # ---------- 1. Enable IIS features (before the Hosting Bundle!) ----------
    Write-Step 'Enabling IIS Windows features (skips anything already enabled)'
    $features = @(
        'IIS-WebServerRole',         # Web server role container
        'IIS-WebServer',             # Core web server (W3SVC/WAS)
        'IIS-CommonHttpFeatures',    # Common HTTP feature group
        'IIS-DefaultDocument',       # Serve default documents
        'IIS-HttpErrors',            # HTTP error pages
        'IIS-StaticContent',         # Serve css/js/uploads directly
        'IIS-RequestFiltering',      # Baseline request security
        'IIS-HttpLogging',           # W3C request logs
        'IIS-HttpCompressionStatic', # Compress static assets
        'IIS-ApplicationInit',       # Warm-up support for AlwaysRunning pools
        'IIS-WebSockets',            # Future-proofing (SignalR etc.)
        'IIS-ManagementConsole'      # IIS Manager UI (inetmgr)
    )
    $pending = @()
    foreach ($feature in $features) {
        $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue).State
        if ($state -ne 'Enabled') { $pending += $feature }
    }
    $restartNeeded = $false
    if ($pending.Count -gt 0) {
        Write-Host "   Enabling: $($pending -join ', ')"
        $featureResult = Enable-WindowsOptionalFeature -Online -FeatureName $pending -All -NoRestart
        if ($featureResult.RestartNeeded) { $restartNeeded = $true }
    } else {
        Write-Host '   All required IIS features already enabled.'
    }

    $appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
    if (-not (Test-Path $appcmd)) { throw 'appcmd.exe not found — IIS feature enablement appears to have failed. Reboot and re-run this script.' }

    # ---------- 1b. IIS must be fully provisioned (a freshly staged install
    #             completes only after a reboot) ----------
    if (-not (Test-Path "$env:windir\System32\inetsrv\config\applicationHost.config")) {
        throw 'IIS features are staged but not yet provisioned (applicationHost.config missing). RESTART WINDOWS, then re-run this script — it will pick up where it left off.'
    }

    # ---------- 2. ASP.NET Core Module V2 (Hosting Bundle) ----------
    # Modern hosting bundles install the module under Program Files\IIS; older ones
    # copied it into System32\inetsrv. Accept either.
    $ancmPaths = @(
        "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll",
        "$env:windir\System32\inetsrv\aspnetcorev2.dll"
    )
    if (-not ($ancmPaths | Where-Object { Test-Path $_ })) {
        Write-Step 'Installing the .NET 9 Hosting Bundle (ASP.NET Core Module for IIS)'
        # A pre-staged installer next to this script wins; otherwise download.
        $bundle = Join-Path $scriptDir 'dotnet-hosting-win.exe'
        if (-not (Test-Path $bundle)) {
            $bundle = Join-Path $env:TEMP 'dotnet-hosting-9-win.exe'
            Write-Host '   Downloading from https://aka.ms/dotnet/9.0/dotnet-hosting-win.exe ...'
            Invoke-WebRequest -Uri 'https://aka.ms/dotnet/9.0/dotnet-hosting-win.exe' -OutFile $bundle -UseBasicParsing
        }
        $proc = Start-Process -FilePath $bundle -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
        if ($proc.ExitCode -eq 3010) {
            $restartNeeded = $true
        } elseif ($proc.ExitCode -ne 0) {
            throw "Hosting Bundle installer failed with exit code $($proc.ExitCode)."
        }
        # Restart IIS so the module registers (required when IIS was enabled in this same run).
        Invoke-Native 'net' @('stop', 'was', '/y') -IgnoreErrors | Out-Null
        Invoke-Native 'net' @('start', 'w3svc') | Out-Null
        if (-not ($ancmPaths | Where-Object { Test-Path $_ })) { throw 'aspnetcorev2.dll still missing after Hosting Bundle install. Reboot and re-run this script.' }
    } else {
        Write-Host '   ASP.NET Core Module V2 already installed.'
    }

    # ---------- 3. Deploy files (database and uploads survive re-runs) ----------
    Write-Step "Deploying application files to $SitePath"
    New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
    # In-process hosting keeps EduConnect.dll loaded in w3wp.exe — stop the site and
    # pool before mirroring or robocopy would retry against the locked file forever.
    Invoke-Native $appcmd @('stop', 'site', $SiteName) -IgnoreErrors | Out-Null
    Invoke-Native $appcmd @('stop', 'apppool', $AppPoolName) -IgnoreErrors | Out-Null
    # /MIR keeps the target in sync with the publish output, EXCEPT the data folders
    # that belong to the running site, not the build. robocopy: 0-7 = success codes.
    Invoke-Native 'robocopy' @(
        $StagingPath, $SitePath, '/MIR', '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP',
        '/XD', (Join-Path $SitePath 'App_Data'), (Join-Path $SitePath 'wwwroot\uploads'), (Join-Path $SitePath 'logs')
    ) -AllowedExitCodes @(0, 1, 2, 3, 4, 5, 6, 7) | Out-Null

    foreach ($dir in 'App_Data', 'App_Data\keys', 'logs', 'wwwroot\uploads', 'wwwroot\uploads\materials', 'wwwroot\uploads\topics') {
        New-Item -ItemType Directory -Force -Path (Join-Path $SitePath $dir) | Out-Null
    }

    # Enable stdout logging in the DEPLOYED web.config: the aspNetCore attributes in
    # the app's own web.config override any apphost-level setting, so this is the only
    # place the flag actually takes effect. Re-applied on every run because robocopy
    # rewrites web.config from staging.
    $deployedWebConfig = Join-Path $SitePath 'web.config'
    (Get-Content $deployedWebConfig -Raw) -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"' |
        Set-Content $deployedWebConfig -Encoding UTF8

    # ---------- 4. App pool + site ----------
    Write-Step "Configuring application pool '$AppPoolName' and site '$SiteName' on ${bindAddress}:$Port"
    $pools = Invoke-Native $appcmd @('list', 'apppool', "/name:$AppPoolName") -IgnoreErrors
    if (-not $pools) {
        Invoke-Native $appcmd @('add', 'apppool', "/name:$AppPoolName") | Out-Null
    }
    # loadUserProfile:false — on this machine the User Profiles Service cannot create
    # profiles for IIS virtual accounts (event 1500 'Access is denied'), which kills
    # worker-process creation. The app does not need a user profile.
    Invoke-Native $appcmd @('set', 'apppool', $AppPoolName,
        '/managedRuntimeVersion:', '/managedPipelineMode:Integrated',
        '/startMode:AlwaysRunning', '/processModel.idleTimeout:00:00:00',
        '/processModel.loadUserProfile:false', '/processModel.setProfileEnvironment:false') | Out-Null

    # Free the port PERMANENTLY: Default Web Site also binds *:80 and races EduConnect
    # for the port at every boot (and its pool is broken on this machine), so stop it
    # AND disable its auto-start. Reversible any time from IIS Manager.
    if ($Port -eq 80 -and $SiteName -ne 'Default Web Site') {
        $defaultSite = Invoke-Native $appcmd @('list', 'site', '/name:Default Web Site') -IgnoreErrors
        if ($defaultSite) {
            Invoke-Native $appcmd @('stop', 'site', 'Default Web Site') -IgnoreErrors | Out-Null
            Invoke-Native $appcmd @('set', 'site', 'Default Web Site', '/serverAutoStart:false') -IgnoreErrors | Out-Null
            Write-Host '   Default Web Site stopped and set to not start with Windows (frees port 80 permanently).'
        }
    }

    # ---------- 4b. HTTPS certificate (self-signed, locally trusted) ----------
    # Gives the browser a clean padlock instead of the "Not secure" warning. The
    # certificate covers the friendly hostname plus localhost and both loopback IPs,
    # and is added to the machine's Trusted Root store so Chrome/Edge accept it.
    Write-Step 'Configuring HTTPS certificate'
    $certName = if ($HostName) { $HostName } else { 'localhost' }
    $friendly = 'EduConnect IIS'
    $cert = Get-ChildItem 'Cert:\LocalMachine\My' |
        Where-Object { $_.FriendlyName -eq $friendly -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
        Sort-Object NotAfter -Descending | Select-Object -First 1
    if (-not $cert) {
        $sanParts = @("DNS=$certName")
        if ($certName -ne 'localhost') { $sanParts += 'DNS=localhost' }
        $sanParts += 'IPAddress=127.0.0.1', 'IPAddress=::1'
        $cert = New-SelfSignedCertificate `
            -Subject "CN=$certName" `
            -TextExtension @("2.5.29.17={text}$($sanParts -join '&')") `
            -CertStoreLocation 'Cert:\LocalMachine\My' `
            -FriendlyName $friendly `
            -KeyExportPolicy Exportable `
            -NotAfter (Get-Date).AddYears(5)
        Write-Host "   Created certificate $($cert.Thumbprint) for $certName (valid 5 years)."
    } else {
        Write-Host "   Reusing certificate $($cert.Thumbprint)."
    }

    # Trust it machine-wide (public part only) so browsers show the padlock.
    if (-not (Get-ChildItem 'Cert:\LocalMachine\Root' | Where-Object Thumbprint -eq $cert.Thumbprint)) {
        $publicOnly = New-Object Security.Cryptography.X509Certificates.X509Certificate2(, $cert.Export('Cert'))
        $rootStore = New-Object Security.Cryptography.X509Certificates.X509Store('Root', 'LocalMachine')
        $rootStore.Open('ReadWrite')
        $rootStore.Add($publicOnly)
        $rootStore.Close()
        Write-Host '   Certificate added to Trusted Root Certification Authorities.'
    }

    # Register the certificate with http.sys for the HTTPS port (IPv4 + IPv6).
    $sslAppId = '{a3ba417c-dc1d-446b-95a5-a306ab26c1af}'
    foreach ($ipport in "0.0.0.0:$HttpsPort", "[::]:$HttpsPort") {
        Invoke-Native 'netsh' @('http', 'delete', 'sslcert', "ipport=$ipport") -IgnoreErrors | Out-Null
        Invoke-Native 'netsh' @('http', 'add', 'sslcert', "ipport=$ipport",
            "certhash=$($cert.Thumbprint)", "appid=$sslAppId", 'certstorename=MY') | Out-Null
    }

    # Local-only mode binds BOTH loopbacks: Windows resolves 'localhost' to ::1 first,
    # so an IPv4-only binding makes http://localhost fail with an http.sys 404.
    $binding = if ($LanAccess) {
        "http/*:${Port}:,https/*:${HttpsPort}:"
    } else {
        "http/127.0.0.1:${Port}:,http/[::1]:${Port}:,https/127.0.0.1:${HttpsPort}:,https/[::1]:${HttpsPort}:"
    }
    $site = Invoke-Native $appcmd @('list', 'site', "/name:$SiteName") -IgnoreErrors
    if (-not $site) {
        Invoke-Native $appcmd @('add', 'site', "/name:$SiteName", "/physicalPath:$SitePath", "/bindings:$binding") | Out-Null
    } else {
        Invoke-Native $appcmd @('set', 'site', $SiteName, "/bindings:$binding") | Out-Null
        Invoke-Native $appcmd @('set', 'vdir', "$SiteName/", "/physicalPath:$SitePath") | Out-Null
    }
    Invoke-Native $appcmd @('set', 'app', "$SiteName/", "/applicationPool:$AppPoolName") | Out-Null
    Invoke-Native $appcmd @('set', 'app', "$SiteName/", '/preloadEnabled:true') -IgnoreErrors | Out-Null

    # ---------- 5. Environment variables (stored in applicationHost.config so a
    #             redeploy that overwrites web.config never loses them) ----------
    Write-Step 'Applying hosting environment variables'
    # Absolute DB path: the app already anchors relative paths to its content root,
    # but an absolute value removes all ambiguity under IIS in-process hosting.
    $dbPath = Join-Path $SitePath 'App_Data\educonnect.db'
    $envVars = @(
        @{ name = 'ASPNETCORE_ENVIRONMENT';               value = $Environment },
        @{ name = 'ConnectionStrings__DefaultConnection'; value = "Data Source=$dbPath" },
        @{ name = 'Seed__SeedDemoData';                   value = 'true' },
        # Redirect plain HTTP to the HTTPS binding so browsers always show the padlock.
        @{ name = 'Security__EnforceHttps';               value = 'true' },
        @{ name = 'ASPNETCORE_HTTPS_PORT';                value = "$HttpsPort" }
    )
    foreach ($var in $envVars) {
        # Remove-if-present (ignore 'not found'), then add — idempotent across re-runs.
        Invoke-Native $appcmd @('set', 'config', $SiteName, '-section:system.webServer/aspNetCore',
            "/-environmentVariables.[name='$($var.name)']", '/commit:apphost') -IgnoreErrors | Out-Null
        Invoke-Native $appcmd @('set', 'config', $SiteName, '-section:system.webServer/aspNetCore',
            "/+environmentVariables.[name='$($var.name)',value='$($var.value)']", '/commit:apphost') | Out-Null
    }
    # ---------- 6. Permissions (least privilege) ----------
    Write-Step 'Setting folder permissions'
    $poolIdentity = "IIS AppPool\$AppPoolName"
    # Read/execute over the whole app for the worker process (inheritable, so /T is unnecessary):
    Invoke-Native 'icacls' @($SitePath, '/grant', "${poolIdentity}:(OI)(CI)RX", '/Q') | Out-Null
    # Modify only where the app writes (SQLite db + WAL/SHM, DP keys, uploads, stdout logs):
    foreach ($writable in 'App_Data', 'wwwroot\uploads', 'logs') {
        Invoke-Native 'icacls' @((Join-Path $SitePath $writable), '/grant', "${poolIdentity}:(OI)(CI)M", '/Q') | Out-Null
    }

    # ---------- 6b. Friendly hostname (hosts file, this machine only) ----------
    # Non-fatal: antivirus tools briefly lock the hosts file, so retry a few times
    # and continue with a warning rather than aborting the whole deployment.
    if ($HostName) {
        Write-Step "Mapping http://$HostName to this machine"
        $hostsFile = "$env:windir\System32\drivers\etc\hosts"
        $hostsContent = Get-Content $hostsFile -ErrorAction SilentlyContinue
        $missing = @()
        foreach ($ip in '127.0.0.1', '::1') {
            $exists = $hostsContent | Where-Object { $_ -match "^\s*$([regex]::Escape($ip))\s+$([regex]::Escape($HostName))\s*$" }
            if (-not $exists) { $missing += "$ip`t$HostName" }
        }
        if ($missing.Count -gt 0) {
            $written = $false
            foreach ($attempt in 1..6) {
                try {
                    Add-Content -Path $hostsFile -Value $missing -ErrorAction Stop
                    $written = $true
                    break
                } catch {
                    Start-Sleep -Milliseconds 500
                }
            }
            if (-not $written) {
                Write-Warning "Could not update the hosts file (locked by another process). The site still works via http://localhost. To add the friendly name manually, append these lines to ${hostsFile}: $($missing -join ' | ')"
            }
        }
    }

    # ---------- 7. Firewall (only when LAN access was requested) ----------
    $ruleName = "EduConnect Web $Port/$HttpsPort"
    if ($LanAccess) {
        Write-Step 'Adding Windows Firewall rule for LAN access'
        if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
            New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow `
                -Protocol TCP -LocalPort $Port, $HttpsPort -Profile Domain, Private | Out-Null
        }
        Write-Warning 'LAN access is enabled with demo credentials seeded. Change the admin password (My Profile > Change Password) before demoing on a shared network.'
    } else {
        # Tidy up if a previous -LanAccess run created rules and we are now local-only.
        Get-NetFirewallRule -DisplayName "EduConnect*" -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue
    }

    # ---------- 8. Start and verify ----------
    Write-Step 'Starting site and warming up'

    function Start-AndProbe {
        Invoke-Native $appcmd @('start', 'apppool', $AppPoolName) -IgnoreErrors | Out-Null
        Invoke-Native $appcmd @('start', 'site', $SiteName) -IgnoreErrors | Out-Null
        foreach ($attempt in 1..20) {
            try {
                $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/" -UseBasicParsing -TimeoutSec 5
                if ($response.StatusCode -eq 200) { return $true }
            } catch { }
            # Rapid-fail protection disables the pool after repeated worker failures —
            # no point probing further once that happens.
            $state = Invoke-Native $appcmd @('list', 'apppool', $AppPoolName, '/text:state') -IgnoreErrors
            if ("$state" -match 'Stopped') { return $false }
            Start-Sleep -Seconds 2
        }
        return $false
    }

    $ok = Start-AndProbe

    if (-not $ok) {
        # Fallback for machines where the User Profiles Service cannot log on IIS
        # virtual accounts at all: run the pool as NetworkService (its profile is
        # built into Windows) and grant that identity the same permissions.
        Write-Warning 'App pool failed under its virtual account — retrying as NetworkService.'
        Invoke-Native $appcmd @('stop', 'apppool', $AppPoolName) -IgnoreErrors | Out-Null
        Invoke-Native $appcmd @('set', 'apppool', $AppPoolName, '/processModel.identityType:NetworkService') | Out-Null
        Invoke-Native 'icacls' @($SitePath, '/grant', 'NT AUTHORITY\NETWORK SERVICE:(OI)(CI)RX', '/Q') | Out-Null
        foreach ($writable in 'App_Data', 'wwwroot\uploads', 'logs') {
            Invoke-Native 'icacls' @((Join-Path $SitePath $writable), '/grant', 'NT AUTHORITY\NETWORK SERVICE:(OI)(CI)M', '/Q') | Out-Null
        }
        $ok = Start-AndProbe
    }

    if ($ok) {
        Write-Host ''
        $httpsSuffix = if ($HttpsPort -ne 443) { ":$HttpsPort" } else { '' }
        Write-Host '================ SUCCESS ================' -ForegroundColor Green
        if ($HostName) { Write-Host " EduConnect is live:  https://${HostName}${httpsSuffix}" }
        Write-Host " Also reachable at:   https://localhost${httpsSuffix}"
        Write-Host ' Plain http:// URLs redirect to https automatically.'
        if ($LanAccess) {
            $lanIp = (Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp, Manual -ErrorAction SilentlyContinue |
                Where-Object { $_.IPAddress -notlike '169.254*' -and $_.IPAddress -ne '127.0.0.1' } |
                Select-Object -First 1).IPAddress
            if ($lanIp) { Write-Host " From other devices:  http://${lanIp}:$Port" }
        } else {
            Write-Host ' Local-only binding. Re-run with -LanAccess to open it to your network.'
        }
        Write-Host ' The site starts automatically with Windows.'
        Write-Host '========================================='
    } else {
        Write-Warning "Site did not answer with 200 yet. Check $SitePath\logs\stdout*.log and Event Viewer > Application (source: 'IIS AspNetCore Module V2')."
        if ($restartNeeded) { Write-Warning 'A Windows restart is pending from feature/bundle installation — reboot and re-run this script.' }
        exit 2
    }

    if ($restartNeeded) {
        Write-Warning 'Windows reported a pending restart during setup. The site is working now, but if anything misbehaves after the next boot, simply re-run this script.'
    }
}
catch {
    Write-Host ''
    Write-Host "SETUP FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Full log: $logFile"
    exit 1
}
finally {
    Stop-Transcript | Out-Null
}
