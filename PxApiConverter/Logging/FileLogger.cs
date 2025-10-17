using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PxApiConverter.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private FileLoggerOptions _currentOptions;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private StreamWriter? _writer;
    private long _currentSize;

    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
    {
        _currentOptions = options.CurrentValue;
        _onChangeToken = options.OnChange(updated =>
        {
            _currentOptions = updated;
            ReopenWriter();
        });
        ReopenWriter();
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new FileLogger(this, name));

    internal bool IsEnabled(LogLevel level)
    {
        if (!Enum.TryParse<LogLevel>(_currentOptions.MinLevel ?? string.Empty, true, out var min))
            return true; // follow global filtering already applied earlier
        return level >= min;
    }

    internal void Write(string line)
    {
        lock (_sync)
        {
            if (_writer == null) return;
            _writer.WriteLine(line);
            _writer.Flush();
            _currentSize += System.Text.Encoding.UTF8.GetByteCount(line + Environment.NewLine);
            if (_currentOptions.MaxFileSizeBytes > 0 && _currentSize > _currentOptions.MaxFileSizeBytes)
            {
                Rotate();
            }
        }
    }

    private void Rotate()
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
            var path = GetAbsolutePath(_currentOptions.Path);
            if (File.Exists(path))
            {
                var dir = Path.GetDirectoryName(path)!;
                var file = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);
                var archive = Path.Combine(dir, $"{file}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}");
                File.Move(path, archive, overwrite: true);
            }
        }
        catch { /* swallow rotation errors */ }
        finally
        {
            ReopenWriter();
        }
    }

    private void ReopenWriter()
    {
        lock (_sync)
        {
            _writer?.Dispose();
            var path = GetAbsolutePath(_currentOptions.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var fileExists = File.Exists(path);
            _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            _currentSize = fileExists ? new FileInfo(path).Length : 0;
        }
    }

    private string GetAbsolutePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath)) return configuredPath;
        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _onChangeToken?.Dispose();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _category;
        public FileLogger(FileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null) return;
            var localNow = DateTimeOffset.Now; // local time as requested
            var line = $"{localNow:O}\t{logLevel}\t{_category}\t{message}";
            if (exception != null)
            {
                line += "\n" + exception.ToString();
            }
            _provider.Write(line);
        }
    }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, Action<FileLoggerOptions>? configure = null)
    {
        builder.Services.AddOptions<FileLoggerOptions>();
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        return builder;
    }
}
