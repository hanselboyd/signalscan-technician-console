using System.IO;
using PdfSharp.Fonts;

namespace SignalScan.TechnicianConsole.Services;

public sealed class WindowsPdfSharpFontResolver : IFontResolver
{
    public const string FontFamilyName = "SignalScanSans";

    private const string RegularFaceName = "SignalScan.Windows.Regular";
    private const string BoldFaceName = "SignalScan.Windows.Bold";

    private static readonly object SyncRoot = new();
    private static WindowsPdfSharpFontResolver? _configuredResolver;

    private readonly string _regularFontPath;
    private readonly string _boldFontPath;

    private WindowsPdfSharpFontResolver(string regularFontPath, string boldFontPath)
    {
        _regularFontPath = regularFontPath;
        _boldFontPath = boldFontPath;
        SelectedRegularFontPath = regularFontPath;
        SelectedBoldFontPath = boldFontPath;
    }

    public static string? SelectedRegularFontPath { get; private set; }

    public static string? SelectedBoldFontPath { get; private set; }

    public static bool IsInitialized => _configuredResolver is not null;

    public static bool IsPdfSharpResolverInitialized => GlobalFontSettings.FontResolver is not null;

    public static void EnsureConfigured()
    {
        lock (SyncRoot)
        {
            if (_configuredResolver is not null)
            {
                return;
            }

            if (GlobalFontSettings.FontResolver is not null)
            {
                if (GlobalFontSettings.FontResolver is WindowsPdfSharpFontResolver existingResolver)
                {
                    _configuredResolver = existingResolver;
                    return;
                }

                throw new InvalidOperationException("PDFsharp font resolver was already initialized before SignalScan could register its Windows font resolver.");
            }

            var fontsDirectory = GetWindowsFontsDirectory();
            var regularFontPath = FindFirstReadableFont(fontsDirectory, "arial.ttf", "segoeui.ttf");
            var boldFontPath = FindFirstReadableFont(fontsDirectory, "arialbd.ttf", "segoeuib.ttf");
            SelectedRegularFontPath = regularFontPath;
            SelectedBoldFontPath = boldFontPath;

            if (regularFontPath is null || boldFontPath is null)
            {
                throw new InvalidOperationException(
                    $"PDF export requires installed Windows fonts. Expected regular font arial.ttf or segoeui.ttf and bold font arialbd.ttf or segoeuib.ttf in {fontsDirectory}.");
            }

            var resolver = new WindowsPdfSharpFontResolver(regularFontPath, boldFontPath);
            GlobalFontSettings.FontResolver = resolver;
            _configuredResolver = resolver;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        if (!IsSupportedFamily(familyName))
        {
            return null;
        }

        return new FontResolverInfo(bold ? BoldFaceName : RegularFaceName);
    }

    public byte[] GetFont(string faceName)
    {
        return faceName switch
        {
            RegularFaceName => File.ReadAllBytes(_regularFontPath),
            BoldFaceName => File.ReadAllBytes(_boldFontPath),
            _ => throw new InvalidOperationException($"PDF font face could not be resolved: {faceName}.")
        };
    }

    private static string GetWindowsFontsDirectory()
    {
        var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");
        if (string.IsNullOrWhiteSpace(windowsDirectory))
        {
            windowsDirectory = @"C:\Windows";
        }

        return Path.Combine(windowsDirectory, "Fonts");
    }

    private static string? FindFirstReadableFont(string fontsDirectory, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(fontsDirectory, fileName);
            if (CanReadFile(path))
            {
                return path;
            }
        }

        return null;
    }

    private static bool CanReadFile(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsSupportedFamily(string familyName) =>
        string.Equals(familyName, FontFamilyName, StringComparison.OrdinalIgnoreCase);
}
