<#
build-release.ps1 - build + package SCANsat-GPU for a GitHub/CKAN release.

Produces under .\Releases\ :
  SCANsat-GPU-<version>.zip   the installable mod (GameData/SCANsat/... ready to drop into KSP)
  SCANsat.version            the KSP-AVC file - attach this to the GitHub release too, so the
                             "releases/latest/download/SCANsat.version" AVC URL resolves.

The zip is assembled from git-tracked GameData/SCANsat content (so gitignored build output and the
user's PluginData/Settings.cfg are excluded), with the freshly built DLLs + the build-stamped
.version overlaid on top.

Usage:   .\build-release.ps1 [-KspRoot ..\Instance] [-Version 21.1.1]
Requires: dotnet SDK, git, PowerShell 5+.
#>
[CmdletBinding()]
param(
    [string]$KspRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) "Instance"),
    [string]$Version
)
$ErrorActionPreference = "Stop"
$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$sln     = Join-Path $root "SCANsat.slnx"
$built   = Join-Path $root "GameData\SCANsat\Plugins"   # KSPBuildTools writes every project's DLL here
$verFile = Join-Path $root "GameData\SCANsat\SCANsat.version"

# Version source of truth = SCANsat.version.props (imported by Directory.Build.props; -Version overrides).
# KSPBuildTools stamps $(Version) into the assemblies and into GameData\SCANsat\SCANsat.version.
if (-not $Version) {
    [xml]$vp = Get-Content (Join-Path $root "SCANsat.version.props")
    $Version = ([string]($vp.Project.PropertyGroup.Version | Select-Object -First 1)).Trim()
}
Write-Host "==> Building SCANsat, SCANsat.Unity, SCANmechjeb (Release) v$Version against $KspRoot"
# Never let the build run ckan against the install; local .csproj.user files say the same, this is the belt.
dotnet build $sln -c Release -p:Version=$Version -p:KSPBT_GameRoot=$($KspRoot -replace '\\','/') -p:KSPBT_InstallCKANDependencies=false
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

# KSPBuildTools regenerated GameData\SCANsat\SCANsat.version with VERSION/KSP_VERSION filled in.
$ver = (Get-Content $verFile -Raw | ConvertFrom-Json).VERSION
$verStr = if ($ver -is [string]) { $ver } else { (@($ver.MAJOR,$ver.MINOR,$ver.PATCH,$ver.BUILD) | Where-Object { $null -ne $_ }) -join '.' }
Write-Host "==> Packaging version $verStr"

$stage = Join-Path $env:TEMP "scansat-gpu-stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

# 1) export git-tracked GameData/SCANsat via a zip (no gitignored DLL, no user Settings.cfg, no untracked junk)
$arch = Join-Path $env:TEMP "scansat-gpu-archive.zip"
if (Test-Path $arch) { Remove-Item $arch -Force }
git -C $root archive --format=zip -o $arch HEAD GameData/SCANsat
if ($LASTEXITCODE -ne 0) { throw "git archive failed" }
Expand-Archive -Path $arch -DestinationPath $stage -Force
Remove-Item $arch
$sat = Join-Path $stage "GameData\SCANsat"

# 2) overlay the build-stamped .version (archive shipped the pre-build template)
Copy-Item $verFile (Join-Path $sat "SCANsat.version") -Force

# 3) overlay freshly built DLLs. SCANmechjeb.dll carries a KSPAssemblyDependency on MechJeb2, so KSP
#    skips it when MechJeb is not installed; it only builds when MechJeb2.dll is in the KSP install.
$plugins = Join-Path $sat "Plugins"
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
foreach ($dll in "SCANsat.dll","SCANsat.Unity.dll","SCANmechjeb.dll") {
    $src = Join-Path $built $dll
    if (Test-Path $src) { Copy-Item $src $plugins -Force } else { Write-Warning "$dll was not built - not shipped" }
}

# 4) license must travel with the (BSD) distribution
Copy-Item (Join-Path $root "LICENSE.txt") (Join-Path $sat "LICENSE.txt") -Force

# 5) zip GameData -> dist\  (forward-slash entry names; Windows PowerShell's Compress-Archive writes
#    backslashes, which violate the ZIP spec and break CKAN's installer, so build the archive by hand)
$dist = Join-Path $root "Releases"
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$zip = Join-Path $dist "SCANsat-GPU-$verStr.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$fs = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Path (Join-Path $stage "GameData") -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($stage.Length + 1) -replace '\\','/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($fs, $_.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $fs.Dispose() }
Copy-Item $verFile (Join-Path $dist "SCANsat.version") -Force

# 6) undo the build's one side effect on tracked files (the stamped .version) so the working tree stays clean
git -C $root checkout -- GameData/SCANsat/SCANsat.version

Write-Host ""
Write-Host "==> Done."
Write-Host "    $zip"
Write-Host "    $(Join-Path $dist 'SCANsat.version')   (attach to the GitHub release)"
