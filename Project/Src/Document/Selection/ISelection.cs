// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// An interface representing a portion of the current selection.
    /// </summary>
    public interface ISelection
    {
        TextLocation StartPosition { get; set; }
        TextLocation EndPosition { get; set; }
        int Offset { get; }
        int EndOffset { get; }
        int Length { get; }
        bool IsRect { get; }
        bool IsEmpty { get; }
        string SelectedText { get; }

        bool ContainsOffset(int offset);
        bool ContainsPosition(TextLocation position);
    }
}
