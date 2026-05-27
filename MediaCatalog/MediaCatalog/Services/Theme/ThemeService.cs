using System.Windows;
using System.Windows.Media;
using VidBrary.Models;
using VidBrary.Services.Settings;

namespace VidBrary.Services.Theme;

public static class ThemeService
{
    // Resource keys — XAML binds to these via {DynamicResource ...}
    public const string Bg        = "AppBrushBg";
    public const string Surface   = "AppBrushSurface";
    public const string Accent    = "AppBrushAccent";
    public const string Secondary = "AppBrushSecondary";
    public const string Text      = "AppBrushText";
    public const string TextMuted = "AppBrushTextMuted";

    // ── Preset palettes ───────────────────────────────────────────────────────

    private static readonly (string Bg, string Surface, string Accent, string Secondary,
                              string Text, string TextMuted) DarkPalette
        = ("#1a1a2e", "#16213e", "#e94560", "#0f3460", "#ffffff", "#adb5bd");

    private static readonly (string Bg, string Surface, string Accent, string Secondary,
                              string Text, string TextMuted) LightPalette
        = ("#f5f7fa", "#ffffff", "#e94560", "#1a73e8", "#1a1a2e", "#495057");

    // ── Public entry-point ────────────────────────────────────────────────────

    public static void Apply(AppSettings settings)
    {
        var palette = settings.Theme switch
        {
            AppTheme.Light         => LightPalette,
            AppTheme.FollowWindows => IsSystemDark() ? DarkPalette : LightPalette,
            _                      => DarkPalette
        };

        var bg        = Override(palette.Bg,        settings.BackgroundColor);
        var surface   = Override(palette.Surface,   settings.SurfaceColor);
        var accent    = Override(palette.Accent,    settings.AccentColor);
        var secondary = Override(palette.Secondary, settings.SecondaryColor);

        var res = Application.Current.Resources;
        SetBrush(res, Bg,        bg);
        SetBrush(res, Surface,   surface);
        SetBrush(res, Accent,    accent);
        SetBrush(res, Secondary, secondary);
        SetBrush(res, Text,      palette.Text);
        SetBrush(res, TextMuted, palette.TextMuted);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Override(string preset, string? custom) =>
        string.IsNullOrWhiteSpace(custom) || custom == preset ? preset : custom;

    private static void SetBrush(ResourceDictionary res, string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
            // Always replace — XAML-declared brushes are frozen and cannot be mutated.
        // DynamicResource updates correctly when the key is replaced in the dictionary.
        res[key] = new SolidColorBrush(color);
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return true; }
    }
}