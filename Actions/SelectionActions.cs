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
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretLeft : CaretLeft
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretUp : CaretUp
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftCaretDown : CaretDown
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftWordRight : WordRight
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftWordLeft : WordLeft
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftHome : Home
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftEnd : End
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMoveToStart : MoveToStart
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMoveToEnd : MoveToEnd
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMovePageUp : MovePageUp
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class ShiftMovePageDown : MovePageDown
    {
        public override void Execute(TextArea textArea)
        {
            base.Execute(textArea);
            textArea.AutoClearSelection = false;
            textArea.SelectionManager.ExtendSelection(textArea.Caret.Position, false);
        }
    }

    public class SelectWholeDocument : EditAction
    {
        public override void Execute(TextArea textArea)
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

    public class ClearSelection : EditAction
    {
        public override void Execute(TextArea textArea)
        {
            textArea.SelectionManager.ClearSelection();
        }
    }
}
