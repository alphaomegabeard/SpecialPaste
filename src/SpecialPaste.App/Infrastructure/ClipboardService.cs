namespace SpecialPaste.Infrastructure;

public sealed class ClipboardService
{
    private readonly FileLogger _logger;

    public ClipboardService(FileLogger logger)
    {
        _logger = logger;
    }

    public void SetText(string text)
    {
        Clipboard.SetText(text, TextDataFormat.UnicodeText);
        _logger.Info($"Copied package to clipboard. Length={text.Length}");
    }

    public string GetText()
    {
        if (!Clipboard.ContainsText(TextDataFormat.UnicodeText))
        {
            throw new InvalidOperationException("Clipboard does not contain Unicode text.");
        }

        var value = Clipboard.GetText(TextDataFormat.UnicodeText);
        _logger.Info($"Read clipboard text. Length={value.Length}");
        return value;
    }
}
