[CmdletBinding()]
param(
  [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$publishScript = Join-Path $scriptDir 'publish-self-contained.ps1'
$registerScript = Join-Path $scriptDir 'register-context-menu.ps1'
$exe = Join-Path $repoRoot "dist\$Runtime\SpecialPaste.exe"

Write-Host 'Step 1/2: Publishing self-contained build...'
& powershell -ExecutionPolicy Bypass -File $publishScript -Configuration $Configuration -Runtime $Runtime

if (-not (Test-Path -LiteralPath $exe)) {
  throw "Self-contained executable missing after publish: $exe"
}

Write-Host 'Step 2/2: Registering Explorer context menu...'
& powershell -ExecutionPolicy Bypass -File $registerScript -ExePath $exe

Write-Host ''
Write-Host 'Install complete.'
Write-Host "Registered EXE: $exe"
