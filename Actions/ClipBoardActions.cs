// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Util;


namespace ICSharpCode.TextEditor.Actions
{
    public class Cut : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {
            if (!textArea.Document.ReadOnly)
            {
                //textArea.ClipboardHandler.Cut(null, null);

                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    Clipboard.SetText(textArea.SelectionManager.SelectedText);
                    if (!textArea.SelectionManager.SelectionIsReadonly)
                    {
                        // Remove text
                        textArea.BeginUpdate();
                        textArea.Caret.Position = textArea.SelectionManager.StartPosition;
                        textArea.SelectionManager.RemoveSelectedText();
                        textArea.EndUpdate();
                    }
                }
            }
        }
    }

    public class Copy : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {
            textArea.AutoClearSelection = false;
            if (textArea.SelectionManager.HasSomethingSelected)
            {
                Clipboard.SetText(textArea.SelectionManager.SelectedText);
            }
        }
    }

    public class Paste : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {
            if (!textArea.Document.ReadOnly)
            {
                if (textArea.EnableCutOrPaste)
                {
                    textArea.Document.UndoStack.StartUndoGroup();
                    string s = Clipboard.GetText();
                    if (textArea.SelectionManager.HasSomethingSelected)
                    {
                        textArea.Caret.Position = textArea.SelectionManager.StartPosition;
                        textArea.SelectionManager.RemoveSelectedText();
                        textArea.InsertString(s);
                    }
                    else
                    {
                        textArea.InsertString(s);
                    }
                    textArea.Document.UndoStack.EndUndoGroup();
                }
            }
        }
    }
}
