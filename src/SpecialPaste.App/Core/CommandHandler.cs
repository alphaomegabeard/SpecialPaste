using SpecialPaste.Infrastructure;
using SpecialPaste.Models;

namespace SpecialPaste.Core;

public static class CommandHandler
{
    public static int Run(
        string[] args,
        ClipboardService clipboard,
        PackageService packageService,
        PartsAssemblyService partsAssembly,
        AppPaths paths,
        FileLogger logger)
    {
        var settings = AppSettings.Load(paths.Root);
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "special-copy":
                {
                    var targets = args.Skip(1).ToArray();
                    if (targets.Length == 0) throw new ArgumentException("special-copy requires at least one file/folder path.");

                    var packageText = targets.Length == 1 && File.Exists(targets[0])
                        ? packageService.CreateSingleFilePackage(targets[0], settings)
                        : packageService.CreateMultiPackage(targets, settings);

                    var chunks = packageService.SplitPackageText(packageText, settings.ChunkSizeBytes, settings).ToList();
                    if (chunks.Count == 1)
                    {
                        clipboard.SetText(chunks[0]);
                        PersistPackage(paths.Packages, chunks[0]);
                    }
                    else
                    {
                        clipboard.SetText(chunks[0]);
                        for (int i = 0; i < chunks.Count; i++)
                        {
                            PersistPackage(paths.Packages, chunks[i], $"part-{i + 1:D4}");
                        }
                        MessageBox.Show($"Package split into {chunks.Count} parts. Part 1 copied to clipboard.", "Special Paste", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return 0;
                }

            case "special-paste":
                {
                    if (args.Length < 2) throw new ArgumentException("special-paste requires destination folder path.");
                    var destination = args[1];
                    var text = clipboard.GetText();
                    var package = packageService.ParsePackage(text);


                    if (package.PackageType == "chunk")
                    {
                        partsAssembly.StorePart(package);
                        var status = partsAssembly.GetStatus(package.PackageId);
                        MessageBox.Show($"Stored part {package.PartIndex}/{package.PartTotal}. Received {status.received}/{status.total}.", "Special Paste", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 0;
                    }

                    var result = packageService.MaterializePackage(package, destination, settings);
                    logger.Info(result.Message);
                    NotifySuccess(result.Message);
                    return 0;
                }

            case "special-assemble":
                {
                    if (args.Length < 3) throw new ArgumentException("special-assemble requires package_id and destination folder.");
                    var packageId = args[1];
                    var destination = args[2];
                    if (!partsAssembly.TryAssemble(packageId, out var assembledText))
                    {
                        throw new InvalidOperationException("Not all parts are available yet.");
                    }

                    var package = packageService.ParsePackage(assembledText);
                    var result = packageService.MaterializePackage(package, destination, settings);
                    NotifySuccess($"Assembled and pasted successfully. {result.Message}");
                    return 0;
                }

            case "show-assembly":
                {
                    var statuses = partsAssembly.ListStatus();
                    var body = statuses.Count == 0
                        ? "No partial packages found."
                        : string.Join(Environment.NewLine, statuses.Select(s => $"{s.packageId}: {s.received}/{s.total}"));
                    MessageBox.Show(body, "Special Paste - Assembly Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

            default:
                throw new ArgumentException($"Unknown command: {command}");
        }
    }

    private static void PersistPackage(string packagesPath, string content, string? suffix = null)
    {
        Directory.CreateDirectory(packagesPath);
        var fileName = $"package-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            fileName += $"-{suffix}";
        }

        File.WriteAllText(Path.Combine(packagesPath, fileName + ".txt"), content);
    }

    private static void NotifySuccess(string message)
    {
        MessageBox.Show(message, "Special Paste", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
