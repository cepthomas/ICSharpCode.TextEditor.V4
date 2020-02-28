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

namespace ICSharpCode.TextEditor
{
    public class TextAreaClipboardHandler
    {
        TextArea textArea;

        public bool EnableCut
        {
            get { return textArea.EnableCutOrPaste; } //textArea.SelectionManager.HasSomethingSelected; }
        }

        public bool EnableCopy
        {
            get { return true; } //textArea.SelectionManager.HasSomethingSelected;       }
        }

        public delegate bool ClipboardContainsTextDelegate();

        /// <summary>
        /// Is called when CachedClipboardContainsText should be updated.
        /// If this property is null (the default value), the text editor uses
        /// System.Windows.Forms.Clipboard.ContainsText.
        /// </summary>
        /// <remarks>
        /// This property is useful if you want to prevent the default Clipboard.ContainsText
        /// behaviour that waits for the clipboard to be available - the clipboard might
        /// never become available if it is owned by a process that is paused by the debugger.
        /// </remarks>
        public static ClipboardContainsTextDelegate GetClipboardContainsText;

        public bool EnablePaste
        {
            get
            {
                if (!textArea.EnableCutOrPaste)
                    return false;

                ClipboardContainsTextDelegate d = GetClipboardContainsText;
                if (d != null)
                {
                    return d();
                }
                else
                {
                    try
                    {
                        return Clipboard.ContainsText();
                    }
                    catch (ExternalException)
                    {
                        return false;
                    }
                }
            }
        }

        public bool EnableDelete
        {
            get { return textArea.SelectionManager.HasSomethingSelected && !textArea.SelectionManager.SelectionIsReadonly; }
        }

        public bool EnableSelectAll
        {
            get { return true; }
        }

        public TextAreaClipboardHandler(TextArea textArea)
        {
            this.textArea = textArea;
            textArea.SelectionManager.SelectionChanged += new EventHandler(DocumentSelectionChanged);
        }

        void DocumentSelectionChanged(object sender, EventArgs e)
        {
//			((DefaultWorkbench)WorkbenchSingleton.Workbench).UpdateToolbars();
        }

        bool CopyTextToClipboard(string stringToCopy)
        {
            bool ret = false;

            if (stringToCopy.Length > 0)
            {
                DataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.UnicodeText, true, stringToCopy);

                // Default has no highlighting, therefore we don't need RTF output
                if (textArea.Document.HighlightingStrategy.Name != "Default")
                {
                    dataObject.SetData(DataFormats.Rtf, RtfWriter.GenerateRtf(textArea));
                }

                OnCopyText(new CopyTextEventArgs(stringToCopy));

                SafeSetClipboard(dataObject);
                ret = true;
            }

            return ret;
        }

        // Code duplication: TextAreaClipboardHandler.cs also has SafeSetClipboard
        [ThreadStatic] static int SafeSetClipboardDataVersion;

        static void SafeSetClipboard(object dataObject)
        {
            // Work around ExternalException bug. (SD2-426)
            // Best reproducable inside Virtual PC.
            int version = unchecked(++SafeSetClipboardDataVersion);
            try
            {
                Clipboard.SetDataObject(dataObject, true);
            }
            catch (ExternalException)
            {
                Timer timer = new Timer();
                timer.Interval = 100;
                timer.Tick += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    if (SafeSetClipboardDataVersion == version)
                    {
                        try
                        {
                            Clipboard.SetDataObject(dataObject, true, 10, 50);
                        }
                        catch (ExternalException) { }
                    }
                };
                timer.Start();
            }
        }

        public void Cut(object sender, EventArgs e)
        {
            if (textArea.SelectionManager.HasSomethingSelected)
            {
                if (CopyTextToClipboard(textArea.SelectionManager.SelectedText))
                {
                    if (textArea.SelectionManager.SelectionIsReadonly)
                        return;

                    // Remove text
                    textArea.BeginUpdate();
                    textArea.Caret.Position = textArea.SelectionManager.StartPosition;
                    textArea.SelectionManager.RemoveSelectedText();
                    textArea.EndUpdate();
                }
            }
        }

        public void Copy(object sender, EventArgs e)
        {
            CopyTextToClipboard(textArea.SelectionManager.SelectedText);
        }

        public void Paste(object sender, EventArgs e)
        {
            if (!textArea.EnableCutOrPaste)
            {
                return;
            }

            // Clipboard.GetDataObject may throw an exception...
            for (int i = 0;; i++)
            {
                try
                {
                    IDataObject data = Clipboard.GetDataObject();
                    if (data == null)
                        return;

                    bool fullLine = data.GetDataPresent("MSDEVLineSelect"); // This is the type VS 2003 and 2005 use for flagging a whole line copy

                    if (data.GetDataPresent(DataFormats.UnicodeText))
                    {
                        string text = (string)data.GetData(DataFormats.UnicodeText);
                        // we got NullReferenceExceptions here, apparently the clipboard can contain null strings
                        if (!string.IsNullOrEmpty(text))
                        {
                            textArea.Document.UndoStack.StartUndoGroup();
                            try
                            {
                                if (textArea.SelectionManager.HasSomethingSelected)
                                {
                                    textArea.Caret.Position = textArea.SelectionManager.StartPosition;
                                    textArea.SelectionManager.RemoveSelectedText();
                                }

                                if (fullLine)
                                {
                                    int col = textArea.Caret.Column;
                                    textArea.Caret.Column = 0;
                                    if (!textArea.IsReadOnly(textArea.Caret.Offset))
                                        textArea.InsertString(text);
                                    textArea.Caret.Column = col;
                                }
                                else
                                {
                                    // textArea.EnableCutOrPaste already checked readonly for this case
                                    textArea.InsertString(text);
                                }
                            }
                            finally
                            {
                                textArea.Document.UndoStack.EndUndoGroup();
                            }
                        }
                    }
                    return;
                }
                catch (ExternalException)
                {
                    // GetDataObject does not provide RetryTimes parameter
                    if (i > 5) throw;
                }
            }
        }

        public void Delete(object sender, EventArgs e)
        {
            new ICSharpCode.TextEditor.Actions.Delete().Execute(textArea);
        }

        public void SelectAll(object sender, EventArgs e)
        {
            new ICSharpCode.TextEditor.Actions.SelectWholeDocument().Execute(textArea);
        }

        protected virtual void OnCopyText(CopyTextEventArgs e)
        {
            if (CopyText != null)
            {
                CopyText(this, e);
            }
        }

        public event CopyTextEventHandler CopyText;
    }

    public delegate void CopyTextEventHandler(object sender, CopyTextEventArgs e);
    public class CopyTextEventArgs : EventArgs
    {
        string text;

        public string Text
        {
            get
            {
                return text;
            }
        }

        public CopyTextEventArgs(string text)
        {
            this.text = text;
        }
    }
}
