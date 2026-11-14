using Spesh.Base.Migration;
using System.Text.Json;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: PrepareMigration <source-configurations-directory> <new-snapshot-directory> [source-plugin-directory]");
    return 2;
}
try
{
    var result = MigrationSnapshot.Prepare(args[0], args[1], args.Length == 3 ? args[2] : null);
    Console.WriteLine(JsonSerializer.Serialize(result));
    return 0;
}
catch (Exception ex)
{
    // Error types identify failure without printing private paths or configuration values.
    Console.Error.WriteLine($"Snapshot incomplete ({ex.GetType().Name}). Production was not changed. Use a fresh destination after resolving the source or path problem.");
    return 1;
}
