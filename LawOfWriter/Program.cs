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
builder.Logging.AddSeq(
    seqUrl: "https://logs.lichtii.de",
    apiKey: "stAubsOwvfqe7Cyag3C8",
    minimumLevel: LogLevel.Information
);

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

builder.Services.AddMudServices();

await builder.Build().RunAsync();