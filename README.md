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

## Build

```powershell
cd src/SpecialPaste.App
dotnet build -c Release
```

Output executable (expected):

`src\SpecialPaste.App\bin\Release\net8.0-windows\SpecialPaste.exe`

## Context menu setup (no admin, per-user)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-context-menu.ps1 -ExePath .\src\SpecialPaste.App\bin\Release\net8.0-windows\SpecialPaste.exe
```

### Uninstall context menu

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\unregister-context-menu.ps1
```

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
```

### C) Tray app

Launch `SpecialPaste.exe` with no arguments.

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
