namespace ImGuiColorTextEditNet.Operations;

internal interface IEditorOperation
{
    void Apply(TextEditor editor);
    void Undo(TextEditor editor);
}
