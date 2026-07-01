# DalamudTextEdit

A multi-line text editor for Dalamud plugins that actually does syntax highlighting. ImGui's built-in `InputTextMultiline` paints the whole box a single color, which is fine for a name field and useless for anything code-shaped. This gives you real per-token coloring in the field as you type.

You bring an `ISyntaxHighlighter`; it handles the coloring.

## What you get

- Syntax highlighting you plug in yourself
- Line numbers you can turn off
- Error markers with tooltips on hover
- Undo/redo, selection, clipboard, the usual editing you'd expect
- Hover tooltips (your highlighter decides what they say)
- It looks like it belongs: the background follows your ImGui theme, the cursor blinks like a normal `InputText`, Escape drops focus, and it fills whatever space you hand it
- Every color is overridable if you don't like mine

## Where this came from

This is a fork of [ImGuiColorTextEditNet](https://github.com/csinkers/ImGuiColorTextEditNet) by csinkers, which is itself a C# port of Balázs Jákó's [ImGuiColorTextEdit](https://github.com/BalazsJako/ImGuiColorTextEdit). Both are MIT, and so is this.

What I changed:

- Pointed it at `Dalamud.Bindings.ImGui` instead of [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) so it runs inside a Dalamud plugin.
- Cut everything I didn't need: breakpoints, the debugger's executing-line highlight, overwrite mode, JSON state serialization, the bundled C-style and regex highlighters, the spare color schemes, tab-to-indent. It's an editor, not an IDE.
- Made it feel native: theme-aware colors, a cursor blink that matches `InputText`, Escape-to-defocus, and a line-number toggle.

## Getting it

You need a Dalamud plugin to host it. It references `Dalamud.Bindings.ImGui.dll` from your local Dalamud dev folder and expects the plugin to supply that at runtime.

For now it's a git submodule plus a `<ProjectReference>`. A NuGet package may come later.

## Using it

```csharp
using ImGuiColorTextEditNet;
using System.Numerics;

readonly TextEditor _editor = new()
{
    AllText = "/say hi\n:if {hpp} < 50\n/ac Cure\n:endif",
    SyntaxHighlighter = new MyHighlighter(),
};
_editor.Renderer.ShowLineNumbers = true;

// then, every frame inside a window:
_editor.Render("###editor", new Vector2(-1, ImGui.GetContentRegionAvail().Y));
```

`Render` returns `true` on the frames where the text changed, so you know when to save. Read or replace the buffer with `_editor.AllText`.

## Your own highlighting

Implement `ISyntaxHighlighter` and hand it to `editor.SyntaxHighlighter`. `Colorize` gets one line of glyphs and you set the palette index on each one:

```csharp
public sealed class MyHighlighter : ISyntaxHighlighter
{
    public bool AutoIndentation => false;
    public int MaxLinesPerFrame => 1000;
    public string? GetTooltip(string id) => null;

    public object Colorize(Span<Glyph> line, object? state)
    {
        for (var i = 0; i < line.Length; i++)
            line[i] = new Glyph(line[i].Char, PaletteIndex.Default); // pick a real index per token

        return state;
    }
}
```

Colors live in `PaletteIndex`. Override one with `editor.Renderer.SetColor(PaletteIndex.Keyword, 0xff_00_ff_ff)`, or swap the whole palette via `editor.Renderer.Palette`. The chrome (background, selection, cursor, line numbers) already tracks your ImGui theme, so you usually only touch the token colors.

## License

MIT. See [LICENSE](LICENSE). It keeps csinkers' and Balázs Jákó's original copyright lines alongside mine, since that's how MIT forks work.
