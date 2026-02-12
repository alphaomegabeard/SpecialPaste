namespace SpecialPaste.Infrastructure;

public sealed class FileLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _sync = new();

    public FileLogger(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);
        var logFile = Path.Combine(logsDirectory, $"specialpaste-{DateTime.UtcNow:yyyyMMdd}.log");
        _writer = new StreamWriter(new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            _writer.WriteLine($"{DateTime.UtcNow:O}\t{level}\t{message}");
        }
    }

    public void Dispose() => _writer.Dispose();
}
