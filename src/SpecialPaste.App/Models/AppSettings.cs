namespace SpecialPaste.Models;

public sealed class AppSettings
{
    public int Base64LineWidth { get; set; } = 120;
    public int ChunkSizeBytes { get; set; } = 1024 * 1024;
    public bool EnableCompression { get; set; } = true;
    public OverwriteBehavior OverwriteBehavior { get; set; } = OverwriteBehavior.RenameWithSuffix;

    public static AppSettings Load(string root)
    {
        var filePath = Path.Combine(root, "settings.json");
        if (!File.Exists(filePath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(filePath);
        return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(string root)
    {
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "settings.json");
        var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }
}

public enum OverwriteBehavior
{
    Prompt = 0,
    RenameWithSuffix = 1,
    Overwrite = 2
}
