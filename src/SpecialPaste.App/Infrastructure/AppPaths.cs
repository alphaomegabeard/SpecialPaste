namespace SpecialPaste.Infrastructure;

public sealed class AppPaths
{
    public string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SpecialCopyPaste");

    public string Logs => Path.Combine(Root, "logs");
    public string Packages => Path.Combine(Root, "Packages");
    public string PartsCache => Path.Combine(Root, "PartsCache");
}
