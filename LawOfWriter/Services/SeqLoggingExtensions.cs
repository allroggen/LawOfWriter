namespace LawOfWriter.Services;

/// <summary>
/// Extension methods for registering the Seq logger provider.
/// </summary>
public static class SeqLoggingExtensions
{
    /// <summary>
    /// Adds a Seq logging provider that sends log events via HTTP to a Seq server.
    /// Compatible with Blazor WebAssembly.
    /// </summary>
    public static ILoggingBuilder AddSeq(this ILoggingBuilder builder, string seqUrl, string apiKey,
        LogLevel minimumLevel = LogLevel.Information)
    {
        builder.AddProvider(new SeqLoggerProvider(seqUrl, apiKey, minimumLevel));
        return builder;
    }
}

