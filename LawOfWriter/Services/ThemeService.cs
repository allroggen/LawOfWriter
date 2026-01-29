using Microsoft.JSInterop;

namespace LawOfWriter.Services;

public class ThemeService
{
    private const string DarkModeKey = "lawofwriter:darkmode";
    private readonly IJSRuntime _js;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<bool> GetDarkModeAsync(bool defaultValue = false)
    {
        try
        {
            var value = await _js.InvokeAsync<string?>("localStorage.getItem", DarkModeKey);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (bool.TryParse(value, out var parsed))
                return parsed;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetDarkModeAsync(bool isDark)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", DarkModeKey, isDark.ToString().ToLowerInvariant());
        }
        catch
        {
            // ignore storage failures (private mode / blocked storage)
        }
    }
}
