namespace Launcher.Core;

/// <summary>
/// Builds the download URL of a single file from the manifest URL and the
/// manifest's base URL. Kept as a pure function so it is easy to test.
/// </summary>
public static class PatchUris
{
    /// <summary>
    /// Resolves the absolute URL of <paramref name="filePath"/>, relative to the
    /// manifest's location and the manifest's <c>baseUrl</c>. Each path segment is
    /// URL-escaped so names with spaces or special characters work.
    /// </summary>
    public static Uri ResolveFileUri(Uri manifestUrl, string baseUrl, string filePath)
    {
        var baseUri = new Uri(manifestUrl, EnsureTrailingSlash(baseUrl));
        return new Uri(baseUri, EscapeRelativePath(filePath));
    }

    /// <summary>
    /// Resolves a file that sits next to the manifest itself (no base URL), used
    /// for launcher binaries listed alongside <c>launcher.json</c>.
    /// </summary>
    public static Uri ResolveSiblingUri(Uri manifestUrl, string filePath) =>
        new(manifestUrl, EscapeRelativePath(filePath));

    private static string EnsureTrailingSlash(string baseUrl)
    {
        // An empty base URL means the files sit next to the manifest itself.
        if (string.IsNullOrEmpty(baseUrl))
        {
            return baseUrl;
        }

        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private static string EscapeRelativePath(string filePath)
    {
        var segments = filePath.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = Uri.EscapeDataString(segments[i]);
        }

        return string.Join('/', segments);
    }
}
