param(
  [ValidateSet('Release','Debug')]
  [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root 'WinKeySwitch.csproj'
$outDir = Join-Path $root 'publish\win-x64'

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Self-contained single-file publish for Windows x64
# NOTE: This makes the installer independent from .NET runtime.
dotnet publish $proj -c $Configuration -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:PublishTrimmed=false `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o $outDir

Write-Host "Published to $outDir"
