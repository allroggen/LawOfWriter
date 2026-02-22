using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace LawOfWriter.Services;

/// <summary>
/// ILoggerProvider that sends log events to a Seq server via HTTP (WASM-compatible).
/// </summary>
public sealed class SeqLoggerProvider : ILoggerProvider
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, SeqLogger> _loggers = new();
    private readonly string _seqUrl;
    private readonly LogLevel _minimumLevel;

    public SeqLoggerProvider(string seqUrl, string apiKey, LogLevel minimumLevel = LogLevel.Information)
    {
        _seqUrl = seqUrl.TrimEnd('/');
        _minimumLevel = minimumLevel;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Seq-ApiKey", apiKey);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new SeqLogger(name, _httpClient, _seqUrl, _minimumLevel));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _loggers.Clear();
    }
}

/// <summary>
/// ILogger implementation that posts CLEF-formatted log events to a Seq ingestion endpoint.
/// </summary>
public sealed class SeqLogger : ILogger
{
    private readonly string _categoryName;
    private readonly HttpClient _httpClient;
    private readonly string _seqUrl;
    private readonly LogLevel _minimumLevel;

    public SeqLogger(string categoryName, HttpClient httpClient, string seqUrl, LogLevel minimumLevel)
    {
        _categoryName = categoryName;
        _httpClient = httpClient;
        _seqUrl = seqUrl;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var clefEvent = BuildClefEvent(logLevel, eventId, message, exception);

        // Fire-and-forget: logging darf den UI-Thread nicht blockieren
        _ = SendAsync(clefEvent);
    }

    /// <summary>
    /// Builds a single CLEF (Compact Log Event Format) line for Seq ingestion.
    /// </summary>
    private string BuildClefEvent(LogLevel logLevel, EventId eventId, string message, Exception? exception)
    {
        var properties = new Dictionary<string, object?>
        {
            ["@t"] = DateTimeOffset.UtcNow.ToString("O"),
            ["@l"] = MapLogLevel(logLevel),
            ["@mt"] = message,
            ["SourceContext"] = _categoryName,
            ["Application"] = "LawOfWriter",
            ["Platform"] = "Blazor WebAssembly"
        };

        if (eventId.Id != 0)
            properties["EventId"] = eventId.Id;

        if (!string.IsNullOrEmpty(eventId.Name))
            properties["EventName"] = eventId.Name;

        if (exception is not null)
            properties["@x"] = exception.ToString();

        return JsonSerializer.Serialize(properties);
    }

    private async Task SendAsync(string clefLine)
    {
        try
        {
            var content = new StringContent(clefLine, Encoding.UTF8, "application/vnd.serilog.clef");
            await _httpClient.PostAsync($"{_seqUrl}/api/events/raw?clef", content);
        }
        catch
        {
            // Logging-Fehler dürfen die App niemals crashen
        }
    }

    private static string MapLogLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "Verbose",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Critical => "Fatal",
        _ => "Information"
    };
}

