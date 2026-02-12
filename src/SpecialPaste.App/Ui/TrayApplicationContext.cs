using SpecialPaste.Core;
using SpecialPaste.Infrastructure;
using SpecialPaste.Models;

namespace SpecialPaste.Ui;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ClipboardService _clipboard;
    private readonly PackageService _packageService;
    private readonly PartsAssemblyService _partsAssemblyService;
    private readonly AppPaths _paths;
    private readonly FileLogger _logger;

    public TrayApplicationContext(
        ClipboardService clipboard,
        PackageService packageService,
        PartsAssemblyService partsAssemblyService,
        AppPaths paths,
        FileLogger logger)
    {
        _clipboard = clipboard;
        _packageService = packageService;
        _partsAssemblyService = partsAssemblyService;
        _paths = paths;
        _logger = logger;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Special Paste"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Copy file as Base64...", null, (_, _) => CopyFromPicker());
        menu.Items.Add("Paste Base64 to...", null, (_, _) => PasteToPicker());
        menu.Items.Add("Assembly status...", null, (_, _) => ShowAssemblyStatus());
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
        menu.Items.Add("Help", null, (_, _) => ShowHelp());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowHelp();
    }

    private void CopyFromPicker()
    {
        var settings = AppSettings.Load(_paths.Root);
        using var dialog = new OpenFileDialog
        {
            Title = "Select file to Special Copy",
            Multiselect = true
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        var package = dialog.FileNames.Length == 1
            ? _packageService.CreateSingleFilePackage(dialog.FileName, settings)
            : _packageService.CreateMultiPackage(dialog.FileNames, settings);

        var parts = _packageService.SplitPackageText(package, settings.ChunkSizeBytes, settings).ToList();
        _clipboard.SetText(parts[0]);

        Directory.CreateDirectory(_paths.Packages);
        for (int i = 0; i < parts.Count; i++)
        {
            var partName = $"package-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}-part-{i + 1:D4}.txt";
            File.WriteAllText(Path.Combine(_paths.Packages, partName), parts[i]);
        }

        _notifyIcon.ShowBalloonTip(2000, "Special Paste", $"Package ready. Copied part 1/{parts.Count} to clipboard.", ToolTipIcon.Info);
    }

    private void PasteToPicker()
    {
        var settings = AppSettings.Load(_paths.Root);
        using var picker = new FolderBrowserDialog
        {
            Description = "Select destination folder"
        };

        if (picker.ShowDialog() != DialogResult.OK) return;

        try
        {
            var text = _clipboard.GetText();
            var parsed = _packageService.ParsePackage(text);
            if (parsed.PackageType == "chunk")
            {
                _partsAssemblyService.StorePart(parsed);
                var status = _partsAssemblyService.GetStatus(parsed.PackageId);
                _notifyIcon.ShowBalloonTip(2000, "Special Paste", $"Stored part {parsed.PartIndex}/{parsed.PartTotal} ({status.received}/{status.total} received)", ToolTipIcon.Info);
                return;
            }

            var result = _packageService.MaterializePackage(parsed, picker.SelectedPath, settings);
            _notifyIcon.ShowBalloonTip(2000, "Special Paste", result.Message, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
            MessageBox.Show(ex.Message, "Special Paste", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAssemblyStatus()
    {
        using var form = new AssemblyStatusForm(_partsAssemblyService);
        form.ShowDialog();
    }

    private void OpenSettings()
    {
        var settings = AppSettings.Load(_paths.Root);
        using var form = new SettingsForm(settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            form.UpdatedSettings.Save(_paths.Root);
            _notifyIcon.ShowBalloonTip(1500, "Special Paste", "Settings saved.", ToolTipIcon.Info);
        }
    }

    private static void ShowHelp()
    {
        var help = "Special Paste usage:\n\n" +
                   "1) Use Special Copy on source machine.\n" +
                   "2) Paste generated text through your remote text channel.\n" +
                   "3) On destination machine use Special Paste (from Clipboard).\n" +
                   "4) For chunked packages, keep pasting all parts then use Assemble command.";
        MessageBox.Show(help, "Special Paste Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
