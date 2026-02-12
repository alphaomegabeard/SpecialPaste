using System.Text;
using SpecialPaste.Infrastructure;
using SpecialPaste.Models;

namespace SpecialPaste.Core;

public sealed class PartsAssemblyService
{
    private readonly string _cacheRoot;
    private readonly FileLogger _logger;
    private readonly PackageService _packageService;

    public PartsAssemblyService(string cacheRoot, FileLogger logger, PackageService packageService)
    {
        _cacheRoot = cacheRoot;
        _logger = logger;
        _packageService = packageService;
        Directory.CreateDirectory(_cacheRoot);
    }

    public void StorePart(SpecialPackage partPackage)
    {
        if (partPackage.PackageType != "chunk")
        {
            throw new InvalidOperationException("Only chunk packages can be stored as parts.");
        }

        var dir = Path.Combine(_cacheRoot, partPackage.PackageId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"part-{partPackage.PartIndex:D4}.txt"), _packageService.SerializePackage(partPackage, 120), Encoding.UTF8);
        File.WriteAllText(Path.Combine(dir, "meta.txt"), partPackage.PartTotal.ToString());
        _logger.Info($"Stored chunk part {partPackage.PartIndex}/{partPackage.PartTotal} for {partPackage.PackageId}");
    }

    public (int received, int total) GetStatus(string packageId)
    {
        var dir = Path.Combine(_cacheRoot, packageId);
        if (!Directory.Exists(dir)) return (0, 0);

        var totalPath = Path.Combine(dir, "meta.txt");
        var total = File.Exists(totalPath) ? int.Parse(File.ReadAllText(totalPath).Trim()) : 0;
        var received = Directory.EnumerateFiles(dir, "part-*.txt").Count();
        return (received, total);
    }

    public bool TryAssemble(string packageId, out string assembledText)
    {
        var dir = Path.Combine(_cacheRoot, packageId);
        assembledText = string.Empty;
        if (!Directory.Exists(dir)) return false;

        var (received, total) = GetStatus(packageId);
        if (total <= 0 || received < total)
        {
            return false;
        }

        var bytes = new List<byte>();
        for (var i = 1; i <= total; i++)
        {
            var path = Path.Combine(dir, $"part-{i:D4}.txt");
            if (!File.Exists(path)) return false;
            var part = _packageService.ParsePackage(File.ReadAllText(path, Encoding.UTF8));
            var partPayload = Convert.FromBase64String(part.PayloadBase64);
            bytes.AddRange(partPayload);
        }

        assembledText = Encoding.UTF8.GetString(bytes.ToArray());
        _logger.Info($"Assembled all parts for package {packageId}. Bytes={bytes.Count}");
        return true;
    }

    public IReadOnlyList<(string packageId, int received, int total)> ListStatus()
    {
        var result = new List<(string packageId, int received, int total)>();
        foreach (var dir in Directory.EnumerateDirectories(_cacheRoot))
        {
            var id = Path.GetFileName(dir);
            var (received, total) = GetStatus(id);
            result.Add((id, received, total));
        }

        return result.OrderBy(r => r.packageId).ToList();
    }

    public void ClearCache(string? packageId = null)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            foreach (var dir in Directory.EnumerateDirectories(_cacheRoot))
            {
                Directory.Delete(dir, recursive: true);
            }
            return;
        }

        var target = Path.Combine(_cacheRoot, packageId);
        if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }
}
