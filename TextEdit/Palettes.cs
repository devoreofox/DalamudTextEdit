namespace ImGuiColorTextEditNet;

/// <summary>
/// Built-in color schemes
/// </summary>
public static class Palettes
{
    /// <summary>Default color scheme</summary>
    public static readonly uint[] Default =
    [
        0xffffffff, // Default
        0xffd69c56, // Keyword
        0xff00ff00, // Number
        0xff7070e0, // String
        0xff70a0e0, // Char literal
        0xffffffff, // Punctuation
        0xff408080, // Preprocessor
        0xffaaaaaa, // Identifier
        0xff9bc64d, // Known identifier
        0xffc040a0, // Preproc identifier
        0xff50c050, // Comment (single line)
        0xff70c050, // Comment (multi line)
        0xff101010, // Background
        0xffe0e0e0, // Cursor
        0x80a06020, // Selection
        0x800020ff, // ErrorMarker
        0xff707000, // Line number
    ];
}
