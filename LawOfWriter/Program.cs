using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LawOfWriter;
using LawOfWriter.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Enable browser console logging + Seq central logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddFilter("Microsoft", LogLevel.Information);

var seqUrl = builder.Configuration["Seq:Url"];
var seqApiKey = builder.Configuration["Seq:ApiKey"];
var seqMinLevel = Enum.TryParse<LogLevel>(builder.Configuration["Seq:MinimumLevel"], out var lvl) ? lvl : LogLevel.Information;

if (!string.IsNullOrEmpty(seqUrl) && !string.IsNullOrEmpty(seqApiKey))
{
    builder.Logging.AddSeq(
        seqUrl: seqUrl,
        apiKey: seqApiKey,
        minimumLevel: seqMinLevel
    );
}

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Standard HttpClient für die App
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Authentication Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<LocalDbService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// API Services mit automatischer Bearer Token Injection
builder.Services.AddScoped<ApiAuthorizationHandler>(sp =>
{
    var authService = sp.GetRequiredService<AuthService>();
    var logger = sp.GetRequiredService<ILogger<ApiAuthorizationHandler>>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    return new ApiAuthorizationHandler(authService, logger, authStateProvider);
});

builder.Services.AddScoped<ApiService>(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationHandler>();
    var logger = sp.GetRequiredService<ILogger<ApiService>>();
    handler.InnerHandler = new HttpClientHandler();
    var httpClient = new HttpClient(handler);
    return new ApiService(httpClient, logger);
});

// GameDayAction Service
builder.Services.AddScoped<IGameDayActionService, GameDayActionService>();
builder.Services.AddScoped<ConnectivityService>();

builder.Services.AddMudServices();

await builder.Build().RunAsync();