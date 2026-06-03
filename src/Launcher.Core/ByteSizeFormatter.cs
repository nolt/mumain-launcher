using System.Globalization;

namespace Launcher.Core;

/// <summary>Formats a byte count as a human-readable size such as "45.3 MB".</summary>
public static class ByteSizeFormatter
{
    private const double Unit = 1024.0;
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        double size = bytes;
        var unitIndex = 0;
        while (size >= Unit && unitIndex < Units.Length - 1)
        {
            size /= Unit;
            unitIndex++;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{size:0.##} {Units[unitIndex]}");
    }
}
