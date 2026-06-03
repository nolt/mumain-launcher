using System.Net.Http;

namespace Launcher.Core;

/// <summary>
/// <see cref="IPatchSource"/> backed by HTTP. Downloads the manifest and each
/// file from the patch host derived from the manifest URL.
/// </summary>
public sealed class HttpPatchSource : IPatchSource
{
    private readonly HttpClient _httpClient;
    private readonly Uri _manifestUrl;
    private string? _baseUrl;

    public HttpPatchSource(HttpClient httpClient, string manifestUrl)
    {
        _httpClient = httpClient;
        _manifestUrl = new Uri(manifestUrl);
    }

    public async Task<Manifest> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(_manifestUrl, cancellationToken);
            var manifest = ManifestSerializer.Deserialize(json)
                ?? throw new UpdateException("The patch manifest was empty or invalid.");
            _baseUrl = manifest.BaseUrl;
            return manifest;
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateException("Could not download the patch manifest.", ex);
        }
    }

    public async Task<Stream> OpenFileAsync(ManifestFile file, CancellationToken cancellationToken = default)
    {
        if (_baseUrl is null)
        {
            throw new InvalidOperationException($"{nameof(GetManifestAsync)} must be called before {nameof(OpenFileAsync)}.");
        }

        var uri = PatchUris.ResolveFileUri(_manifestUrl, _baseUrl, file.Path);
        try
        {
            var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateException($"Could not download file: {file.Path}", ex);
        }
    }
}
