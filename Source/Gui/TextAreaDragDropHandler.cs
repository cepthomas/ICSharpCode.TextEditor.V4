// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    public class TextAreaDragDropHandler
    {
        TextArea _textArea;

        public void Attach(TextArea textArea)
        {
            _textArea = textArea;
            _textArea.AllowDrop = true;

            _textArea.DragEnter += MakeDragEventHandler(OnDragEnter);
            _textArea.DragDrop += MakeDragEventHandler(OnDragDrop);
            _textArea.DragOver += MakeDragEventHandler(OnDragOver);
        }

        /// <summary>
        /// Create a drag'n'drop event handler.
        /// Windows Forms swallows unhandled exceptions during drag'n'drop, so we report them here.
        /// </summary>
        static DragEventHandler MakeDragEventHandler(DragEventHandler h)
        {
            return (sender, e) =>
            {
                try
                {
                    h(sender, e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            };
        }

        static DragDropEffects GetDragDropEffect(DragEventArgs e)
        {
            if ((e.AllowedEffect & DragDropEffects.Move) > 0 && (e.AllowedEffect & DragDropEffects.Copy) > 0)
            {
                return (e.KeyState & 8) > 0 ? DragDropEffects.Copy : DragDropEffects.Move;
            }
            else if ((e.AllowedEffect & DragDropEffects.Move) > 0)
            {
                return DragDropEffects.Move;
            }
            else if ((e.AllowedEffect & DragDropEffects.Copy) > 0)
            {
                return DragDropEffects.Copy;
            }
            return DragDropEffects.None;
        }

        protected void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                e.Effect = GetDragDropEffect(e);
            }
        }

        void InsertString(int offset, string str)
        {
            _textArea.Document.Insert(offset, str);
            _textArea.SelectionManager.SetSelection(_textArea.Document.OffsetToPosition(offset), _textArea.Document.OffsetToPosition(offset + str.Length), false);
            _textArea.Caret.Position = _textArea.Document.OffsetToPosition(offset + str.Length);
            _textArea.Refresh();
        }

        protected void OnDragDrop(object sender, DragEventArgs e)
        {
            Point p = _textArea.PointToClient(new Point(e.X, e.Y));

            if (e.Data.GetDataPresent(typeof(string)))
            {
                _textArea.BeginUpdate();
                _textArea.Document.UndoStack.StartUndoGroup();
                try
                {
                    int offset = _textArea.Caret.Offset;
                    if (_textArea.IsReadOnly(offset))
                    {
                        // prevent dragging text into readonly section
                        return;
                    }

                    //TODO1 drag/drop - all this:
                    //if (e.Data.GetDataPresent(typeof(Selection)))
                    //{
                    //    Selection sel = (Selection)e.Data.GetData(typeof(Selection));
                    //    if (sel.ContainsPosition(textArea.Caret.Position))
                    //    {
                    //        return;
                    //    }

                    //    if (GetDragDropEffect(e) == DragDropEffects.Move)
                    //    {
                    //        if (SelectionManager.SelectionIsReadOnly(textArea.Document, sel))
                    //        {
                    //            // prevent dragging text out of readonly section
                    //            return;
                    //        }

                    //        int len = sel.Length;
                    //        textArea.Document.Remove(sel.StartOffset, len);

                    //        if (sel.StartOffset < offset)
                    //        {
                    //            offset -= len;
                    //        }
                    //    }
                    //}

                    _textArea.SelectionManager.ClearSelection();
                    InsertString(offset, (string)e.Data.GetData(typeof(string)));
                    _textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
                }
                finally
                {
                    _textArea.Document.UndoStack.EndUndoGroup();
                    _textArea.EndUpdate();
                }
            }
        }

        protected void OnDragOver(object sender, DragEventArgs e)
        {
            if (!_textArea.Focused)
            {
                _textArea.Focus();
            }

            Point p = _textArea.PointToClient(new Point(e.X, e.Y));

            if (_textArea.TextView.DrawingPosition.Contains(p.X, p.Y))
            {
                TextLocation realmousepos= _textArea.TextView.GetLogicalPosition(p.X - _textArea.TextView.DrawingPosition.X,
                                           p.Y - _textArea.TextView.DrawingPosition.Y);
                int lineNr = Math.Min(_textArea.Document.TotalNumberOfLines - 1, Math.Max(0, realmousepos.Y));

                _textArea.Caret.Position = new TextLocation(realmousepos.X, lineNr);
                _textArea.SetDesiredColumn();

                if (e.Data.GetDataPresent(typeof(string)) && !_textArea.IsReadOnly(_textArea.Caret.Offset))
                {
                    e.Effect = GetDragDropEffect(e);
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
    }
}
