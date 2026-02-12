[CmdletBinding()]
param()

$keys = @(
  'HKCU:\Software\Classes\*\shell\SpecialCopyBase64',
  'HKCU:\Software\Classes\AllFilesystemObjects\shell\SpecialCopyBase64',
  'HKCU:\Software\Classes\Directory\shell\SpecialCopyBase64',
  'HKCU:\Software\Classes\lnkfile\shell\SpecialCopyBase64',
  'HKCU:\Software\Classes\InternetShortcut\shell\SpecialCopyBase64',
  'HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard',
  'HKCU:\Software\Classes\DesktopBackground\Shell\SpecialPasteFromClipboard',
  'HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteAssemble',
  'HKCU:\Software\Classes\DesktopBackground\Shell\SpecialPasteAssemble'
)

foreach ($key in $keys) {
  if (Test-Path -LiteralPath $key) {
    Remove-Item -LiteralPath $key -Recurse -Force
    Write-Host "Removed: $key"
  }
}

Write-Host 'Special Paste context menu entries removed.'
