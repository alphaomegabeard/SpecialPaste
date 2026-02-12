$keys = @(
  "HKCU:\Software\Classes\*\shell\SpecialCopyBase64",
  "HKCU:\Software\Classes\Directory\shell\SpecialCopyBase64",
  "HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard",
  "HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteAssemble"
)

foreach ($key in $keys) {
  if (Test-Path $key) {
    Remove-Item -Path $key -Recurse -Force
  }
}

Write-Host "Special Paste context menu entries removed."
