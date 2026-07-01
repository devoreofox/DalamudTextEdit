# DalamudTextEdit

A colorizing, multi-line text editor widget for [Dalamud](https://github.com/goatcorp/Dalamud) plugins, built on `Dalamud.Bindings.ImGui`. It gives you an in-field, syntax-highlighted editing surface — something ImGui's stock `InputTextMultiline` can't do, since it renders the entire buffer in a single color.

Highlighting is pluggable: you supply an `ISyntaxHighlighter` and the editor colors tokens as the user types.

## Features

- Pluggable syntax highlighting (`ISyntaxHighlighter`)
- Toggleable line numbers
- Error markers with hover tooltips
- Undo/redo, selection, and clipboard
- Word tooltips (driven by the highlighter)
- Native feel: background follows the active ImGui theme, cursor blink matches `InputText`, Escape releases focus, and it fills its container like `InputTextMultiline`
- Fully overridable color palette

## Origin & credits

This is a Dalamud-targeted fork of **[ImGuiColorTextEditNet](https://github.com/csinkers/ImGuiColorTextEditNet)** by [csinkers](https://github.com/csinkers), which is itself a C# port of **[ImGuiColorTextEdit](https://github.com/BalazsJako/ImGuiColorTextEdit)** by [BalázsJákó](https://github.com/BalazsJako). Both are MIT licensed, and this fork stays MIT.

What changed here:

- **Retargeted** from [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) to `Dalamud.Bindings.ImGui`, so it runs inside Dalamud plugins.
- **Trimmed** to a lean editor: removed breakpoints, the debugger executing-line highlight, overwrite/insert mode, JSON state serialization, the built-in C-style and regex language highlighters, the extra color schemes, and tab-to-indent.
- **Native polish**: ImGui-theme-driven chrome (background, selection, cursor, line numbers), an `InputText`-matched cursor blink, Escape-to-defocus, and a line-number toggle.

## Requirements

A Dalamud plugin host. The project references `Dalamud.Bindings.ImGui.dll` from your local Dalamud dev folder and is intended to be consumed by a Dalamud plugin (which supplies that assembly at runtime).

## Usage

Reference the project — a git submodule plus a `<ProjectReference>` today, a NuGet package later — then:

```csharp
using ImGuiColorTextEditNet;
using System.Numerics;

readonly TextEditor _editor = new()
{
    AllText = "/say hi\n:if {hpp} < 50\n/ac Cure\n:endif",
    SyntaxHighlighter = new MyHighlighter(), // your ISyntaxHighlighter
};
_editor.Renderer.ShowLineNumbers = true;

// each frame, inside a window:
_editor.Render("###editor", new Vector2(-1, ImGui.GetContentRegionAvail().Y));
```

`Render` returns `true` on frames where the text changed. Read or replace the buffer with `_editor.AllText`.

## Custom syntax highlighting

Implement `ISyntaxHighlighter` and assign it to `editor.SyntaxHighlighter`. `Colorize` receives one line's glyphs and sets each glyph's palette index:

```csharp
public sealed class MyHighlighter : ISyntaxHighlighter
{
    public bool AutoIndentation => false;
    public int MaxLinesPerFrame => 1000;
    public string? GetTooltip(string id) => null;

    public object Colorize(Span<Glyph> line, object? state)
    {
        for (var i = 0; i < line.Length; i++)
            line[i] = new Glyph(line[i].Char, /* PaletteIndex for this token */ PaletteIndex.Default);

        return state;
    }
}
```

Token colors resolve through `PaletteIndex`. Override any slot with `editor.Renderer.SetColor(PaletteIndex.Keyword, 0xff_00_ff_ff)`, or replace the whole palette via `editor.Renderer.Palette`. Chrome colors (background, selection, cursor, line numbers) track the ImGui theme automatically.

## License

MIT — see [LICENSE](LICENSE). The license retains the original copyrights of ImGuiColorTextEditNet (csinkers) and ImGuiColorTextEdit (Balázs Jákó); this fork adds its own.
