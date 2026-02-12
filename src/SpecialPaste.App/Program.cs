using System.Text;
using SpecialPaste.Core;
using SpecialPaste.Infrastructure;
using SpecialPaste.Ui;

namespace SpecialPaste;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var paths = new AppPaths();
        Directory.CreateDirectory(paths.Root);
        Directory.CreateDirectory(paths.Logs);
        Directory.CreateDirectory(paths.Packages);
        Directory.CreateDirectory(paths.PartsCache);

        using var logger = new FileLogger(paths.Logs);
        var clipboard = new ClipboardService(logger);
        var packager = new PackageService(logger);
        var assemblyService = new PartsAssemblyService(paths.PartsCache, logger, packager);

        try
        {
            if (args.Length > 0)
            {
                return CommandHandler.Run(args, clipboard, packager, assemblyService, paths, logger);
            }

            Application.Run(new TrayApplicationContext(clipboard, packager, assemblyService, paths, logger));
            return 0;
        }
        catch (Exception ex)
        {
            logger.Error($"Fatal error: {ex}");
            MessageBox.Show(
                ex.Message,
                "Special Paste - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }
}
