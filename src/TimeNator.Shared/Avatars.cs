namespace TimeNator.Shared;

/// <summary>
/// The built-in avatars. A user picks a key; clients draw the glyph. There are no uploads,
/// so there is no image storage and nothing to moderate.
/// </summary>
public static class Avatars
{
    public static IReadOnlyDictionary<string, string> Glyphs { get; } = new Dictionary<string, string>
    {
        ["owl"] = "🦉", ["fox"] = "🦊", ["cat"] = "🐱", ["panda"] = "🐼", ["tiger"] = "🐯",
        ["frog"] = "🐸", ["penguin"] = "🐧", ["octopus"] = "🐙", ["rocket"] = "🚀", ["books"] = "📚",
        ["coffee"] = "☕", ["plant"] = "🌱"
    };

    public static string GlyphFor(string? key) => key is not null && Glyphs.TryGetValue(key, out var g) ? g : "🙂";
}

public static class Themes
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    public static IReadOnlyList<string> All { get; } = [System, Light, Dark];
}
