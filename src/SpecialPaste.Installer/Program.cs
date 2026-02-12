using Microsoft.Win32;
using System.Diagnostics;

namespace SpecialPasteInstaller;

internal static class Program
{
    private const string IconValue = "imageres.dll,-5302";

    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var installerExe = Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("Unable to determine installer location.");
            var root = Directory.GetParent(installerExe)?.Parent?.Parent?.Parent?.FullName;
            var candidateFromDist = root is null
                ? null
                : Path.Combine(root, "dist", "win-x64", "SpecialPaste.exe");
            var candidateSibling = Path.Combine(Path.GetDirectoryName(installerExe)!, "SpecialPaste.exe");

            var specialPasteExe = ResolveSpecialPasteExe(args, candidateFromDist, candidateSibling);
            if (specialPasteExe is null)
            {
                MessageBox.Show(
                    "Could not locate SpecialPaste.exe.\n\nPlace the installer next to SpecialPaste.exe or pass the path as the first argument.",
                    "Special Paste Installer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            RegisterContextMenus(specialPasteExe);
            CreateShortcuts(specialPasteExe);

            MessageBox.Show(
                $"Special Paste installed successfully.\n\nEXE: {specialPasteExe}\n\nContext menu entries were added and shortcuts were created.",
                "Special Paste Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Special Paste Installer - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string? ResolveSpecialPasteExe(string[] args, string? candidateFromDist, string candidateSibling)
    {
        if (args.Length > 0 && File.Exists(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        if (candidateFromDist is not null && File.Exists(candidateFromDist))
        {
            return Path.GetFullPath(candidateFromDist);
        }

        if (File.Exists(candidateSibling))
        {
            return Path.GetFullPath(candidateSibling);
        }

        return null;
    }

    private static void RegisterContextMenus(string exe)
    {
        var copyCommand = $"\"{exe}\" special-copy \"%1\"";
        var pasteCommand = $"\"{exe}\" special-paste \"%V\"";
        var assembleCommand = $"\"{exe}\" show-assembly";

        SetVerb(@"Software\Classes\*\shell\SpecialCopyBase64", "Special Copy (Base64 Package)", copyCommand);
        SetVerb(@"Software\Classes\AllFilesystemObjects\shell\SpecialCopyBase64", "Special Copy (Base64 Package)", copyCommand);
        SetVerb(@"Software\Classes\Directory\shell\SpecialCopyBase64", "Special Copy (Base64 Package)", copyCommand);
        SetVerb(@"Software\Classes\lnkfile\shell\SpecialCopyBase64", "Special Copy (Base64 Package)", copyCommand);
        SetVerb(@"Software\Classes\InternetShortcut\shell\SpecialCopyBase64", "Special Copy (Base64 Package)", copyCommand);

        SetVerb(@"Software\Classes\Directory\Background\shell\SpecialPasteFromClipboard", "Special Paste (from Clipboard)", pasteCommand);
        SetVerb(@"Software\Classes\DesktopBackground\Shell\SpecialPasteFromClipboard", "Special Paste (from Clipboard)", pasteCommand);
        SetVerb(@"Software\Classes\Directory\Background\shell\SpecialPasteAssemble", "Special Paste (Assemble Parts...)", assembleCommand);
        SetVerb(@"Software\Classes\DesktopBackground\Shell\SpecialPasteAssemble", "Special Paste (Assemble Parts...)", assembleCommand);
    }

    private static void SetVerb(string keyPath, string menuText, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath, true)
            ?? throw new InvalidOperationException($"Unable to create key: HKCU\\{keyPath}");
        key.SetValue("MUIVerb", menuText, RegistryValueKind.String);
        key.SetValue("Icon", IconValue, RegistryValueKind.String);

        using var commandKey = key.CreateSubKey("command", true)
            ?? throw new InvalidOperationException($"Unable to create command key: HKCU\\{keyPath}\\command");
        commandKey.SetValue(string.Empty, command, RegistryValueKind.String);
    }

    private static void CreateShortcuts(string exe)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

        CreateShortcut(Path.Combine(desktop, "Special Paste.lnk"), exe);
        CreateShortcut(Path.Combine(programs, "Special Paste.lnk"), exe);
    }

    private static void CreateShortcut(string shortcutPath, string targetExe)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell COM is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Failed to create WScript.Shell instance.");

        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetExe;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
        shortcut.IconLocation = "imageres.dll,-5302";
        shortcut.Description = "Special Paste tray app";
        shortcut.Save();
    }
}
