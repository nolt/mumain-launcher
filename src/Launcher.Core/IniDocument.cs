using System.Globalization;
using System.IO;
using System.Text;

namespace Launcher.Core;

/// <summary>
/// A line-preserving INI editor. It reads a file keeping its exact bytes, encoding
/// and line endings, lets callers replace specific keys, and writes it back so
/// every untouched line - comments, unknown sections, other keys - survives
/// verbatim. Used to edit the client's config.ini without disturbing server
/// settings or saved login. No BOM is ever added that wasn't already there.
/// </summary>
internal sealed class IniDocument
{
    private readonly List<string> _lines;
    private readonly string _eol;
    private readonly bool _trailingNewline;
    private readonly Encoding _encoding;
    private readonly byte[] _preamble;

    private IniDocument(List<string> lines, string eol, bool trailingNewline, Encoding encoding, byte[] preamble)
    {
        _lines = lines;
        _eol = eol;
        _trailingNewline = trailingNewline;
        _encoding = encoding;
        _preamble = preamble;
    }

    public static IniDocument Empty() =>
        new([], "\n", trailingNewline: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), []);

    public static IniDocument Load(string path)
    {
        var bytes = File.ReadAllBytes(path);

        Encoding encoding;
        byte[] preamble;
        int start;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            preamble = [0xEF, 0xBB, 0xBF];
            start = 3;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = Encoding.Unicode;
            preamble = [0xFF, 0xFE];
            start = 2;
        }
        else
        {
            // No BOM: treat as raw single bytes (Latin1 maps 0..255 one-to-one),
            // so any non-ASCII bytes in untouched lines round-trip unchanged.
            encoding = Encoding.Latin1;
            preamble = [];
            start = 0;
        }

        var text = encoding.GetString(bytes, start, bytes.Length - start);
        var eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var trailingNewline = text.EndsWith('\n');
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        if (trailingNewline && lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return new IniDocument(lines, eol, trailingNewline, encoding, preamble);
    }

    public void Save(string path)
    {
        var joined = string.Join(_eol, _lines);
        if (_trailingNewline)
        {
            joined += _eol;
        }

        using var stream = File.Create(path);
        if (_preamble.Length > 0)
        {
            stream.Write(_preamble);
        }

        stream.Write(_encoding.GetBytes(joined));
    }

    public int GetInt(string section, string key, int fallback) =>
        Get(section, key) is { } raw
        && int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    public bool GetBool(string section, string key, bool fallback)
    {
        if (Get(section, key) is not { } raw)
        {
            return fallback;
        }

        raw = raw.Trim();
        return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public void Set(string section, string key, string value)
    {
        var (start, end) = SectionRange(section);
        if (start < 0)
        {
            if (_lines.Count > 0 && _lines[^1].Length != 0)
            {
                _lines.Add(string.Empty);
            }

            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
            return;
        }

        for (var i = start; i < end; i++)
        {
            if (TryParseKey(_lines[i], out var existing, out _) && existing.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                var equals = _lines[i].IndexOf('=');
                _lines[i] = string.Concat(_lines[i].AsSpan(0, equals + 1), value);
                return;
            }
        }

        _lines.Insert(end, $"{key}={value}");
    }

    private string? Get(string section, string key)
    {
        var (start, end) = SectionRange(section);
        if (start < 0)
        {
            return null;
        }

        for (var i = start; i < end; i++)
        {
            if (TryParseKey(_lines[i], out var existing, out var value) && existing.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Returns the [start, end) line range of a section's body, or (-1, -1) if absent.</summary>
    private (int Start, int End) SectionRange(string section)
    {
        var header = -1;
        for (var i = 0; i < _lines.Count; i++)
        {
            if (IsSectionHeader(_lines[i], out var name) && name.Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                header = i;
                break;
            }
        }

        if (header < 0)
        {
            return (-1, -1);
        }

        var end = _lines.Count;
        for (var i = header + 1; i < _lines.Count; i++)
        {
            if (IsSectionHeader(_lines[i], out _))
            {
                end = i;
                break;
            }
        }

        return (header + 1, end);
    }

    private static bool IsSectionHeader(string line, out string name)
    {
        var trimmed = line.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            name = trimmed[1..^1].Trim();
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static bool TryParseKey(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] is ';' or '#' or '[')
        {
            return false;
        }

        var equals = line.IndexOf('=');
        if (equals < 0)
        {
            return false;
        }

        key = line[..equals].Trim();
        value = line[(equals + 1)..];
        return key.Length > 0;
    }
}
