using System;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>
/// Represents options for configuring the behavior and appearance of the text editor.
/// </summary>
public class TextEditorOptions
{
    int _tabSize = 4;

    /// <summary>
    /// Whether the text editor is read-only or allows editing.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the editor should colorize text using a syntax highlighter.
    /// </summary>
    public bool IsColorizerEnabled { get; set; } = true;

    /// <summary>
    /// The number of spaces to use for a tab character.
    /// </summary>
    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = Math.Max(1, Math.Min(32, value));
    }

    internal int NextTab(int column) => column / TabSize * TabSize + TabSize;
}
