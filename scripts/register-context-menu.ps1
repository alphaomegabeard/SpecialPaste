param(
  [string]$ExePath = "$PSScriptRoot\..\src\SpecialPaste.App\bin\Release\net8.0-windows\SpecialPaste.exe"
)

$exe = (Resolve-Path $ExePath).Path

# File / folder item -> Special Copy
$copyKey = "HKCU:\Software\Classes\*\shell\SpecialCopyBase64"
New-Item -Path $copyKey -Force | Out-Null
Set-ItemProperty -Path $copyKey -Name "MUIVerb" -Value "Special Copy (Base64 Package)"
Set-ItemProperty -Path $copyKey -Name "Icon" -Value $exe
New-Item -Path "$copyKey\command" -Force | Out-Null
Set-ItemProperty -Path "$copyKey\command" -Name "(default)" -Value "`"$exe`" special-copy `"%1`""

$copyDirKey = "HKCU:\Software\Classes\Directory\shell\SpecialCopyBase64"
New-Item -Path $copyDirKey -Force | Out-Null
Set-ItemProperty -Path $copyDirKey -Name "MUIVerb" -Value "Special Copy (Base64 Package)"
Set-ItemProperty -Path $copyDirKey -Name "Icon" -Value $exe
New-Item -Path "$copyDirKey\command" -Force | Out-Null
Set-ItemProperty -Path "$copyDirKey\command" -Name "(default)" -Value "`"$exe`" special-copy `"%1`""

# Folder background / desktop -> Special Paste
$pasteBgKey = "HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard"
New-Item -Path $pasteBgKey -Force | Out-Null
Set-ItemProperty -Path $pasteBgKey -Name "MUIVerb" -Value "Special Paste (from Clipboard)"
Set-ItemProperty -Path $pasteBgKey -Name "Icon" -Value $exe
New-Item -Path "$pasteBgKey\command" -Force | Out-Null
Set-ItemProperty -Path "$pasteBgKey\command" -Name "(default)" -Value "`"$exe`" special-paste `"%V`""

$assembleBgKey = "HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteAssemble"
New-Item -Path $assembleBgKey -Force | Out-Null
Set-ItemProperty -Path $assembleBgKey -Name "MUIVerb" -Value "Special Paste (Assemble Parts...)"
Set-ItemProperty -Path $assembleBgKey -Name "Icon" -Value $exe
New-Item -Path "$assembleBgKey\command" -Force | Out-Null
Set-ItemProperty -Path "$assembleBgKey\command" -Name "(default)" -Value "`"$exe`" show-assembly"

Write-Host "Context menu entries registered for current user."
