using Launcher.Core;
using PatchManifest;

return await Run(args);

static async Task<int> Run(string[] args)
{
    CommandLineOptions options;
    try
    {
        options = CommandLineParser.Parse(args);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.Error.WriteLine();
        PrintUsage();
        return 1;
    }

    if (!Directory.Exists(options.InputDirectory))
    {
        Console.Error.WriteLine($"Error: input directory not found: {options.InputDirectory}");
        return 1;
    }

    var manifest = await new ManifestGenerator(options).GenerateAsync();
    var json = ManifestSerializer.Serialize(manifest);

    var outputDirectory = Path.GetDirectoryName(options.OutputFile);
    if (!string.IsNullOrEmpty(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    await File.WriteAllTextAsync(options.OutputFile, json);

    PrintSummary(manifest, options.OutputFile);
    return 0;
}

static void PrintSummary(Manifest manifest, string outputFile)
{
    long totalBytes = 0;
    foreach (var file in manifest.Files)
    {
        totalBytes += file.Size;
    }

    Console.WriteLine($"Version:    {manifest.Version}");
    Console.WriteLine($"Files:      {manifest.Files.Count} ({ByteSizeFormatter.Format(totalBytes)})");
    Console.WriteLine($"Manifest:   {outputFile}");
}

static void PrintUsage()
{
    Console.Error.WriteLine(
        """
        Usage:
          PatchManifest --input <dir> [--output <file>] [--base-url <url>]
                        [--version <str>] [--exclude <glob>]...

          --input     Release directory to scan (required).
          --output    Manifest file to write (default: <input>/version.json).
          --base-url  Location of files relative to the manifest (default: files/).
          --version   Release tag (default: today's UTC date, yyyy.MM.dd).
          --exclude   Extra file-name glob to skip (repeatable).
        """);
}
