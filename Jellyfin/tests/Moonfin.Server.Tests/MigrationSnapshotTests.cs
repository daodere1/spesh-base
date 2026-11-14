using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Spesh.Base.Migration;
using Xunit;

namespace Moonfin.Server.Tests;

public sealed class MigrationSnapshotTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spesh-migration-" + Guid.NewGuid().ToString("N"));
    private string Source => Path.Combine(_root, "source");
    private string Output => Path.Combine(_root, "snapshot");

    public MigrationSnapshotTests()
    {
        Directory.CreateDirectory(Path.Combine(Source, "Moonfin", "themes"));
        File.WriteAllText(Path.Combine(Source, "Moonfin.Server.xml"), "<PluginConfiguration><EnableSettingsSync>true</EnableSettingsSync><FutureSetting answer=\"42\">keep</FutureSetting><PushRelayEnabled>true</PushRelayEnabled><PushRelayAppKey>old-key</PushRelayAppKey><PushRelayUrl>https://old-relay.invalid</PushRelayUrl></PluginConfiguration>");
        File.WriteAllText(Path.Combine(Source, "Moonfin", Guid.Empty + ".json"), "{\"schemaVersion\":2,\"global\":{\"theme\":\"spesha-style\"}}");
        File.WriteAllText(Path.Combine(Source, "Moonfin", "themes", "spesha-style.json"), "{\"id\":\"spesha-style\"}");
    }

    [Fact]
    public void SnapshotPreservesSourceAndUserDataAndConvertsOnlyTheRelaySettings()
    {
        var original = File.ReadAllBytes(Path.Combine(Source, "Moonfin.Server.xml"));
        var result = MigrationSnapshot.Prepare(Source, Output);
        Assert.Equal(1, result.UserProfiles);
        Assert.Equal(2, result.DataFiles);
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(Source, "Moonfin.Server.xml")));
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(Output, "original", "configurations", "Moonfin.Server.xml")));
        var prepared = Path.Combine(Output, "prepared", "configurations");
        var xml = XDocument.Load(Path.Combine(prepared, "Spesh.Base.xml"));
        Assert.Equal("false", xml.Root!.Element("PushRelayEnabled")!.Value);
        Assert.Equal("", xml.Root.Element("PushRelayUrl")!.Value);
        Assert.Equal("", xml.Root.Element("PushRelayAppKey")!.Value);
        Assert.Equal("42", xml.Root.Element("FutureSetting")!.Attribute("answer")!.Value);
        Assert.Equal("true", xml.Root.Element("EnableSettingsSync")!.Value);
        Assert.Equal(File.ReadAllBytes(Path.Combine(Source, "Moonfin", Guid.Empty + ".json")), File.ReadAllBytes(Path.Combine(prepared, "SpeshBase", Guid.Empty + ".json")));
        Assert.Equal(File.ReadAllBytes(Path.Combine(prepared, "Spesh.Base.xml")), File.ReadAllBytes(Path.Combine(prepared, "SpeshBase", "config-backup", "Spesh.Base.xml")));
        Assert.True(File.Exists(Path.Combine(Output, "COMPLETE")));
        Assert.DoesNotContain("old-key", File.ReadAllText(Path.Combine(Output, "report.json")));
    }

    [Fact]
    public void ExistingDestinationIsNeverOverwritten()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "keep"), "unchanged");
        Assert.Throws<IOException>(() => MigrationSnapshot.Prepare(Source, Output));
        Assert.Equal("unchanged", File.ReadAllText(Path.Combine(Output, "keep")));
    }

    [Fact]
    public void DestinationCannotBeInsideTheSource()
    {
        Assert.Throws<InvalidOperationException>(() => MigrationSnapshot.Prepare(Source, Path.Combine(Source, "snapshot")));
    }

    [Fact]
    public void MalformedUserSettingsPreventACompletedSnapshot()
    {
        File.WriteAllText(Path.Combine(Source, "Moonfin", Guid.Empty + ".json"), "broken");
        Assert.ThrowsAny<JsonException>(() => MigrationSnapshot.Prepare(Source, Output));
        Assert.False(File.Exists(Path.Combine(Output, "COMPLETE")));
    }

    [Theory]
    [InlineData("<WrongRoot />")]
    [InlineData("<PluginConfiguration><PushRelayEnabled>true</PushRelayEnabled><PushRelayEnabled>false</PushRelayEnabled></PluginConfiguration>")]
    public void InvalidConfigurationCannotBeConverted(string xml)
    {
        Assert.Throws<InvalidDataException>(() => MigrationSnapshot.ConvertConfiguration(Encoding.UTF8.GetBytes(xml)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

