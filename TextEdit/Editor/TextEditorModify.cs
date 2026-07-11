using System;
using System.Text;
using ImGuiColorTextEditNet.Operations;
using Dalamud.Bindings.ImGui;

namespace ImGuiColorTextEditNet.Editor;

/// <summary>Provides methods for modifying text, e.g. copy, cut, paste, delete, and character entry.</summary>
public static class TextEditorModify
{
    static readonly SimpleCache<char, string> CharLabelCache = new(
        "char strings",
        x => x.ToString()
    );

    /// <summary>Copies the currently selected text to the clipboard.</summary>
    public static void Copy(TextEditor e)
    {
        if (e.Selection.HasSelection)
        {
            ImGui.SetClipboardText(e.Selection.GetSelectedText());
            return;
        }

        if (e.Text.LineCount == 0)
            return;

        StringBuilder sb = new();

        var line = e.Text.GetLine(e.Selection.GetActualCursorCoordinates().Line);
        foreach (var g in line)
            sb.Append(g.Char);

        ImGui.SetClipboardText(sb.ToString());
    }

    /// <summary>Cuts the currently selected text, copying it to the clipboard and removing it from the editor.</summary>
    public static void Cut(TextEditor e)
    {
        if (e.Options.IsReadOnly)
        {
            Copy(e);
            return;
        }

        if (!e.Selection.HasSelection)
            return;

        Copy(e);
        UndoRecord undo = DeleteSelection(e);
        e.UndoStack.AddUndo(undo);
    }

    /// <summary>Pastes text from the clipboard into the editor at the current cursor position or replaces the selection if any exists.</summary>
    public static void Paste(TextEditor e)
    {
        var clipText = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(clipText))
            return;

        ReplaceSelection(e, clipText);
    }

    /// <summary>Replaces the currently selected text with the specified text, or inserts the text at the cursor position if no selection exists.</summary>
    public static void ReplaceSelection(TextEditor e, string text)
    {
        Util.Assert(!e.Options.IsReadOnly);

        UndoRecord u = e.Selection.HasSelection
            ? DeleteSelection(e)
            : new() { Before = e.Selection.State };

        var pos = e.Selection.GetActualCursorCoordinates();
        u.Added = text;
        u.AddedStart = pos;
        u.AddedEnd = pos;

        if (!string.IsNullOrEmpty(text))
        {
            var start = pos < e.Selection.Start ? pos : e.Selection.Start;

            u.AddedEnd = e.Text.InsertTextAt(pos, text);

            e.Selection.Select(pos, pos);
            e.Selection.Cursor = pos;
            e.Color.InvalidateColor(start.Line - 1, u.AddedEnd.Line - start.Line + 1);
        }

        u.After = e.Selection.State;
        e.UndoStack.AddUndo(u);
    }

    /// <summary>Deletes the currently selected text or the character at the cursor position if no selection exists.</summary>
    public static void Delete(TextEditor e)
    {
        if (e.Options.IsReadOnly)
            return;

        if (e.Text.LineCount == 0)
            return;

        if (e.Selection.HasSelection)
        {
            UndoRecord u = DeleteSelection(e);
            e.UndoStack.AddUndo(u);
        }
        else
        {
            var pos = e.Selection.GetActualCursorCoordinates();
            e.Selection.Cursor = pos;

            UndoRecord u = new() { Before = e.Selection.State };

            if (pos.Column == e.Text.GetLineMaxColumn(pos.Line))
            {
                if (pos.Line == e.Text.LineCount - 1)
                    return;

                u.Removed = "\n";
                u.RemovedStart = u.RemovedEnd = e.Selection.GetActualCursorCoordinates();
                e.Text.Advance(u.RemovedEnd);
                e.Text.AppendToLine(pos.Line, e.Text.GetLineText(pos.Line + 1));
                e.Text.RemoveLine(pos.Line + 1);
            }
            else
            {
                var cindex = e.Text.GetCharacterIndex(pos);
                u.RemovedStart = u.RemovedEnd = e.Selection.GetActualCursorCoordinates();
                u.RemovedEnd.Column++;
                u.Removed = e.Text.GetText(u.RemovedStart, u.RemovedEnd);

                e.Text.RemoveInLine(pos.Line, cindex, cindex + 1);
            }

            u.After = e.Selection.State;
            e.Color.InvalidateColor(pos.Line, 1);
            e.UndoStack.AddUndo(u);
        }
    }

    /// <summary>Inserts a character at the current cursor position or replaces the selection if any exists.</summary>
    public static void EnterCharacter(TextEditor e, char c)
    {
        Util.Assert(!e.Options.IsReadOnly);
        UndoRecord u = e.Selection.HasSelection
            ? DeleteSelection(e)
            : new() { Before = e.Selection.State };

        var coord = e.Selection.GetActualCursorCoordinates();
        u.AddedStart = coord;

        Util.Assert(e.Text.LineCount != 0);

        if (c == '\n')
        {
            var line = e.Text.GetLine(coord.Line);
            var newLine = new Line();

            if (e.Color.SyntaxHighlighter.AutoIndentation)
            {
                for (
                    int it = 0;
                    it < line.Length
                        && char.IsAscii(line[it].Char)
                        && TextEditorText.IsBlank(line[it].Char);
                    ++it
                )
                {
                    newLine.Glyphs.Add(line[it]);
                }
            }

            int whitespaceSize = newLine.Glyphs.Count;
            var cindex = e.Text.GetCharacterIndex(coord);
            foreach (var glyph in line[cindex..])
                newLine.Glyphs.Add(glyph);

            e.Text.InsertLine(coord.Line + 1, newLine);
            e.Text.RemoveInLine(coord.Line, cindex, line.Length);
            e.Selection.Cursor = (
                coord.Line + 1,
                e.Text.GetCharacterColumn(coord.Line + 1, whitespaceSize)
            );

            u.Added = "\n";
        }
        else
        {
            var cindex = e.Text.GetCharacterIndex(coord);

            e.Text.InsertCharAt(coord, c);
            u.Added = CharLabelCache.Get(c);

            e.Selection.Cursor = (coord.Line, e.Text.GetCharacterColumn(coord.Line, cindex + 1));
        }

        u.AddedEnd = e.Selection.GetActualCursorCoordinates();
        u.After = e.Selection.State;

        e.UndoStack.AddUndo(u);

        e.Color.InvalidateColor(coord.Line - 1, 3);
        e.Text.PendingScrollRequest = coord.Line;
    }

    /// <summary>Deletes the character before the cursor position or the selected text if any exists.</summary>
    public static void Backspace(TextEditor e)
    {
        Util.Assert(!e.Options.IsReadOnly);

        if (e.Text.LineCount == 0)
            return;

        if (e.Selection.HasSelection)
        {
            UndoRecord undo = DeleteSelection(e);
            e.UndoStack.AddUndo(undo);
            return;
        }

        MetaOperation u = new() { Before = e.Selection.State };
        var pos = e.Selection.GetActualCursorCoordinates();
        e.Selection.Cursor = pos;

        if (e.Selection.Cursor.Column == 0)
        {
            if (e.Selection.Cursor.Line == 0)
                return;

            int lineNum = e.Selection.Cursor.Line;
            var lineText = e.Text.GetLineText(lineNum);
            var prevSize = e.Text.GetLineMaxColumn(lineNum - 1);

            u.Add(
                new ModifyLineOperation
                {
                    Line = lineNum - 1,
                    Added = lineText,
                    AddedColumn = prevSize,
                }
            );

            u.Add(new RemoveLineOperation { Line = lineNum, Removed = lineText });
            u.After.Cursor = (e.Selection.Cursor.Line - 1, prevSize);
        }
        else
        {
            var charIndex = e.Text.GetCharacterIndex(pos);
            var removeCount = 1;

            var lineText = e.Text.GetLineText(pos.Line);
            if (charIndex > 0 && lineText.AsSpan(0, charIndex).IndexOfAnyExcept(' ') < 0)
            {
                var tab = e.Options.TabSize;
                var prevStop = (pos.Column - 1) / tab * tab;
                removeCount = pos.Column - prevStop;
            }

            var cindex = charIndex - removeCount;
            var removed = e.Text.GetText(pos - (0, removeCount), pos);

            u.Add(
                new ModifyLineOperation
                {
                    Line = e.Selection.Cursor.Line,
                    RemovedColumn = cindex,
                    Removed = removed,
                }
            );

            u.After.Cursor = (e.Selection.Cursor.Line, e.Selection.Cursor.Column - removeCount);
        }

        u.After.Start = u.After.End = u.After.Cursor;
        u.Apply(e);

        e.Text.PendingScrollRequest = e.Selection.Cursor.Line;
        e.UndoStack.AddUndo(u);
    }

    static UndoRecord DeleteSelection(TextEditor e)
    {
        Util.Assert(e.Selection.End >= e.Selection.Start);

        UndoRecord undo = new()
        {
            Before = e.Selection.State,
            Removed = e.Selection.GetSelectedText(),
            RemovedStart = e.Selection.Start,
            RemovedEnd = e.Selection.End,
        };

        if (e.Selection.End != e.Selection.Start)
        {
            e.Text.DeleteRange(e.Selection.Start, e.Selection.End);

            e.Selection.Select(e.Selection.Start, e.Selection.Start);
            e.Selection.Cursor = e.Selection.Start;
            e.Color.InvalidateColor(e.Selection.Start.Line, 1);
        }

        undo.After = e.Selection.State;
        return undo;
    }
}
