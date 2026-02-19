using Microsoft.JSInterop;

namespace LawOfWriter.Services;

public class ThemeService(IJSRuntime js)
{
    private const string DarkModeKey = "lawofwriter:darkmode";

    public async Task<bool> GetDarkModeAsync(bool defaultValue = false)
    {
        try
        {
            var value = await js.InvokeAsync<string?>("localStorage.getItem", DarkModeKey);
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
            await js.InvokeVoidAsync("localStorage.setItem", DarkModeKey, isDark.ToString().ToLowerInvariant());
        }
        catch
        {
            // ignore storage failures (private mode / blocked storage)
        }
    }
}
