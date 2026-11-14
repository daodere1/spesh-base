using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Moonfin.Games;

/// <summary>
/// Framework helpers used by the Jellyfin games features.
/// </summary>
internal static class GamesCompat
{
    /// <summary>Lowercase hex, the form every hash in this code is stored in.</summary>
    internal static string LowerHex(byte[] bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();

    internal static byte[] Sha256(byte[] data)
    {
        return SHA256.HashData(data);
    }

    internal static byte[] Sha256(Stream data)
    {
        return SHA256.HashData(data);
    }

    internal static byte[] Sha1(byte[] data)
    {
        return SHA1.HashData(data);
    }

    internal static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }

    internal static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
    }

    internal static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
    }

    internal static void ThrowIfLessThan(int value, int other, [CallerArgumentExpression("value")] string? paramName = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, other, paramName);
    }

    internal static bool IsWindows() =>
        OperatingSystem.IsWindows();

    /// <summary>Moves a finished temp file over its target, replacing any copy already there.</summary>
    internal static void MoveOverwrite(string source, string destination)
    {
        File.Move(source, destination, overwrite: true);
    }

    internal static string[] SplitTrimmed(string value, char separator) =>
        value.Split(separator, StringSplitOptions.TrimEntries);

    /// <summary>The raw Last-Modified header, or null.</summary>
    internal static string? LastModified(HttpContentHeaders headers) =>
        headers.NonValidated.TryGetValues("Last-Modified", out var modified) ? modified.ToString() : null;

    internal static IEqualityComparer<T> ByReference<T>()
        where T : class =>
        ReferenceEqualityComparer.Instance;

}
