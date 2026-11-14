using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moonfin.Calendar;
using Moonfin.Server.Api;
using Moonfin.Server.Services;
using Xunit;

namespace Moonfin.Server.Tests;

public class DirectSonarrCalendarTests
{
    private static PluginConfiguration Direct() => new()
    {
        SonarrCalendarEnabled = true,
        SonarrUrl = " https://sonarr.example.test/sonarr/ ",
        SonarrApiKey = "server-only-secret",
    };

    private static SeerrProxyResponse Answer(int status, string body = "[]") => new()
    {
        StatusCode = status, ContentType = "application/json", Body = Encoding.UTF8.GetBytes(body),
    };

    [Fact]
    public void DisabledByDefaultAndDoesNotEnableRadarr()
    {
        Assert.False(SonarrCalendarConnection.Configured(new PluginConfiguration(), true));
        Assert.True(SonarrCalendarConnection.Configured(Direct(), true));
        Assert.False(SonarrCalendarConnection.Configured(Direct(), false));
    }

    [Theory]
    [InlineData("file:///tmp/sonarr")]
    [InlineData("ftp://sonarr.example.test")]
    [InlineData("https://user:password@sonarr.example.test")]
    [InlineData("https://sonarr.example.test?apikey=secret")]
    [InlineData("https://sonarr.example.test#secret")]
    [InlineData("sonarr.example.test")]
    public void InvalidOrCredentialBearingUrlsAreNotAvailable(string url)
    {
        var config = Direct();
        config.SonarrUrl = url;
        Assert.False(SonarrCalendarConnection.TryGetDirect(config, out _, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("key\r\nInjected: yes")]
    public void MissingOrHeaderInjectingKeysAreRejected(string? key)
    {
        var config = Direct();
        config.SonarrApiKey = key;
        Assert.False(SonarrCalendarConnection.TryGetDirect(config, out _, out _));
    }

    [Fact]
    public async Task WithSeerrDisabledDirectCalendarNeedsNoSessionAndKeepsUtcTimes()
    {
        const string body = """[{"id":12,"airDateUtc":"2026-11-16T02:30:00Z","airDate":"2026-11-15"}]""";
        var result = await SonarrCalendarConnection.ReadAsync(Direct(), true,
            () => throw new InvalidOperationException("Must not ask Seerr"),
            (url, key) =>
            {
                Assert.Equal("https://sonarr.example.test/sonarr", url);
                Assert.Equal("server-only-secret", key);
                return Task.FromResult(Answer(200, body));
            });
        Assert.Equal(body, Encoding.UTF8.GetString(result.Body!));
    }

    [Fact]
    public async Task SuccessfulSeerrCalendarStillTakesPrecedence()
    {
        var config = Direct();
        config.SeerrEnabled = true;
        config.SeerrUrl = "https://seerr.example.test";
        var expected = Answer(200);
        var result = await SonarrCalendarConnection.ReadAsync(config, true,
            () => Task.FromResult(expected), (_, _) => throw new InvalidOperationException("Must not ask direct Sonarr"));
        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(502)]
    [InlineData(503)]
    public async Task MissingSeerrSessionOrFailedSeerrFallsBack(int status)
    {
        var config = Direct();
        config.SeerrEnabled = true;
        config.SeerrUrl = "https://seerr.example.test";
        var expected = Answer(200);
        Assert.Same(expected, await SonarrCalendarConnection.ReadAsync(config, true,
            () => Task.FromResult(Answer(status)), (_, _) => Task.FromResult(expected)));
    }

    [Fact]
    public async Task SeerrNetworkFailureAlsoFallsBack()
    {
        var config = Direct();
        config.SeerrEnabled = true;
        config.SeerrUrl = "https://seerr.example.test";
        Assert.Equal(200, (await SonarrCalendarConnection.ReadAsync(config, true,
            () => throw new HttpRequestException("offline"), (_, _) => Task.FromResult(Answer(200)))).StatusCode);
    }

    [Fact]
    public async Task DisabledFallbackPreservesExistingSeerrError()
    {
        var config = Direct();
        config.SonarrCalendarEnabled = false;
        config.SeerrEnabled = true;
        config.SeerrUrl = "https://seerr.example.test";
        var expected = Answer(503);
        Assert.Same(expected, await SonarrCalendarConnection.ReadAsync(config, true,
            () => Task.FromResult(expected), (_, _) => throw new InvalidOperationException()));
    }

    [Fact]
    public void ChangingConnectionOrKeyChangesCachePartition()
    {
        var config = Direct();
        var before = SonarrCalendarConnection.CachePartition(config);
        config.SonarrApiKey = "rotated-secret";
        Assert.NotEqual(before, SonarrCalendarConnection.CachePartition(config));
        before = SonarrCalendarConnection.CachePartition(config);
        config.SonarrUrl = "https://replacement.example.test";
        Assert.NotEqual(before, SonarrCalendarConnection.CachePartition(config));
    }

    [Fact]
    public async Task OutgoingCalendarUsesHeaderAndReverseProxyPathWithExistingQueryPolicy()
    {
        var handler = new CalendarHandler();
        var factory = new CalendarFactory(handler);
        var query = ArrCalendarWindow.Query(true, new DateTime(2026, 11, 14), new DateTime(2026, 11, 21), true);
        var result = await SeerrSessionService.TrySendArrAsync(factory, new NoOpLogger<SeerrSessionService>(),
            "sonarr", ["http://sonarr.lan:8989/sonarr"], "server-only-secret", HttpMethod.Get,
            "api/v3/calendar?" + query, null, _ => Task.FromResult(new[] { IPAddress.Parse("192.168.1.2") }),
            requireSafePrimary: true, clientName: SonarrCalendarConnection.HttpClientName);
        Assert.Equal(200, result!.StatusCode);
        Assert.Equal("server-only-secret", handler.Key);
        Assert.Equal("http://sonarr.lan:8989/sonarr/api/v3/calendar?start=2026-11-14&end=2026-11-21&includeSeries=true&includeEpisodeImages=true&unmonitored=true", handler.Url);
        Assert.DoesNotContain("server-only-secret", handler.Url);
        Assert.Equal(SonarrCalendarConnection.HttpClientName, factory.Name);
    }

    [Fact]
    public async Task DirectHttpOnPublicNetworkNeverGetsAnApiKey()
    {
        var handler = new CalendarHandler();
        var result = await SeerrSessionService.TrySendArrAsync(new CalendarFactory(handler), new NoOpLogger<SeerrSessionService>(),
            "sonarr", ["http://public.example.test"], "server-only-secret", HttpMethod.Get, "api/v3/calendar", null,
            _ => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }), requireSafePrimary: true,
            clientName: SonarrCalendarConnection.HttpClientName);
        Assert.Null(result);
        Assert.Null(handler.Url);
    }

    [Fact]
    public void CompatibleAndCleanCalendarRoutesStillRequireAuthentication()
    {
        var method = typeof(SeerrProxyController).GetMethod(nameof(SeerrProxyController.GetSonarrCalendar))!;
        Assert.NotEmpty(method.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        var routes = method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Select(x => x.Template);
        Assert.Contains("Sonarr/Calendar", routes);
        Assert.Contains("~/Spesh/Sonarr/Calendar", routes);
    }

    [Fact]
    public void CapabilityPayloadContainsOnlyNonSecretFlags()
    {
        var json = JsonSerializer.Serialize(new MoonfinPingResponse
        {
            CalendarAvailable = true, SonarrCalendarAvailable = true, DirectSonarrCalendarAvailable = true,
        });
        Assert.Contains("\"sonarrCalendarAvailable\":true", json);
        Assert.Contains("\"directSonarrCalendarAvailable\":true", json);
        Assert.DoesNotContain("SonarrApiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SonarrUrl", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CalendarHandler : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public string? Key { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            Key = request.Headers.GetValues("X-Api-Key").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") });
        }
    }

    private sealed class CalendarFactory(CalendarHandler handler) : IHttpClientFactory
    {
        public string? Name { get; private set; }
        public HttpClient CreateClient(string name)
        {
            Name = name;
            return new HttpClient(handler, false);
        }
    }
}
