using System.Text.Json;
using Moonfin.Server.Services;
using Xunit;

namespace Moonfin.Server.Tests;

public class SpeshCinematicThemeTests
{
    [Fact]
    public void BundledThemePassesClientSchemaValidation()
    {
        using var stream = typeof(MoonfinThemeStore).Assembly.GetManifestResourceStream("Spesh.Base.Themes.spesh-cinematic.json");
        Assert.NotNull(stream);
        using var document = JsonDocument.Parse(stream);
        var result = new MoonfinThemeValidator().Validate(document.RootElement);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("spesh-cinematic", document.RootElement.GetProperty("id").GetString());
    }
}
