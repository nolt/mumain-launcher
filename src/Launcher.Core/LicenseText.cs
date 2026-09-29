namespace Launcher.Core;

/// <summary>One paragraph of the license texts; headings are shown highlighted.</summary>
public readonly record struct LicenseParagraph(string Text, bool IsHeading);

/// <summary>
/// Splits the launcher's LICENSE and THIRD-PARTY-LICENSES.md into paragraphs, so
/// the About → Licenses list only lays out what is on screen. Markdown headings
/// become heading paragraphs and code fences are dropped; inside a fence every
/// line is plain text, so banner lines like "#####" stay text.
/// </summary>
public static class LicenseText
{
    public static IReadOnlyList<LicenseParagraph> Split(params string[] documents)
    {
        var paragraphs = new List<LicenseParagraph>();
        var lines = new List<string>();

        void Flush()
        {
            if (lines.Count > 0)
            {
                paragraphs.Add(new LicenseParagraph(string.Join('\n', lines), false));
                lines.Clear();
            }
        }

        foreach (var document in documents)
        {
            var inFence = false;
            foreach (var raw in document.ReplaceLineEndings("\n").Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.StartsWith("```"))
                {
                    Flush();
                    inFence = !inFence;
                }
                else if (!inFence && line.StartsWith('#'))
                {
                    Flush();
                    paragraphs.Add(new LicenseParagraph(line.TrimStart('#').Trim(), true));
                }
                else if (line.Length == 0)
                {
                    Flush();
                }
                else
                {
                    lines.Add(line);
                }
            }

            Flush();
        }

        return paragraphs;
    }
}
