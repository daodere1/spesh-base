using System.Security.Cryptography;
using System.Text;

namespace Moonfin.Server.Services;

/// <summary>Server-only connection selection for the read-only Sonarr calendar.</summary>
internal static class SonarrCalendarConnection
{
    public const string HttpClientName = "SpeshDirectSonarr";

    public static bool SeerrConfigured(PluginConfiguration? config) =>
        config?.SeerrEnabled == true && !string.IsNullOrEmpty(config.GetEffectiveSeerrUrl());

    public static bool TryGetDirect(PluginConfiguration? config, out string url, out string key)
    {
        url = string.Empty;
        key = string.Empty;
        if (config?.SonarrCalendarEnabled != true
            || !Uri.TryCreate(config.SonarrUrl?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || string.IsNullOrWhiteSpace(config.SonarrApiKey) || config.SonarrApiKey.Any(char.IsControl))
        {
            return false;
        }

        url = uri.AbsoluteUri.TrimEnd('/');
        key = config.SonarrApiKey.Trim();
        return true;
    }

    public static bool Configured(PluginConfiguration? config, bool isSonarr) =>
        SeerrConfigured(config) || (isSonarr && TryGetDirect(config, out _, out _));

    // Prevent a response cached under old credentials or a previous server from being reused.
    // Only the hash is used internally; neither credentials nor the hash reach clients.
    public static string CachePartition(PluginConfiguration? config) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",
            config?.SeerrEnabled, config?.GetEffectiveSeerrUrl(), config?.SonarrCalendarEnabled,
            config?.SonarrUrl, config?.SonarrApiKey))));

    public static async Task<SeerrProxyResponse> ReadAsync(
        PluginConfiguration? config,
        bool isSonarr,
        Func<Task<SeerrProxyResponse>> readSeerr,
        Func<string, string, Task<SeerrProxyResponse>> readDirect)
    {
        SeerrProxyResponse? seerr = null;
        if (SeerrConfigured(config))
        {
            try
            {
                seerr = await readSeerr().ConfigureAwait(false);
                if (seerr.StatusCode == 200 && seerr.Body != null) return seerr;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Network failure must not make the independently configured calendar unusable.
                seerr = new SeerrProxyResponse { StatusCode = 502 };
            }
        }

        if (isSonarr && TryGetDirect(config, out var url, out var key))
        {
            return await readDirect(url, key).ConfigureAwait(false);
        }

        return seerr ?? new SeerrProxyResponse
        {
            StatusCode = 503,
            ContentType = "application/json",
            Body = Encoding.UTF8.GetBytes("{\"error\":\"No calendar connection is configured\"}"),
        };
    }
}
