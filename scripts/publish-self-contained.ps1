[CmdletBinding()]
param(
  [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  throw "dotnet SDK is not installed on this machine. Use prebuilt self-contained EXEs (see README: 'If target machine has no dotnet command')."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\SpecialPaste.App\SpecialPaste.App.csproj'
$outDir = Join-Path $repoRoot "dist\$Runtime"

Write-Host "Publishing self-contained SpecialPaste..."
Write-Host "Project: $project"
Write-Host "Output : $outDir"

dotnet publish $project `
  -c $Configuration `
  -r $Runtime `
  --self-contained true `
  /p:SelfContained=true `
  /p:UseAppHost=true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false `
  -o $outDir

$exe = Join-Path $outDir 'SpecialPaste.exe'
if (-not (Test-Path -LiteralPath $exe)) {
  throw "Publish finished but executable was not found at: $exe"
}

Write-Host ''
Write-Host "Done. Self-contained EXE: $exe"
Write-Host 'You can register context menus with:'
Write-Host "powershell -ExecutionPolicy Bypass -File .\scripts\register-context-menu.ps1 -ExePath `"$exe`""
