// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Diagnostics;

namespace ICSharpCode.TextEditor.Document
{
    // selection initiated from...
    internal class SelectFrom
    {
        public int where = WhereFrom.None; // last selection initiator
        public int first = WhereFrom.None; // first selection initiator
    }

    // selection initiated from type...
    internal class WhereFrom
    {
        public const int None = 0;
        public const int Gutter = 1;
        public const int TArea = 2;
    }

    /// <summary>
    /// This class manages the selections in a document.
    /// </summary>
    public class SelectionManager : IDisposable
    {
        TextLocation _selectionStart;
        IDocument _document;
        TextArea _textArea;

        internal SelectFrom selectFrom = new SelectFrom();
        public event EventHandler SelectionChanged;

        internal TextLocation SelectionStart
        {
            get { return _selectionStart; }
            set { DefaultDocument.ValidatePosition(_document, value); _selectionStart = value; }
        }

        /// <value>A collection containing all selections.</value>
        public Selection CurrentSelection { get; private set; }

        /// <value>true if the <see cref="CurrentSelection"/> is not empty, false otherwise.</value>
        public bool HasSomethingSelected { get { return CurrentSelection.IsValid && !CurrentSelection.IsEmpty; } }

        public bool SelectionIsReadonly
        {
            get
            {
                if (_document.ReadOnly)
                    return true;
                if (SelectionIsReadOnly(_document, CurrentSelection))
                    return true;
                return false;
            }
        }

        internal static bool SelectionIsReadOnly(IDocument document, Selection sel)
        {
            if (document.TextEditorProperties.SupportReadOnlySegments)
                return document.MarkerStrategy.GetMarkers(sel.Offset, sel.Length).Exists(m=>m.IsReadOnly);
            else
                return false;
        }

        /// <value>The text that is currently selected.</value>
        public string SelectedText
        {
            get { return CurrentSelection.SelectedText; }
        }

        /// <summary>Creates a new instance of <see cref="SelectionManager"/></summary>
        public SelectionManager(IDocument document, TextArea textArea)
        {
            _document = document;
            _textArea = textArea;
            CurrentSelection = new Selection(document);

            document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
        }

        public void Dispose()
        {
            if (this._document != null)
            {
                _document.DocumentChanged -= new DocumentEventHandler(DocumentChanged);
                this._document = null;
            }
        }

        void DocumentChanged(object sender, DocumentEventArgs e)
        {
            if (e.Text == null)
            {
                Remove(e.Offset, e.Length);
            }
            else
            {
                if (e.Length < 0)
                {
                    Insert(e.Offset, e.Text);
                }
                else
                {
                    Replace(e.Offset, e.Length, e.Text);
                }
            }
        }

        /// <remarks>Clears the selection and sets a new selection using the given <see cref="Selection"/> object.</remarks>
        public void SetSelection(Selection selection)
        {
            if (selection != null)
            {
                ClearWithoutUpdate();
                CurrentSelection = selection;
                _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
                _document.CommitUpdate();
                OnSelectionChanged(EventArgs.Empty);
            }
            else
            {
                ClearSelection();
            }
        }

        public void SetSelection(TextLocation startPosition, TextLocation endPosition, bool isRect)
        {
            SetSelection(new Selection(_document, startPosition, endPosition, isRect));
        }

        public bool GreaterEqPos(TextLocation p1, TextLocation p2)
        {
            return p1.Y > p2.Y || p1.Y == p2.Y && p1.X >= p2.X;
        }

        public void ExtendSelection(TextLocation oldPosition, TextLocation newPosition, bool isRect)
        {
            // where oldposition is where the cursor was,
            // and newposition is where it has ended up from a click (both zero based)

            if (oldPosition == newPosition)
            {
                return;
            }

            TextLocation min;
            TextLocation max;
            int oldnewX = newPosition.X;
            bool oldIsGreater = GreaterEqPos(oldPosition, newPosition);

            if (oldIsGreater)
            {
                min = newPosition;
                max = oldPosition;
            }
            else
            {
                min = oldPosition;
                max = newPosition;
            }

            if (min == max)
            {
                return;
            }

            if (!HasSomethingSelected)
            {
                SetSelection(new Selection(_document, min, max, isRect));
                // initialise selectFrom for a cursor selection
                if (selectFrom.where == WhereFrom.None)
                    SelectionStart = oldPosition; //textArea.Caret.Position;
                return;
            }

            Selection selection = CurrentSelection;

            if (min == max)
            {
                //selection.StartPosition = newPosition;
                return;
            }
            else
            {
                // changed selection via gutter
                if (selectFrom.where == WhereFrom.Gutter)
                {
                    // selection new position is always at the left edge for gutter selections
                    newPosition.X = 0;
                }

                if (GreaterEqPos(newPosition, SelectionStart)) // selecting forward
                {
                    selection.StartPosition = SelectionStart;
                    // this handles last line selection
                    if (selectFrom.where == WhereFrom.Gutter) //&& newPosition.Y != oldPosition.Y)
                    {
                        selection.EndPosition = new TextLocation(_textArea.Caret.Column, _textArea.Caret.Line);
                    }
                    else
                    {
                        newPosition.X = oldnewX;
                        selection.EndPosition = newPosition;
                    }
                }
                else // selecting back
                {
                    if (selectFrom.where == WhereFrom.Gutter && selectFrom.first == WhereFrom.Gutter)
                    {
                        // gutter selection
                        selection.EndPosition = NextValidPosition(SelectionStart.Y);
                    }
                    else
                    {
                        // internal text selection
                        selection.EndPosition = SelectionStart; //selection.StartPosition;
                    }
                    selection.StartPosition = newPosition;
                }
            }

            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, min.Y, max.Y));
            _document.CommitUpdate();
            OnSelectionChanged(EventArgs.Empty);
        }

        // retrieve the next available line
        // - checks that there are more lines available after the current one
        // - if there are then the next line is returned
        // - if there are NOT then the last position on the given line is returned
        public TextLocation NextValidPosition(int line)
        {
            if (line < _document.TotalNumberOfLines - 1)
                return new TextLocation(0, line + 1);
            else
                return new TextLocation(_document.GetLineSegment(_document.TotalNumberOfLines - 1).Length + 1, line);
        }

        void ClearWithoutUpdate()
        {
            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, CurrentSelection.StartPosition.Y, CurrentSelection.EndPosition.Y));
            CurrentSelection = new Selection(_document);
            OnSelectionChanged(EventArgs.Empty);
        }

        /// <remarks>Clears the selection.</remarks>
        public void ClearSelection()
        {
            Point mousepos;
            mousepos = _textArea.mousepos;
            // this is the most logical place to reset selection starting positions because it is always called before a new selection
            selectFrom.first = selectFrom.where;
            TextLocation newSelectionStart = _textArea.TextView.GetLogicalPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);

            if (selectFrom.where == WhereFrom.Gutter)
            {
                newSelectionStart.X = 0;
//				selectionStart.Y = -1;
            }

            if (newSelectionStart.Line >= _document.TotalNumberOfLines)
            {
                newSelectionStart.Line = _document.TotalNumberOfLines-1;
                newSelectionStart.Column = _document.GetLineSegment(_document.TotalNumberOfLines-1).Length;
            }

            this.SelectionStart = newSelectionStart;

            ClearWithoutUpdate();
            _document.CommitUpdate();
        }

        /// <remarks>Removes the selected text from the buffer and clears the selection.</remarks>
        public void RemoveSelectedText()
        {
            if (SelectionIsReadonly)
            {
                ClearSelection();
                return;
            }

            List<int> lines = new List<int>();
            int offset = -1;
            bool oneLine = true;

            Selection selection = _textArea.SelectionManager.CurrentSelection;
            if (oneLine)
            {
                int lineBegin = selection.StartPosition.Y;
                if (lineBegin != selection.EndPosition.Y)
                {
                    oneLine = false;
                }
                else
                {
                    lines.Add(lineBegin);
                }
            }
            offset = selection.Offset;
            _document.Remove(selection.Offset, selection.Length);

            ClearSelection();

            if (offset >= 0)
            {
//				document.Caret.Offset = offset; // original
            }

            if (offset != -1)
            {
                if (oneLine)
                {
                    foreach (int i in lines)
                    {
                        _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, i));
                    }
                }
                else
                {
                    _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
                }
                _document.CommitUpdate();
            }
        }

        bool SelectionsOverlap(Selection s1, Selection s2)
        {
            return (s1.Offset <= s2.Offset && s2.Offset <= s1.Offset + s1.Length)                         ||
                   (s1.Offset <= s2.Offset + s2.Length && s2.Offset + s2.Length <= s1.Offset + s1.Length) ||
                   (s1.Offset >= s2.Offset && s1.Offset + s1.Length <= s2.Offset + s2.Length);
        }

        /// <remarks>Returns true if the given offset points to a section which is selected.</remarks>
        public bool IsSelected(int offset)
        {
            bool ret = CurrentSelection.IsValid;
            if(ret)
                ret = GetSelectionAt(offset) != null;
            return ret;
        }

        /// <remarks>Returns a <see cref="Selection"/> object giving the selection in which the offset points to.</remarks>
        /// <returns>null if the offset doesn't point to a selection</returns>
        public Selection GetSelectionAt(int offset)
        {
            if (CurrentSelection.ContainsOffset(offset))
            {
                return CurrentSelection;
            }
            return null;
        }

        /// <remarks>Used internally, do not call.</remarks>
        internal void Insert(int offset, string text)
        {
//			foreach (Selection selection in SelectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset += text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length += text.Length;
//				}
//			}
        }

        /// <remarks>Used internally, do not call.</remarks>
        internal void Remove(int offset, int length)
        {
//			foreach (Selection selection in SelectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset -= length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length -= length;
//				}
//			}
        }

        /// <remarks>Used internally, do not call.</remarks>
        internal void Replace(int offset, int length, string text)
        {
//			foreach (Selection selection in SelectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset = selection.Offset - length + text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length = selection.Length - length + text.Length;
//				}
//			}
        }

        public ColumnRange GetSelectionAtLine(int lineNumber)
        {
            Selection sel = _textArea.SelectionManager.CurrentSelection;

            int startLine = sel.StartPosition.Y;
            int endLine = sel.EndPosition.Y;

            if (sel.IsRect)
            {
                return new ColumnRange(sel.StartPosition.X, sel.EndPosition.X);
            }

            if (startLine < lineNumber && lineNumber < endLine)
            {
                return ColumnRange.WholeColumn;
            }

            if (startLine == lineNumber)
            {
                LineSegment line = _document.GetLineSegment(startLine);
                int startColumn = sel.StartPosition.X;
                int endColumn = endLine == lineNumber ? sel.EndPosition.X : line.Length + 1;
                return new ColumnRange(startColumn, endColumn);
            }

            if (endLine == lineNumber)
            {
                int endColumn = sel.EndPosition.X;
                return new ColumnRange(0, endColumn);
            }

            return ColumnRange.NoColumn;
        }

        protected virtual void OnSelectionChanged(EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, e);
            }
        }
    }
}
