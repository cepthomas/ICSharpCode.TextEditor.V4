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

    /// <summary>This class manages the selection in a document.</summary>
    public class SelectionManager : IDisposable
    {
        //////////////////////// from old Selection //////////////////////////////
        TextLocation _startPosition;
        TextLocation _endPosition;

        public TextLocation StartPosition
        {
            get { return _startPosition; }
            set { DefaultDocument.ValidatePosition(_document, value); _startPosition = value; }
        }

        public TextLocation EndPosition
        {
            get { return _endPosition; }
            set { DefaultDocument.ValidatePosition(_document, value); _endPosition = value; }
        }

        public int StartOffset
        {
            get { return _document.PositionToOffset(_startPosition); }
        }

        public int EndOffset
        {
            get { return _document.PositionToOffset(_endPosition); }
        }

        public int Length
        {
            get { return EndOffset - StartOffset; }
        }

        public bool IsEmpty
        {
            get { return _startPosition == _endPosition; }
        }

        public bool IsValid
        {
            get { return _startPosition.IsValid && _endPosition.IsValid; }
        }

        public bool IsRect { get; set; }

        public string SelectedText
        {
            get
            {
                if (_document != null)
                {
                    if (Length < 0)
                    {
                        return null;
                    }
                    return _document.GetText(StartOffset, Length);
                }
                return null;
            }
        }

        public bool ContainsPosition(TextLocation position)
        {
            if (this.IsEmpty)
                return false;

            return _startPosition.Y < position.Y && position.Y  < _endPosition.Y ||
                   _startPosition.Y == position.Y && _startPosition.X <= position.X && (_startPosition.Y != _endPosition.Y || position.X <= _endPosition.X) ||
                   _endPosition.Y == position.Y && _startPosition.Y != _endPosition.Y && position.X <= _endPosition.X;
        }

        public bool ContainsOffset(int offset)
        {
            return _startPosition.IsValid && _endPosition.IsValid && StartOffset <= offset && offset <= EndOffset;
        }

        /////////////////////////////end////////////////////////////////////////

        TextLocation _selectionStart;
        IDocument _document;
        TextArea _textArea;

        public event EventHandler SelectionChanged;


        internal SelectFrom SelectFrom = new SelectFrom();

        internal TextLocation SelectionStart
        {
            get { return _selectionStart; }
            set { DefaultDocument.ValidatePosition(_document, value); _selectionStart = value; }
        }

        ///// <value>A collection containing all selections.</value>
        //public Selection CurrentSelection { get; private set; }

        /// <value>true if the <see cref="CurrentSelection"/> is not empty, false otherwise.</value>
        public bool HasSomethingSelected { get { return this.IsValid && !this.IsEmpty; } }

        public bool SelectionIsReadonly
        {
            get
            {
                if (_document.ReadOnly)
                    return true;
                if (_document.TextEditorProperties.SupportReadOnlySegments)
                    return _document.MarkerStrategy.GetMarkers(StartOffset, Length).Exists(m => m.IsReadOnly);
                return false;
            }
        }

        /// <summary>Converts a <see cref="Selection"/> instance to string (for debug purposes)</summary>
        public override string ToString()
        {
            return String.Format("[Selection : StartPosition={0}, EndPosition={1}, IsRect={2}]", _startPosition, _endPosition, IsRect);
        }



        //internal static bool SelectionIsReadOnly(IDocument document, SelectionManager sel)
        //{
        //    if (document.TextEditorProperties.SupportReadOnlySegments)
        //        return document.MarkerStrategy.GetMarkers(sel.StartOffset, sel.Length).Exists(m => m.IsReadOnly);
        //    else
        //        return false;
        //}

        ///// <value>The text that is currently selected.</value>
        //public string SelectedText
        //{
        //    get { return CurrentSelection.SelectedText; }
        //}

        /// <summary>Creates a new instance of <see cref="SelectionManager"/></summary>
        public SelectionManager(IDocument document, TextArea textArea)
        {
            _document = document;
            _textArea = textArea;
//            CurrentSelection = new Selection(document);
            _startPosition = new TextLocation();
            _endPosition = new TextLocation();
            IsRect = false;

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

        ///// <remarks>Clears the selection and sets a new selection using the given <see cref="Selection"/> object.</remarks>
        //public void SetSelection(Selection selection)
        //{
        //    if (selection != null)
        //    {
        //        ClearWithoutUpdate();
        //        CurrentSelection = selection;
        //        _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
        //        _document.CommitUpdate();
        //        OnSelectionChanged(EventArgs.Empty);
        //    }
        //    else
        //    {
        //        ClearSelection();
        //    }
        //}

        public void SetSelection(IDocument document, TextLocation startPosition, TextLocation endPosition, bool isRect)
        {
            DefaultDocument.ValidatePosition(document, startPosition);
            DefaultDocument.ValidatePosition(document, endPosition);
            Debug.Assert(startPosition <= endPosition);
            _document = document;
            _startPosition = startPosition;
            _endPosition = endPosition;
            IsRect = isRect;
        }

        public void SetSelection(TextLocation startPosition, TextLocation endPosition, bool isRect)
        {
            //            SetSelection(new Selection(_document, startPosition, endPosition, isRect));
            ClearWithoutUpdate();
            //                CurrentSelection = selection;
            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, StartPosition.Y, EndPosition.Y));
            _document.CommitUpdate();
            OnSelectionChanged(EventArgs.Empty);
        }

        public bool GreaterEqPos(TextLocation p1, TextLocation p2)
        {
            return p1.Y > p2.Y || p1.Y == p2.Y && p1.X >= p2.X;
        }

        public void ExtendSelection(TextLocation oldPosition, TextLocation newPosition, bool isRect)
        {
            // where oldposition is where the cursor was, and newposition is where it has ended up from a click (both zero based)
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
                SetSelection(_document, min, max, isRect);
                // initialise selectFrom for a cursor selection
                if (SelectFrom.where == WhereFrom.None)
                    SelectionStart = oldPosition; //textArea.Caret.Position;
                return;
            }

            //Selection selection = CurrentSelection;

            if (min == max)
            {
                //selection.StartPosition = newPosition;
                return;
            }
            else
            {
                // changed selection via gutter
                if (SelectFrom.where == WhereFrom.Gutter)
                {
                    // selection new position is always at the left edge for gutter selections
                    newPosition.X = 0;
                }

                if (GreaterEqPos(newPosition, SelectionStart)) // selecting forward
                {
                    StartPosition = SelectionStart;
                    // this handles last line selection
                    if (SelectFrom.where == WhereFrom.Gutter) //&& newPosition.Y != oldPosition.Y)
                    {
                        EndPosition = new TextLocation(_textArea.Caret.Column, _textArea.Caret.Line);
                    }
                    else
                    {
                        newPosition.X = oldnewX;
                        EndPosition = newPosition;
                    }
                }
                else // selecting back
                {
                    if (SelectFrom.where == WhereFrom.Gutter && SelectFrom.first == WhereFrom.Gutter)
                    {
                        // gutter selection
                        EndPosition = NextValidPosition(SelectionStart.Y);
                    }
                    else
                    {
                        // internal text selection
                        EndPosition = SelectionStart; //selection.StartPosition;
                    }
                    StartPosition = newPosition;
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
            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, StartPosition.Y, EndPosition.Y));

            _startPosition = new TextLocation();
            _endPosition = new TextLocation();
            IsRect = false;

            OnSelectionChanged(EventArgs.Empty);
        }

        /// <remarks>Clears the selection.</remarks>
        public void ClearSelection()
        {
            Point mousepos;
            mousepos = _textArea.mousepos;
            // this is the most logical place to reset selection starting positions because it is always called before a new selection
            SelectFrom.first = SelectFrom.where;
            TextLocation newSelectionStart = _textArea.TextView.GetLogicalPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);

            if (SelectFrom.where == WhereFrom.Gutter)
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

            if (oneLine)
            {
                int lineBegin = StartPosition.Y;
                if (lineBegin != EndPosition.Y)
                {
                    oneLine = false;
                }
                else
                {
                    lines.Add(lineBegin);
                }
            }
            offset = StartOffset;
            _document.Remove(StartOffset, Length);

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

        //bool SelectionsOverlap(Selection s1, Selection s2)
        //{
        //    return (s1.StartOffset <= s2.StartOffset && s2.StartOffset <= s1.StartOffset + s1.Length)                         ||
        //           (s1.StartOffset <= s2.StartOffset + s2.Length && s2.StartOffset + s2.Length <= s1.StartOffset + s1.Length) ||
        //           (s1.StartOffset >= s2.StartOffset && s1.StartOffset + s1.Length <= s2.StartOffset + s2.Length);
        //}

        /// <remarks>Returns true if the given offset points to a section which is selected.</remarks>
        public bool IsSelected(int offset)
        {
            bool ret = IsValid;
            if (ret)
            {
                ret = ContainsOffset(offset);
                //ret = GetSelectionAt(offset) != null;
            }

            return ret;
        }

        ///// <remarks>Returns a <see cref="Selection"/> object giving the selection in which the offset points to.</remarks>
        ///// <returns>null if the offset doesn't point to a selection</returns>
        //public Selection GetSelectionAt(int offset)
        //{
        //    if (ContainsOffset(offset))
        //    {
        //        return CurrentSelection;
        //    }
        //    return null;
        //}

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
            //Selection sel = _textArea.SelectionManager.CurrentSelection;

            //Debug.WriteLine("ZZZ{0}", sel);

            if (IsValid)
            {
                //Debug.WriteLine("ZZZ{0}", lineNumber);

                int startLine = StartPosition.Y;
                int endLine = EndPosition.Y;

                if (IsRect)
                {
                    if (startLine < lineNumber && lineNumber < endLine)
                        return new ColumnRange(StartPosition.X, EndPosition.X);
                    else
                        return ColumnRange.NoColumn;
                }

                if (startLine < lineNumber && lineNumber < endLine)
                {
                    return ColumnRange.WholeColumn;
                }

                if (startLine == lineNumber)
                {
                    LineSegment line = _document.GetLineSegment(startLine);
                    int startColumn = StartPosition.X;
                    int endColumn = endLine == lineNumber ? EndPosition.X : line.Length + 1;
                    return new ColumnRange(startColumn, endColumn);
                }

                if (endLine == lineNumber)
                {
                    int endColumn = EndPosition.X;
                    return new ColumnRange(0, endColumn);
                }
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
