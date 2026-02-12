[CmdletBinding()]
param(
  [string]$ExePath = "$PSScriptRoot\..\dist\win-x64\SpecialPaste.exe"
)

$ErrorActionPreference = 'Stop'

function Set-Verb {
  param(
    [Parameter(Mandatory = $true)][string]$KeyPath,
    [Parameter(Mandatory = $true)][string]$MenuText,
    [Parameter(Mandatory = $true)][string]$Command
  )

  New-Item -Path $KeyPath -Force | Out-Null
  Set-ItemProperty -Path $KeyPath -Name 'MUIVerb' -Value $MenuText
  Set-ItemProperty -Path $KeyPath -Name 'Icon' -Value 'imageres.dll,-5302'

  $cmdKey = "$KeyPath\command"
  New-Item -Path $cmdKey -Force | Out-Null

  # Set default value of command key (reliable for registry provider)
  Set-Item -Path $cmdKey -Value $Command
  Write-Host "Registered: $KeyPath"
}

if (-not (Test-Path -LiteralPath $ExePath)) {
  throw "SpecialPaste executable not found: $ExePath. Build/publish first (recommended: .\scripts\publish-self-contained.ps1)."
}

$exe = (Resolve-Path -LiteralPath $ExePath).Path

$exeName = [System.IO.Path]::GetFileNameWithoutExtension($ExePath)
$runtimeConfig = Join-Path ([System.IO.Path]::GetDirectoryName($ExePath)) ($exeName + '.runtimeconfig.json')
if (Test-Path -LiteralPath $runtimeConfig) {
  throw "The selected EXE appears framework-dependent ($runtimeConfig exists). Publish self-contained first: .\scripts\publish-self-contained.ps1, then register dist\win-x64\SpecialPaste.exe"
}

Write-Host "Using EXE: $exe"

# File/folder item verbs
Set-Verb -KeyPath 'HKCU:\Software\Classes\*\shell\SpecialCopyBase64' -MenuText 'Special Copy (Base64 Package)' -Command "`"$exe`" special-copy `"%1`""
Set-Verb -KeyPath 'HKCU:\Software\Classes\AllFilesystemObjects\shell\SpecialCopyBase64' -MenuText 'Special Copy (Base64 Package)' -Command "`"$exe`" special-copy `"%1`""
Set-Verb -KeyPath 'HKCU:\Software\Classes\Directory\shell\SpecialCopyBase64' -MenuText 'Special Copy (Base64 Package)' -Command "`"$exe`" special-copy `"%1`""


# Explicit shortcut classes (some Windows configurations do not honor * for .lnk/.url)
Set-Verb -KeyPath 'HKCU:\Software\Classes\lnkfile\shell\SpecialCopyBase64' -MenuText 'Special Copy (Base64 Package)' -Command "`"$exe`" special-copy `"%1`""
Set-Verb -KeyPath 'HKCU:\Software\Classes\InternetShortcut\shell\SpecialCopyBase64' -MenuText 'Special Copy (Base64 Package)' -Command "`"$exe`" special-copy `"%1`""

# Folder background + desktop verbs
Set-Verb -KeyPath 'HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard' -MenuText 'Special Paste (from Clipboard)' -Command "`"$exe`" special-paste `"%V`""
Set-Verb -KeyPath 'HKCU:\Software\Classes\DesktopBackground\Shell\SpecialPasteFromClipboard' -MenuText 'Special Paste (from Clipboard)' -Command "`"$exe`" special-paste `"%V`""
Set-Verb -KeyPath 'HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteAssemble' -MenuText 'Special Paste (Assemble Parts...)' -Command "`"$exe`" show-assembly"
Set-Verb -KeyPath 'HKCU:\Software\Classes\DesktopBackground\Shell\SpecialPasteAssemble' -MenuText 'Special Paste (Assemble Parts...)' -Command "`"$exe`" show-assembly"

Write-Host ''
Write-Host 'Context menu entries registered for current user (HKCU).'
Write-Host 'Windows 11 note: these classic verbs usually appear under "Show more options".'
Write-Host 'If Explorer was open during registration, restart Explorer or sign out/in to refresh shell cache.'
