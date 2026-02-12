[CmdletBinding()]
param(
  [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\SpecialPaste.Installer\SpecialPaste.Installer.csproj'
$outDir = Join-Path $repoRoot "dist\installer\$Runtime"

Write-Host "Publishing SpecialPasteInstaller (self-contained)..."
dotnet publish $project `
  -c $Configuration `
  -r $Runtime `
  --self-contained true `
  /p:SelfContained=true `
  /p:UseAppHost=true `
  /p:PublishSingleFile=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false `
  -o $outDir

Write-Host "Installer EXE: $(Join-Path $outDir 'SpecialPasteInstaller.exe')"
