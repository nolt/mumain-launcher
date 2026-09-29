// Writes THIRD-PARTY-LICENSES.md from restored project.assets.json files: the license
// texts of every NuGet package and .NET runtime pack that ends up in the published
// launcher. Run through `./build.sh licenses`, which restores each RID first.
//
// Usage: dotnet run ThirdPartyLicenses.cs -- <output.md> <project.assets.json>...
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ThirdPartyLicenses.cs <output.md> <project.assets.json>...");
    return 2;
}

var output = args[0];
var assetsFiles = args[1..].Select(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement).ToList();
var packageFolder = assetsFiles[0].GetProperty("packageFolders").EnumerateObject().First().Name;

// Packages that put at least one runtime or native file into a RID-specific build.
// Build-only packages and natives for other platforms carry only "_._" or nothing.
var shipped = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var target in assetsFiles.SelectMany(a => a.GetProperty("targets").EnumerateObject()).Where(t => t.Name.Contains('/')))
{
    foreach (var library in target.Value.EnumerateObject())
    {
        if (library.Value.GetProperty("type").GetString() != "package")
        {
            continue;
        }

        var ships = new[] { "runtime", "native" }.Any(kind =>
            library.Value.TryGetProperty(kind, out var files) &&
            files.EnumerateObject().Any(f => !f.Name.EndsWith("_._")));
        if (ships)
        {
            shipped.Add(library.Name);
        }
    }
}

// The runtime (and Windows apphost) packs a self-contained publish copies in.
foreach (var framework in assetsFiles.SelectMany(a => a.GetProperty("project").GetProperty("frameworks").EnumerateObject()))
{
    if (!framework.Value.TryGetProperty("downloadDependencies", out var downloads))
    {
        continue;
    }

    foreach (var pack in downloads.EnumerateArray())
    {
        var name = pack.GetProperty("name").GetString()!;
        if (name.StartsWith("Microsoft.NETCore.App.Runtime.") || name.StartsWith("Microsoft.NETCore.App.Host."))
        {
            shipped.Add($"{name}/{pack.GetProperty("version").GetString()!.Trim('[', ']').Split(',')[0].Trim()}");
        }
    }
}

// One section per distinct text, listing every package that ships it.
var sections = new List<(string Text, bool IsNotice, SortedSet<string> UsedBy)>();
var missing = new List<string>();

void Add(string text, bool isNotice, string usedBy)
{
    text = text.Replace("\r\n", "\n").Replace("\r", "\n").Trim('\n');
    var section = sections.FirstOrDefault(s => s.Text == text);
    if (section.Text is null)
    {
        section = (text, isNotice, new SortedSet<string>(StringComparer.OrdinalIgnoreCase));
        sections.Add(section);
    }

    section.UsedBy.Add(usedBy);
}

foreach (var package in shipped)
{
    var (id, version) = (package.Split('/')[0], package.Split('/')[1]);
    var directory = Path.Combine(packageFolder, id.ToLowerInvariant(), version.ToLowerInvariant());
    var nuspec = XDocument.Load(Directory.GetFiles(directory, "*.nuspec").Single());
    string? Meta(string element) => nuspec.Descendants().FirstOrDefault(e => e.Name.LocalName == element)?.Value.Trim();

    var files = Directory.GetFiles(directory)
        .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^(LICENSE|THIRD-PARTY-NOTICES|NOTICE)(\.(txt|md))?$", RegexOptions.IgnoreCase))
        .OrderBy(f => Path.GetFileName(f).StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ToList();

    if (files.Count > 0)
    {
        foreach (var file in files)
        {
            Add(File.ReadAllText(file), !Path.GetFileName(file).StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase), id);
        }
    }
    else if (Meta("license") == "MIT")
    {
        // License given only as an SPDX expression: the MIT text with the package's copyright.
        var copyright = Meta("copyright") ?? Meta("authors") ?? id;
        if (!copyright.StartsWith("Copyright", StringComparison.OrdinalIgnoreCase))
        {
            copyright = $"Copyright {copyright}";
        }

        Add($"MIT License\n\n{copyright}\n\n{MitBody}", false, id);
    }
    else
    {
        missing.Add($"{package} (license: {Meta("license") ?? Meta("licenseUrl") ?? "none"})");
    }
}

if (missing.Count > 0)
{
    Console.Error.WriteLine("No license text found for:");
    missing.ForEach(m => Console.Error.WriteLine($"  {m}"));
    Console.Error.WriteLine("Add the text to tools/licenses/ and list it in Extras in ThirdPartyLicenses.cs.");
    return 1;
}

// Texts that no package metadata carries.
var toolsDirectory = (string)AppContext.GetData("EntryPointFileDirectoryPath")!;
foreach (var (file, usedBy) in Extras)
{
    Add(File.ReadAllText(Path.Combine(toolsDirectory, file)), false, usedBy);
}

var markdown = new StringBuilder()
    .Append("# Third-party licenses\n\n")
    .Append("The launcher contains the components listed below. Generated by `./build.sh licenses`.\n");
foreach (var (text, isNotice, usedBy) in sections)
{
    markdown.Append($"\n## {Title(usedBy)} — {(isNotice ? "third-party notices" : "license")}\n\n")
        .Append($"Used by: {string.Join(", ", usedBy)}\n\n")
        .Append("```text\n").Append(text).Append("\n```\n");
}

File.WriteAllText(output, markdown.ToString());
Console.WriteLine($"{output}: {sections.Count} texts from {shipped.Count} packages");
return 0;

// Heading for a section: the projects behind its packages. Packages of one
// project share the first part of their id (Avalonia.*, SkiaSharp.*).
static string Title(IEnumerable<string> usedBy) => string.Join(", ", usedBy
    .Select(id => id.StartsWith("Microsoft.NETCore.App.") ? ".NET runtime" : id)
    .GroupBy(id => id.Split('.')[0])
    .Select(g => g.Distinct().Count() == 1 ? g.First() : g.Key)
    .Distinct());

partial class Program
{
    static readonly (string File, string UsedBy)[] Extras =
    [
        ("Inter-OFL.txt", "Inter font (inside Avalonia.Fonts.Inter)"),
    ];

    const string MitBody = """
        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """;
}
