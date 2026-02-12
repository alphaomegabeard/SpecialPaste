# Special Paste

Special Paste is a Windows desktop utility for moving files through text-only channels (RDP chat, remote CLI clipboard, ticketing systems, etc.). It converts files/folders to self-describing Base64 text packages and reconstructs them on another machine.

## Features

- **Special Copy** from Explorer context menu (file/folder).
- **Special Paste** into a destination folder from clipboard text.
- **Chunked transfer** for large payloads with out-of-order part assembly cache.
- **Tray icon UI** fallback:
  - Copy file as Base64...
  - Paste Base64 to...
  - Assembly status
  - Settings (line width, chunk size, compression, overwrite behavior)
  - Help
- **Safety checks**:
  - SHA-256 integrity validation.
  - Size checks.
  - Path traversal prevention for manifests (rejects rooted paths and `..`).
  - Never executes decoded content.
- Logs at `%LOCALAPPDATA%\SpecialCopyPaste\logs`.

## MVP scope shipped

✅ Single-file copy/paste.  
✅ Registry-based classic context menu verbs (per-user).  
✅ Clipboard text package format with header/footer.  
✅ Local package archive copy.  
✅ Multi-file/folder support (manifest + payload stream).  
✅ Chunk splitting + assembly cache/status UI.

## Build / Publish

### Recommended (no .NET runtime required on target machine)

Publish a **self-contained** single-file EXE:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-self-contained.ps1
```

Output executable (default):

`dist\win-x64\SpecialPaste.exe`

> Important: do **not** register/use `src\SpecialPaste.App\bin\Release\...\SpecialPaste.exe` on runtime-clean target machines; that build is framework-dependent and can show ".NET runtime not installed".

### Installer EXE (recommended user workflow)

Build an installer EXE (self-contained):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-installer.ps1
```

Then run:

`dist\installer\win-x64\SpecialPasteInstaller.exe`

The installer will:
- detect `dist\win-x64\SpecialPaste.exe` (or accept an explicit path argument),
- register context menu entries,
- create shortcuts:
  - Desktop: `Special Paste.lnk`
  - Start Menu Programs: `Special Paste.lnk`

### Developer build (requires .NET SDK + runtime on machine)

```powershell
cd src/SpecialPaste.App
dotnet build -c Release
```

## Context menu setup (no admin, per-user)

### Recommended one-command install (publish + register)

> Alternative: use `SpecialPasteInstaller.exe` (see **Installer EXE** above).

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-context-menu.ps1
```

### Manual register (after publish)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-context-menu.ps1 -ExePath .\dist\win-x64\SpecialPaste.exe
```

### Uninstall context menu

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\unregister-context-menu.ps1
```

### Windows 11 behavior note

- These are **classic shell verbs**. On Windows 11 they usually appear under **Show more options** (Shift+Right-click).
- The menu entries use a built-in Windows icon (`imageres.dll,-5302`) to avoid shipping binary icon assets in this repository.
- Showing these entries in the **top-level Win11 menu** is generally not possible with classic registry verbs alone; that requires a modern `IExplorerCommand`/shell extension implementation.
- If verbs do not appear immediately after registration, restart Explorer:

```powershell
taskkill /f /im explorer.exe
start explorer.exe
```

- You can verify registration with:

```powershell
Get-Item "HKCU:\Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard"
Get-Item "HKCU:\Software\Classes\*\shell\SpecialCopyBase64"
Get-Item "HKCU:\Software\Classes\lnkfile\shell\SpecialCopyBase64"
```

- Shortcuts (`.lnk` / `.url`) are also registered explicitly so **Special Copy** works reliably when right-clicking desktop shortcuts.

## Usage

### A) Explorer context menu

1. Right-click file/folder → **Special Copy (Base64 Package)**.
2. Paste text package through remote channel to destination machine clipboard.
3. Right-click destination folder background → **Special Paste (from Clipboard)**.
4. If chunked package parts are used, paste each chunk and then use **Special Paste (Assemble Parts...)** in tray/CLI.

### B) CLI handler commands (used by context menu)

```powershell
SpecialPaste.exe special-copy "C:\path\to\file-or-folder"
SpecialPaste.exe special-paste "C:\destination\folder"
SpecialPaste.exe show-assembly
SpecialPaste.exe special-assemble "<package-guid>" "C:\destination\folder"
SpecialPasteInstaller.exe "C:\path\to\SpecialPaste.exe"
```

### C) Tray app

Launch `SpecialPaste.exe` with no arguments (recommended from `dist\win-x64\SpecialPaste.exe`).

## Package format

Text package envelope:

```text
-----BEGIN SPECIALCOPY-----
type=single|multi|chunk
package_id=<guid>
timestamp_utc=<iso8601>
name=<name>
size=<original bytes>
stored_size=<payload bytes>
sha256=<hex lowercase>
compress=none|gzip
part_index=<n>
part_total=<n>
manifest=<base64 json when type=multi>
b64=
<wrapped base64 payload lines>
-----END SPECIALCOPY-----
```

- `sha256` for `single`/`multi` is over decompressed/original payload bytes.
- `sha256` for `chunk` is over that chunk's raw segment bytes.
- Base64 payload is wrapped to configured fixed line width (default 120 chars).

## Limits and recommendations

- Default chunk size: **1 MB** (configurable).
- Recommended max per part for fragile remote channels: **1–5 MB** text payload.
- For very large transfers, use chunking and verify all parts appear in assembly status before assembling.

## Troubleshooting

- **Clipboard does not contain Unicode text**: Copy full package text again.
- **".NET runtime not installed" when launching app**: run `scripts/install-context-menu.ps1` (or `scripts/publish-self-contained.ps1`), then register/use `dist\win-x64\SpecialPaste.exe` only.
- **Invalid package markers**: Ensure header/footer were not altered.
- **Hash mismatch**: Package tampered/corrupted in transit. Re-copy source package.
- **Size mismatch**: Incomplete payload or wrong chunk assembly.
- **Path traversal blocked**: Manifest contains unsafe relative path; package refused intentionally.
- **Part assembly incomplete**: Open Assembly Status and confirm received count equals total.

## Data directories

- Root: `%LOCALAPPDATA%\SpecialCopyPaste`
- Logs: `%LOCALAPPDATA%\SpecialCopyPaste\logs`
- Saved package text backups: `%LOCALAPPDATA%\SpecialCopyPaste\Packages`
- Chunk cache: `%LOCALAPPDATA%\SpecialCopyPaste\PartsCache`

## Security notes

- Files are **decoded and written only**; never opened/executed.
- Destination writes are restricted to selected destination folder subtree.
- Manifest paths are validated to reject traversal.
- AES-GCM passphrase encryption fields are not implemented yet (reserved for next iteration).
