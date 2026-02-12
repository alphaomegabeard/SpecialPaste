using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpecialPaste.Infrastructure;
using SpecialPaste.Models;

namespace SpecialPaste.Core;

public sealed class PackageService
{
    public const string Header = "-----BEGIN SPECIALCOPY-----";
    public const string Footer = "-----END SPECIALCOPY-----";

    private readonly FileLogger _logger;

    public PackageService(FileLogger logger)
    {
        _logger = logger;
    }

    public string CreateSingleFilePackage(string filePath, AppSettings settings, string? packageId = null, int partIndex = 1, int partTotal = 1)
    {
        var info = new FileInfo(filePath);
        var raw = File.ReadAllBytes(filePath);
        var compression = "none";
        var payload = raw;

        if (settings.EnableCompression)
        {
            var gz = Compress(raw);
            if (gz.Length < raw.Length)
            {
                payload = gz;
                compression = "gzip";
            }
        }

        var package = new SpecialPackage
        {
            PackageType = "single",
            PackageId = packageId ?? Guid.NewGuid().ToString("D"),
            TimestampUtc = DateTimeOffset.UtcNow,
            Compression = compression,
            Sha256 = Sha256Hex(raw),
            OriginalSize = raw.LongLength,
            StoredSize = payload.LongLength,
            Name = info.Name,
            PartIndex = partIndex,
            PartTotal = partTotal,
            PayloadBase64 = Convert.ToBase64String(payload)
        };

        return SerializePackage(package, settings.Base64LineWidth);
    }

    public string CreateMultiPackage(IEnumerable<string> selectedPaths, AppSettings settings)
    {
        var fileList = ExpandToFiles(selectedPaths).ToList();
        var entries = new List<ManifestFileEntry>();
        using var payloadStream = new MemoryStream();

        foreach (var fullPath in fileList)
        {
            var relative = NormalizeRelativePath(fullPath, selectedPaths);
            var bytes = File.ReadAllBytes(fullPath);
            entries.Add(new ManifestFileEntry
            {
                RelativePath = relative,
                Size = bytes.LongLength,
                Sha256 = Sha256Hex(bytes)
            });
            payloadStream.Write(BitConverter.GetBytes(bytes.LongLength));
            payloadStream.Write(bytes);
        }

        var rawPayload = payloadStream.ToArray();
        var compression = "none";
        var stored = rawPayload;

        if (settings.EnableCompression)
        {
            var compressed = Compress(rawPayload);
            if (compressed.Length < rawPayload.Length)
            {
                compression = "gzip";
                stored = compressed;
            }
        }

        var manifest = new MultiManifest { Files = entries };

        var package = new SpecialPackage
        {
            PackageType = "multi",
            PackageId = Guid.NewGuid().ToString("D"),
            TimestampUtc = DateTimeOffset.UtcNow,
            Compression = compression,
            Sha256 = Sha256Hex(rawPayload),
            OriginalSize = rawPayload.LongLength,
            StoredSize = stored.LongLength,
            Name = "bundle",
            PayloadBase64 = Convert.ToBase64String(stored),
            ManifestJson = JsonSerializer.Serialize(manifest)
        };

        return SerializePackage(package, settings.Base64LineWidth);
    }

    public IEnumerable<string> SplitPackageText(string fullPackageText, int maxChunkBytes, AppSettings settings)
    {
        var bytes = Encoding.UTF8.GetBytes(fullPackageText);
        if (bytes.Length <= maxChunkBytes)
        {
            return new[] { fullPackageText };
        }

        var packageId = Guid.NewGuid().ToString("D");
        var partTexts = new List<string>();
        var chunkCount = (int)Math.Ceiling((double)bytes.Length / maxChunkBytes);

        for (var i = 0; i < chunkCount; i++)
        {
            var offset = i * maxChunkBytes;
            var length = Math.Min(maxChunkBytes, bytes.Length - offset);
            var segment = new byte[length];
            Array.Copy(bytes, offset, segment, 0, length);

            var package = new SpecialPackage
            {
                PackageType = "chunk",
                PackageId = packageId,
                TimestampUtc = DateTimeOffset.UtcNow,
                Compression = "none",
                Sha256 = Sha256Hex(segment),
                OriginalSize = length,
                StoredSize = length,
                Name = "chunk",
                PartIndex = i + 1,
                PartTotal = chunkCount,
                PayloadBase64 = Convert.ToBase64String(segment)
            };

            partTexts.Add(SerializePackage(package, settings.Base64LineWidth));
        }

        return partTexts;
    }

    public SpecialPackage ParsePackage(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var start = Array.IndexOf(lines, Header);
        var end = Array.IndexOf(lines, Footer);

        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Clipboard text does not contain a valid SpecialCopy package.");
        }

        var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var b64Builder = new StringBuilder();
        var inB64 = false;

        for (int i = start + 1; i < end; i++)
        {
            var line = lines[i].TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (inB64)
            {
                b64Builder.Append(line.Trim());
                continue;
            }

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (key.Equals("b64", StringComparison.OrdinalIgnoreCase))
            {
                inB64 = true;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    b64Builder.Append(value);
                }
                continue;
            }

            kv[key] = value;
        }

        return new SpecialPackage
        {
            PackageType = GetRequired(kv, "type"),
            PackageId = GetRequired(kv, "package_id"),
            TimestampUtc = DateTimeOffset.Parse(GetRequired(kv, "timestamp_utc")),
            Compression = GetRequired(kv, "compress"),
            Sha256 = GetRequired(kv, "sha256"),
            OriginalSize = long.Parse(GetRequired(kv, "size")),
            StoredSize = long.Parse(GetRequired(kv, "stored_size")),
            Name = kv.GetValueOrDefault("name"),
            PartIndex = int.Parse(kv.GetValueOrDefault("part_index") ?? "1"),
            PartTotal = int.Parse(kv.GetValueOrDefault("part_total") ?? "1"),
            ManifestJson = kv.TryGetValue("manifest", out var manifestB64) ? Encoding.UTF8.GetString(Convert.FromBase64String(manifestB64)) : null,
            PayloadBase64 = b64Builder.ToString()
        };
    }

    public PasteResult MaterializePackage(SpecialPackage package, string destinationFolder, AppSettings settings)
    {
        Directory.CreateDirectory(destinationFolder);
        var payload = Convert.FromBase64String(package.PayloadBase64);
        if (!string.Equals(Sha256Hex(payload), package.Sha256, StringComparison.OrdinalIgnoreCase) && package.PackageType == "chunk")
        {
            throw new InvalidOperationException("Chunk sha256 mismatch.");
        }

        byte[] expanded = package.Compression == "gzip" ? Decompress(payload) : payload;

        if (!string.Equals(Sha256Hex(expanded), package.Sha256, StringComparison.OrdinalIgnoreCase) && package.PackageType != "chunk")
        {
            throw new InvalidOperationException("Payload sha256 mismatch.");
        }

        if (expanded.LongLength != package.OriginalSize)
        {
            throw new InvalidOperationException($"Size mismatch. Expected {package.OriginalSize}, got {expanded.LongLength}.");
        }

        if (package.PackageType == "single")
        {
            var name = package.Name ?? "output.bin";
            var path = ResolveDestinationPath(destinationFolder, name, settings.OverwriteBehavior);
            File.WriteAllBytes(path, expanded);
            return new PasteResult
            {
                CreatedPaths = new List<string> { path },
                Message = $"Created file: {path}"
            };
        }

        if (package.PackageType == "multi")
        {
            if (string.IsNullOrWhiteSpace(package.ManifestJson))
            {
                throw new InvalidOperationException("Missing manifest for multi package.");
            }

            var manifest = JsonSerializer.Deserialize<MultiManifest>(package.ManifestJson)
                ?? throw new InvalidOperationException("Invalid manifest JSON.");

            var created = new List<string>();
            using var stream = new MemoryStream(expanded);
            foreach (var file in manifest.Files)
            {
                ValidateRelativePath(file.RelativePath);
                var lengthBytes = new byte[sizeof(long)];
                stream.ReadExactly(lengthBytes);
                var length = BitConverter.ToInt64(lengthBytes);
                var fileBytes = new byte[length];
                stream.ReadExactly(fileBytes);

                if (length != file.Size)
                {
                    throw new InvalidOperationException($"Manifest size mismatch for {file.RelativePath}.");
                }

                if (!string.Equals(Sha256Hex(fileBytes), file.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Manifest hash mismatch for {file.RelativePath}.");
                }

                var abs = Path.GetFullPath(Path.Combine(destinationFolder, file.RelativePath));
                if (!abs.StartsWith(Path.GetFullPath(destinationFolder), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Refused to write outside destination folder.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
                var resolved = ResolveDestinationPath(Path.GetDirectoryName(abs)!, Path.GetFileName(abs), settings.OverwriteBehavior);
                File.WriteAllBytes(resolved, fileBytes);
                created.Add(resolved);
            }

            return new PasteResult
            {
                CreatedPaths = created,
                Message = $"Created {created.Count} file(s)."
            };
        }

        throw new InvalidOperationException($"Unsupported package type for direct paste: {package.PackageType}");
    }

    public string SerializePackage(SpecialPackage package, int lineWidth)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine($"type={package.PackageType}");
        sb.AppendLine($"package_id={package.PackageId}");
        sb.AppendLine($"timestamp_utc={package.TimestampUtc:O}");
        sb.AppendLine($"name={package.Name}");
        sb.AppendLine($"size={package.OriginalSize}");
        sb.AppendLine($"stored_size={package.StoredSize}");
        sb.AppendLine($"sha256={package.Sha256}");
        sb.AppendLine($"compress={package.Compression}");
        sb.AppendLine($"part_index={package.PartIndex}");
        sb.AppendLine($"part_total={package.PartTotal}");
        if (!string.IsNullOrWhiteSpace(package.ManifestJson))
        {
            sb.AppendLine($"manifest={Convert.ToBase64String(Encoding.UTF8.GetBytes(package.ManifestJson))}");
        }

        sb.AppendLine("b64=");
        foreach (var line in Wrap(package.PayloadBase64, lineWidth))
        {
            sb.AppendLine(line);
        }
        sb.AppendLine(Footer);
        return sb.ToString();
    }

    private static string ResolveDestinationPath(string folder, string fileName, OverwriteBehavior behavior)
    {
        var full = Path.Combine(folder, fileName);
        if (!File.Exists(full) || behavior == OverwriteBehavior.Overwrite)
        {
            return full;
        }

        if (behavior == OverwriteBehavior.Prompt)
        {
            var result = MessageBox.Show(
                $"File exists:\n{full}\nOverwrite?",
                "Special Paste",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            return result == DialogResult.Yes ? full : NextAvailable(full);
        }

        return NextAvailable(full);
    }

    private static string NextAvailable(string fullPath)
    {
        var dir = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var ext = Path.GetExtension(fullPath);
        var counter = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({counter}){ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static string GetRequired(IDictionary<string, string> kv, string key)
        => kv.TryGetValue(key, out var value) ? value : throw new InvalidOperationException($"Missing required field: {key}");

    private static IEnumerable<string> Wrap(string value, int width)
    {
        for (var i = 0; i < value.Length; i += width)
        {
            var length = Math.Min(width, value.Length - i);
            yield return value.Substring(i, length);
        }
    }

    private static string Sha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static IEnumerable<string> ExpandToFiles(IEnumerable<string> selectedPaths)
    {
        foreach (var path in selectedPaths)
        {
            if (File.Exists(path))
            {
                yield return Path.GetFullPath(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    yield return Path.GetFullPath(file);
                }
            }
        }
    }

    private static string NormalizeRelativePath(string fullPath, IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            var absRoot = Path.GetFullPath(root);
            if (File.Exists(root))
            {
                if (string.Equals(Path.GetFullPath(root), fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(fullPath);
                }
                continue;
            }

            var rootWithSep = absRoot.EndsWith(Path.DirectorySeparatorChar) ? absRoot : absRoot + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(absRoot, fullPath).Replace('\\', '/');
            }
        }

        return Path.GetFileName(fullPath);
    }

    private static void ValidateRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        if (Path.IsPathRooted(normalized) || normalized.StartsWith("../") || normalized.Contains("/../") || normalized == "..")
        {
            throw new InvalidOperationException($"Unsafe relative path in manifest: {relativePath}");
        }
    }
}
