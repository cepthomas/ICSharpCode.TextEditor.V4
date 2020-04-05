// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using ICSharpCode.TextEditor.Document;


namespace ICSharpCode.TextEditor.Actions
{
    public class ShiftCaretRight : CaretRight
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretLeft : CaretLeft
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretUp : CaretUp
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretDown : CaretDown
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftWordRight : WordRight
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftWordLeft : WordLeft
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftHome : Home
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftEnd : End
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMoveToStart : MoveToStart
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMoveToEnd : MoveToEnd
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMovePageUp : MovePageUp
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMovePageDown : MovePageDown
    {
        public void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class SelectWholeDocument : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {
            textArea.AutoClearSelection = false;
            TextLocation endPoint = textArea.Document.OffsetToPosition(textArea.Document.TextLength);
            textArea.SelectionManager.SetSelection(new TextLocation(0, 0), endPoint, false);
            textArea.Caret.Position = textArea.SelectionManager.NextValidPosition(endPoint.Y);
            // after a SelectWholeDocument selection, the caret is placed correctly,
            // but it is not positioned internally.  The effect is when the cursor
            // is moved up or down a line, the caret will take on the column that
            // it was in before the SelectWholeDocument
            textArea.SetDesiredColumn();
        }
    }

    public class ClearSelection : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {
            textArea.SelectionManager.ClearSelection();
        }
    }
}
