using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class LicenseTextTests
{
    [Fact]
    public void Split_KeepsDocumentOrderAndLineBreaksWithinParagraphs()
    {
        var paragraphs = LicenseText.Split("MIT License\n\nCopyright (c) 2026\n\nline one\nline two\n", "# Third-party");

        Assert.Equal(
            [
                new LicenseParagraph("MIT License", false),
                new LicenseParagraph("Copyright (c) 2026", false),
                new LicenseParagraph("line one\nline two", false),
                new LicenseParagraph("Third-party", true),
            ],
            paragraphs);
    }

    [Fact]
    public void Split_TurnsHeadingsOutsideFencesIntoHeadingsAndDropsFences()
    {
        var paragraphs = LicenseText.Split("## Skia — license\r\n\r\nUsed by: SkiaSharp\r\n\r\n```text\r\n# ANGLE\r\n#####\r\n```\r\n");

        Assert.Equal(
            [
                new LicenseParagraph("Skia — license", true),
                new LicenseParagraph("Used by: SkiaSharp", false),
                new LicenseParagraph("# ANGLE\n#####", false),
            ],
            paragraphs);
    }
}
