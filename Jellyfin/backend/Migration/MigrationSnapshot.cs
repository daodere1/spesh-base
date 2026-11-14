using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Spesh.Base.Migration;

/// <summary>Prepares an offline migration. Never installs files or modifies the source.</summary>
public static class MigrationSnapshot
{
    public sealed record Result(int DataFiles, int UserProfiles, long DataBytes, string ConfigurationSha256);

    public static Result Prepare(string configurationsPath, string outputPath, string? pluginPath = null)
    {
        var source = Path.GetFullPath(configurationsPath);
        var output = Path.GetFullPath(outputPath);
        if (Contains(source, output) || Contains(output, source))
            throw new InvalidOperationException("Source and snapshot paths must be separate.");
        if (Directory.Exists(output) || File.Exists(output))
            throw new IOException("Snapshot destination already exists. Use a new directory.");

        var configPath = Path.Combine(source, "Moonfin.Server.xml");
        var dataPath = Path.Combine(source, "Moonfin");
        if ((File.GetAttributes(configPath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Linked configuration files are not supported in migration snapshots.");
        var config = File.ReadAllBytes(configPath);
        var converted = ConvertConfiguration(config);
        if (!Directory.Exists(dataPath)) throw new DirectoryNotFoundException("Source plugin data folder is missing.");
        var before = Inventory(dataPath);
        var users = 0;
        foreach (var name in before.Keys.Where(name => !name.Contains(Path.DirectorySeparatorChar)))
        {
            if (Path.GetExtension(name) == ".json" && Guid.TryParse(Path.GetFileNameWithoutExtension(name), out _))
            {
                using var json = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dataPath, name)));
                if (json.RootElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("A user settings file is not a JSON object.");
                users++;
            }
        }

        var original = Path.Combine(output, "original", "configurations");
        var prepared = Path.Combine(output, "prepared", "configurations");
        Directory.CreateDirectory(original);
        Directory.CreateDirectory(prepared);
        File.WriteAllBytes(Path.Combine(original, "Moonfin.Server.xml"), config);
        File.WriteAllBytes(Path.Combine(prepared, "Spesh.Base.xml"), converted);
        CopyVerified(dataPath, Path.Combine(original, "Moonfin"), before);
        CopyVerified(dataPath, Path.Combine(prepared, "SpeshBase"), before);
        var backup = Path.Combine(prepared, "SpeshBase", "config-backup");
        Directory.CreateDirectory(backup);
        File.WriteAllBytes(Path.Combine(backup, "Spesh.Base.xml"), converted);
        // A pre-migration backup must never restore the upstream relay or plugin settings.
        var oldBackup = Path.Combine(backup, "Moonfin.Server.xml");
        if (File.Exists(oldBackup)) File.Delete(oldBackup);

        if (pluginPath != null)
        {
            var plugin = Path.GetFullPath(pluginPath);
            if (Contains(plugin, output) || Contains(output, plugin))
                throw new InvalidOperationException("Plugin and snapshot paths must be separate.");
            CopyVerified(plugin, Path.Combine(output, "original", "plugin"), Inventory(plugin));
        }
        if (!config.SequenceEqual(File.ReadAllBytes(configPath)) || !Equal(before, Inventory(dataPath)))
            throw new IOException("Source changed during the snapshot. Stop Jellyfin and prepare a fresh snapshot.");

        var result = new Result(before.Count, users, before.Values.Sum(value => value.Length), Hash(config));
        File.WriteAllText(Path.Combine(output, "report.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "COMPLETE"), "Prepared only. Production has not been changed.\n");
        return result;
    }

    public static byte[] ConvertConfiguration(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var root = doc.Root ?? throw new InvalidDataException("Missing configuration root.");
        if (root.Name != "PluginConfiguration") throw new InvalidDataException("Unexpected configuration root.");
        Set("PushRelayEnabled", "false");
        Set("PushRelayUrl", "");
        Set("PushRelayAppKey", "");
        using var output = new MemoryStream();
        doc.Save(output, SaveOptions.DisableFormatting);
        return output.ToArray();

        void Set(string name, string value)
        {
            var matches = root.Elements(name).ToList();
            if (matches.Count > 1) throw new InvalidDataException("Duplicate configuration property.");
            if (matches.Count == 0) root.Add(new XElement(name, value));
            else matches[0].Value = value;
        }
    }

    private sealed record Stamp(long Length, string Sha256);
    private static Dictionary<string, Stamp> Inventory(string root)
    {
        var result = new Dictionary<string, Stamp>(StringComparer.Ordinal);
        Visit(root);
        return result;
        void Visit(string directory)
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked directories are not supported in migration snapshots.");
            foreach (var item in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(item);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked files are not supported in migration snapshots.");
                if ((attributes & FileAttributes.Directory) != 0) Visit(item);
                else
                {
                    using var stream = File.OpenRead(item);
                    result.Add(Path.GetRelativePath(root, item), new Stamp(stream.Length, Convert.ToHexString(SHA256.HashData(stream))));
                }
            }
        }
    }
    private static void CopyVerified(string source, string target, Dictionary<string, Stamp> inventory)
    {
        Directory.CreateDirectory(target);
        foreach (var name in inventory.Keys)
        {
            var path = Path.Combine(target, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(Path.Combine(source, name), path, overwrite: false);
        }
        if (!Equal(inventory, Inventory(target))) throw new IOException("Snapshot checksum verification failed.");
    }
    private static bool Equal(Dictionary<string, Stamp> left, Dictionary<string, Stamp> right) =>
        left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var stamp) && stamp == pair.Value);
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static bool Contains(string parent, string child) =>
        string.Equals(parent, child, StringComparison.OrdinalIgnoreCase) || child.StartsWith(Path.TrimEndingDirectorySeparator(parent) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
