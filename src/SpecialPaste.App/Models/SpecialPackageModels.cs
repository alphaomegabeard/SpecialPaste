namespace SpecialPaste.Models;

public sealed class SpecialPackage
{
    public required string PackageType { get; init; }
    public required string PackageId { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Compression { get; init; }
    public required string Sha256 { get; init; }
    public required long OriginalSize { get; init; }
    public required long StoredSize { get; init; }
    public string? Name { get; init; }
    public int PartIndex { get; init; } = 1;
    public int PartTotal { get; init; } = 1;
    public required string PayloadBase64 { get; init; }
    public string? ManifestJson { get; init; }
}

public sealed class ManifestFileEntry
{
    public required string RelativePath { get; init; }
    public required long Size { get; init; }
    public required string Sha256 { get; init; }
}

public sealed class MultiManifest
{
    public required List<ManifestFileEntry> Files { get; init; }
}

public sealed class PasteResult
{
    public required List<string> CreatedPaths { get; init; }
    public required string Message { get; init; }
}
