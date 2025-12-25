param(
  [ValidateSet('Release','Debug')]
  [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

# 1) Publish (self-contained)
& (Join-Path $root 'scripts\publish.ps1') -Configuration $Configuration

# 2) Locate ISCC.exe
$possible = @(
  "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
  "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
  "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

# Try uninstall registry (per-user + machine)
$uninstallRoots = @(
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall',
  'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
)

foreach ($rootKey in $uninstallRoots) {
  if (-not (Test-Path $rootKey)) { continue }
  Get-ChildItem $rootKey | ForEach-Object {
    try { Get-ItemProperty $_.PsPath } catch { $null }
  } | Where-Object {
    $_ -and ($_.DisplayName -like '*Inno Setup*' -or $_.Publisher -like '*JRSoftware*')
  } | ForEach-Object {
    if ($_.InstallLocation) {
      $possible += (Join-Path $_.InstallLocation 'ISCC.exe')
    }
  }
}

$possible = $possible | Select-Object -Unique
$iscc = $possible | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $iscc) {
  throw "ISCC.exe not found. Install Inno Setup 6 (winget install JRSoftware.InnoSetup)."
}

$iss = Join-Path $root 'installer\WinKeySwitch.iss'
& $iscc $iss
if ($LASTEXITCODE -ne 0) {
  throw "Inno Setup compiler failed with exit code $LASTEXITCODE"
}

$setupExe = Join-Path $root 'dist\WinKeySwitchSetup.exe'
if (-not (Test-Path $setupExe)) {
  throw "Installer output not found: $setupExe"
}

Write-Host "Installer built to: $setupExe"
