param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
if ($Runtime -ne 'win-x64') { throw 'This release supports win-x64 only.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
$packageVersion = (Get-Content (Join-Path $repoRoot 'package.json') -Raw | ConvertFrom-Json).version
$publishDir = Join-Path $repoRoot 'dist/windows-x64'
$packageDir = Join-Path $repoRoot 'dist/portable'
$releaseDir = Join-Path $repoRoot 'dist/release'
dotnet publish (Join-Path $repoRoot 'launcher/Windows/Launcher.csproj') -c Release -r $Runtime --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
New-Item -ItemType Directory -Force $packageDir, $releaseDir | Out-Null
$launcherName = "DiscordThemeLauncher-$packageVersion-windows-x64.exe"
$zipName = "DiscordThemeLauncher-$packageVersion-windows-x64-portable.zip"
Copy-Item -LiteralPath (Join-Path $publishDir 'DiscordThemeLauncher.exe') -Destination (Join-Path $releaseDir $launcherName)
Copy-Item -LiteralPath (Join-Path $publishDir 'DiscordThemeLauncher.exe') -Destination (Join-Path $packageDir 'DiscordThemeLauncher.exe')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $packageDir 'LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/PORTABLE-SETUP.txt') -Destination (Join-Path $packageDir 'START-HERE.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/fonts/OFL.txt') -Destination (Join-Path $packageDir 'NotoSans-OFL.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/fonts/bangers/OFL.txt') -Destination (Join-Path $packageDir 'Bangers-OFL.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/fonts/rajdhani/OFL.txt') -Destination (Join-Path $packageDir 'Rajdhani-OFL.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/fonts/jetbrains-mono/OFL.txt') -Destination (Join-Path $packageDir 'JetBrainsMono-OFL.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/community/zenit-gold/LICENSE') -Destination (Join-Path $packageDir 'ZenitGold-MIT.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets/community/zenit-gold/font/OFL.txt') -Destination (Join-Path $packageDir 'Montserrat-OFL.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/BETTERDISCORD-IMPORT.md') -Destination (Join-Path $packageDir 'CSS-IMPORT-COMPATIBILITY.md')
# The SDK resolves the bundled runtime version in project.assets.json; ship its notices.
$assetInfo = Get-Content (Join-Path $repoRoot 'launcher/Windows/obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$packageCache = @($assetInfo.packageFolders.Keys)[0]
$runtimePackages = @('Microsoft.NETCore.App.Runtime.win-x64', 'Microsoft.WindowsDesktop.App.Runtime.win-x64')
foreach ($runtimePackage in $runtimePackages) {
    $entry = @($assetInfo.project.frameworks.Values | ForEach-Object { $_.downloadDependencies } | Where-Object { $_.name -eq $runtimePackage })[0]
    if (!$entry) { throw "Runtime provenance missing: $runtimePackage" }
    $runtimeVersion = ($entry.version.Trim('[',']').Split(',')[0]).Trim()
    $licenseRoot = Join-Path $packageCache "$($runtimePackage.ToLowerInvariant())/$runtimeVersion"
    $noticeFiles = @(Get-ChildItem -LiteralPath $licenseRoot -File | Where-Object { $_.Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$' })
    if ($noticeFiles.Count -eq 0) { throw "Runtime notices missing: $runtimePackage" }
    foreach ($notice in $noticeFiles) { Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $packageDir "$runtimePackage-$($notice.Name)") }
}
$webViewEntry = @($assetInfo.libraries.Keys | Where-Object { $_ -like 'Microsoft.Web.WebView2/*' })[0]
if (!$webViewEntry) { throw 'WebView2 SDK provenance missing.' }
$webViewRoot = Join-Path $packageCache $webViewEntry.ToLowerInvariant()
foreach ($notice in @('LICENSE.txt', 'NOTICE.txt')) {
    Copy-Item -LiteralPath (Join-Path $webViewRoot $notice) -Destination (Join-Path $packageDir "WebView2-$notice")
}
$themeNames = @('PrimeBuild.theme.css','PrimeBuildDev.theme.css','FantaFort.theme.css')
foreach ($name in $themeNames) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination (Join-Path $packageDir $name)
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination (Join-Path $releaseDir $name)
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'catalog.json') -Destination (Join-Path $releaseDir 'catalog.json')
$runtimeNoticeNames = @(Get-ChildItem -LiteralPath $packageDir -File | Where-Object { $_.Name -like 'Microsoft.*' } | Select-Object -ExpandProperty FullName)
$portableFiles = @('DiscordThemeLauncher.exe','LICENSE.txt','START-HERE.txt','NotoSans-OFL.txt','Bangers-OFL.txt','Rajdhani-OFL.txt','JetBrainsMono-OFL.txt','ZenitGold-MIT.txt','Montserrat-OFL.txt','CSS-IMPORT-COMPATIBILITY.md','WebView2-LICENSE.txt','WebView2-NOTICE.txt') + $themeNames | ForEach-Object { Join-Path $packageDir $_ }
Compress-Archive -LiteralPath ($portableFiles + $runtimeNoticeNames) -DestinationPath (Join-Path $releaseDir $zipName) -Force
$releaseNames = @($launcherName,$zipName,'catalog.json') + $themeNames
$hashLines = foreach ($name in $releaseNames) { (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $releaseDir $name)).Hash.ToLowerInvariant() + '  ' + $name }
[IO.File]::WriteAllText((Join-Path $releaseDir 'SHA256SUMS.txt'), ($hashLines -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
Write-Output "Release assets ready in $releaseDir"
