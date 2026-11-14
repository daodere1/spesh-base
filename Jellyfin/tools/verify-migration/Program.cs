using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using Moonfin.Server;
using Moonfin.Server.Models;

if (args.Length != 1) return 2;
try
{
    var snapshot = Path.GetFullPath(args[0]);
    if (!File.Exists(Path.Combine(snapshot, "COMPLETE"))) throw new InvalidDataException();
    var prepared = Path.Combine(snapshot, "prepared", "configurations");
    var configPath = Path.Combine(prepared, "Spesh.Base.xml");
    using var stream = File.OpenRead(configPath);
    var config = (PluginConfiguration)new XmlSerializer(typeof(PluginConfiguration)).Deserialize(stream)!;
    if (config.PushRelayEnabled || !string.IsNullOrEmpty(config.PushRelayUrl) || !string.IsNullOrEmpty(config.PushRelayAppKey))
        throw new InvalidDataException();
    var originalXml = XDocument.Load(Path.Combine(snapshot, "original", "configurations", "Moonfin.Server.xml"));
    var preparedXml = XDocument.Load(configPath);
    foreach (var document in new[] { originalXml, preparedXml })
        foreach (var name in new[] { "PushRelayEnabled", "PushRelayUrl", "PushRelayAppKey" })
            document.Root!.Elements(name).Remove();
    if (!XNode.DeepEquals(originalXml, preparedXml)) throw new InvalidDataException();
    var data = Path.Combine(prepared, "SpeshBase");
    var users = 0;
    foreach (var file in Directory.EnumerateFiles(data, "*.json"))
    {
        if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out _)) continue;
        if (JsonSerializer.Deserialize<MoonfinUserSettings>(File.ReadAllText(file)) == null) throw new InvalidDataException();
        users++;
    }
    foreach (var entry in config.UploadedThemes)
    {
        if (Path.GetFileName(entry.FileName) != entry.FileName) throw new InvalidDataException();
        var bytes = File.ReadAllBytes(Path.Combine(data, "themes", entry.FileName));
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), entry.ChecksumSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
        using var theme = JsonDocument.Parse(bytes);
        if (theme.RootElement.GetProperty("id").GetString() != entry.Id) throw new InvalidDataException();
    }
    var report = new { ConfigurationDeserialized = true, OtherConfigurationUnchanged = true, UserProfilesDeserialized = users, UploadedThemesVerified = config.UploadedThemes.Count, UpstreamRelayDisabled = true, ProductionChanged = false };
    File.WriteAllText(Path.Combine(snapshot, "verification.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(report));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration verification failed ({ex.GetType().Name}); source data was not modified.");
    return 1;
}
