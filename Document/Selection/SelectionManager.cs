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
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor.Document
{
    /// <summary>Where selection initiated from.</summary>
    public enum SelSource { None = 0, Gutter = 1, TArea = 2 }

    /// <summary>This class manages the selection in a document.</summary>
    public class SelectionManager : IDisposable
    {
        Document _document;

        TextArea _textArea;

        bool _isRect;

        public event EventHandler SelectionChanged;

        public SelSource WhereFrom { get; set; }

        public TextLocation StartPosition { get; set; }

        public TextLocation EndPosition { get; set; }

        public int StartOffset { get { return _document.PositionToOffset(StartPosition); } }

        public int EndOffset { get { return _document.PositionToOffset(EndPosition); } }

        public int Length { get { return EndOffset - StartOffset; } }

        public bool IsEmpty { get { return StartPosition == EndPosition; } }

        public bool IsValid { get { return StartPosition.IsValid && EndPosition.IsValid; } }

        public string SelectedText //TODO1 test for IsRect - make a list
        {
            get
            {
                if (_document != null)
                {
                    return Length < 0 ? null : _document.GetText(StartOffset, Length);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <value>true if the <see cref="CurrentSelection"/> is not empty, false otherwise.</value>
        public bool HasSomethingSelected { get { return IsValid && !IsEmpty; } }

        //public bool SelectionIsReadonly
        //{
        //    get
        //    {
        //        if (_document.ReadOnly)
        //            return true;

        //        if (Shared.TEP.SupportReadOnlySegments)
        //            return _document.MarkerStrategy.GetMarkers(StartOffset, Length).Exists(m => m.IsReadOnly);

        //        return false;
        //    }
        //}

        /// <summary>Converts a <see cref="Selection"/> instance to string (for debug purposes)</summary>
        public override string ToString()
        {
            return string.Format("[Selection : StartPosition={0}, EndPosition={1}, IsRect={2}]", StartPosition, EndPosition, _isRect);
        }

        /// <summary>Creates a new instance of <see cref="SelectionManager"/></summary>
        public SelectionManager(Document document, TextArea textArea)
        {
            _document = document;
            _textArea = textArea;
            _isRect = false;
            StartPosition = new TextLocation();
            EndPosition = new TextLocation();
            document.DocumentChanged += DocumentChanged;
        }

        public bool ContainsPosition(TextLocation position)
        {
            if (IsEmpty)
                return false;

            return StartPosition.Y < position.Y && position.Y < EndPosition.Y ||
                   StartPosition.Y == position.Y && StartPosition.X <= position.X && (StartPosition.Y != EndPosition.Y || position.X <= EndPosition.X) ||
                   EndPosition.Y == position.Y && StartPosition.Y != EndPosition.Y && position.X <= EndPosition.X;
        }

        public bool ContainsOffset(int offset)
        {
            return StartPosition.IsValid && EndPosition.IsValid && StartOffset <= offset && offset <= EndOffset;
        }

        public void Dispose()
        {
            if (_document != null)
            {
                _document.DocumentChanged -= DocumentChanged;
                _document = null;
            }
        }

        void DocumentChanged(object sender, DocumentEventArgs e)
        {
            //if (e.Text == null)
            //{
            //    Remove(e.Offset, e.Length);
            //}
            //else
            //{
            //    if (e.Length < 0)
            //    {
            //        Insert(e.Offset, e.Text);
            //    }
            //    else
            //    {
            //        Replace(e.Offset, e.Length, e.Text);
            //    }
            //}
        }

        public void SetSelection(TextLocation startPosition, TextLocation endPosition, bool isRect)
        {
            ClearWithoutUpdate();

            // Make sure to clean out old stuff too.
            int ymin = Math.Min(StartPosition.Y, startPosition.Y);
            int ymax = Math.Max(StartPosition.Y, startPosition.Y);

            // Save new.
            _isRect = isRect;
            StartPosition = startPosition;
            EndPosition = endPosition;

            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, ymin, ymax));
            _document.CommitUpdate();

            OnSelectionChanged(EventArgs.Empty);
        }

        public void ExtendSelection(TextLocation newPosition, bool isRect)
        {
            // where oldposition is where the cursor was, and newposition is where it has ended up from a click (both zero based)
            if (WhereFrom == SelSource.Gutter)
            {
                // selection new position is always at the left edge for gutter selections
                newPosition.X = 0;
                StartPosition = newPosition;
                EndPosition = newPosition;
            }
            else // via text area
            {
                if (newPosition > StartPosition) // selecting forward
                {
                    EndPosition = newPosition;
                }
                else // selecting backwards
                {
                    StartPosition = newPosition;
                }
            }

            _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, StartPosition.Y, EndPosition.Y));
            _document.CommitUpdate();

            OnSelectionChanged(EventArgs.Empty);
        }

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

            StartPosition = new TextLocation();
            EndPosition = new TextLocation();
            _isRect = false;

            OnSelectionChanged(EventArgs.Empty);
        }

        /// <remarks>Clears the selection.</remarks>
        public void ClearSelection()
        {
            Point mousepos;
            mousepos = _textArea.MousePos;

            // this is the most logical place to reset selection starting positions because it is always called before a new selection
            TextLocation newSelectionStart = _textArea.GetLogicalPosition(mousepos.X - _textArea.DrawingPosition.X, mousepos.Y - _textArea.DrawingPosition.Y);

            if (WhereFrom == SelSource.Gutter)
            {
                newSelectionStart.X = 0;
            }

            if (newSelectionStart.Line >= _document.TotalNumberOfLines)
            {
                newSelectionStart.Line = _document.TotalNumberOfLines-1;
                newSelectionStart.Column = _document.GetLineSegment(_document.TotalNumberOfLines-1).Length;
            }

            StartPosition = newSelectionStart;

            _isRect = false;

            ClearWithoutUpdate();
            _document.CommitUpdate();
        }

        /// <remarks>Removes the selected text from the buffer and clears the selection.</remarks>
        public void RemoveSelectedText()
        {
            if (_document.ReadOnly)
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

        public ColumnRange GetSelectionAtLine(int lineNumber)
        {
            ColumnRange ret = null;

            //Debug.Write(string.Format("L:{0}  ", lineNumber));

            if (IsValid && !IsEmpty)
            {
                int startLine = Math.Min(StartPosition.Y, EndPosition.Y);
                int endLine = Math.Max(StartPosition.Y, EndPosition.Y);

                if (_isRect)
                {
                    if (startLine <= lineNumber && lineNumber <= endLine)
                    {
                        ret = new ColumnRange(StartPosition.X, EndPosition.X);
                    }
                }
                else
                {
                    if (ret == null && startLine < lineNumber && lineNumber < endLine)
                    {
                        ret = ColumnRange.WHOLE_COLUMN;
                    }

                    if (ret == null && startLine == lineNumber)
                    {
                        LineSegment line = _document.GetLineSegment(startLine);
                        int startColumn = StartPosition.X;
                        int endColumn = endLine == lineNumber ? EndPosition.X : line.Length + 1;
                        ret = new ColumnRange(startColumn, endColumn);
                    }

                    if (ret == null && endLine == lineNumber)
                    {
                        int endColumn = EndPosition.X;
                        ret = new ColumnRange(0, endColumn);
                    }
                }

                //Debug.Write(string.Format("L:{0}:{1}  ", lineNumber, ret));
            }

            return ret ?? ColumnRange.NO_COLUMN;
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
