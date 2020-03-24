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
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    /// <summary>This class handles all mouse stuff for a textArea.</summary>
    public class TextAreaMouseHandler
    {
        TextArea _textArea;
        TextLocation _minSelection;
        TextLocation _maxSelection;
        bool _doubleclick = false;
        bool _clickedOnSelectedText = false; //TODO0 this is still a bit messed up.
        MouseButtons _button;
        static readonly Point NIL_POINT = new Point(-1, -1);
        Point _mousedownpos = NIL_POINT;
        Point _lastmousedownpos = NIL_POINT;
        bool _gotmousedown = false;
        bool _dodragdrop = false;

        public TextAreaMouseHandler(TextArea ttextArea)
        {
            _textArea = ttextArea;
            _minSelection = new TextLocation();
            _maxSelection = new TextLocation();
        }

        public void Attach()
        {
            _textArea.Click += new EventHandler(TextAreaClick);
            _textArea.MouseMove += new MouseEventHandler(TextAreaMouseMove);
            _textArea.MouseDown += new MouseEventHandler(OnMouseDown);
            _textArea.DoubleClick += new EventHandler(OnDoubleClick);
            _textArea.MouseLeave += new EventHandler(OnMouseLeave);
            _textArea.MouseUp += new MouseEventHandler(OnMouseUp);
            _textArea.LostFocus += new EventHandler(TextAreaLostFocus);
            _textArea.ToolTipRequest += new ToolTipRequestEventHandler(OnToolTipRequest);
        }

        void OnToolTipRequest(object sender, ToolTipRequestEventArgs e)
        {
            if (e.ToolTipShown)
                return;

            Point mousepos = e.MousePosition;
            FoldMarker marker = _textArea.TextView.GetFoldMarkerFromPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
            if (marker != null && marker.IsFolded)
            {
                StringBuilder sb = new StringBuilder(marker.InnerText);

                // max 10 lines
                int endLines = 0;
                for (int i = 0; i < sb.Length; ++i)
                {
                    if (sb[i] == '\n')
                    {
                        ++endLines;
                        if (endLines >= 10)
                        {
                            sb.Remove(i + 1, sb.Length - i - 1);
                            sb.Append(Environment.NewLine);
                            sb.Append("...");
                            break;
                        }
                    }
                }

                sb.Replace("\t", "    ");
                e.ShowToolTip(sb.ToString());
                return;
            }

            foreach (TextMarker tm in _textArea.Document.MarkerStrategy.GetMarkers(e.LogicalPosition))
            {
                if (tm.ToolTip != null)
                {
                    e.ShowToolTip(tm.ToolTip.Replace("\t", "    "));
                    return;
                }
            }
        }

        void ShowHiddenCursorIfMovedOrLeft()
        {
            _textArea.ShowHiddenCursor(!_textArea.Focused || !_textArea.ClientRectangle.Contains(_textArea.PointToClient(Cursor.Position)));
        }

        void TextAreaLostFocus(object sender, EventArgs e)
        {
            // The call to ShowHiddenCursorIfMovedOrLeft is delayed until pending messages have been processed
            // so that it can properly detect whether the TextArea has really lost focus.
            // For example, the CodeCompletionWindow gets focus when it is shown, but immediately gives back focus to the TextArea.
            _textArea.BeginInvoke(new MethodInvoker(ShowHiddenCursorIfMovedOrLeft));
        }

        void OnMouseLeave(object sender, EventArgs e)
        {
            ShowHiddenCursorIfMovedOrLeft();
            _gotmousedown = false;
            _mousedownpos = NIL_POINT;
        }

        void OnMouseUp(object sender, MouseEventArgs e)
        {
            _textArea.SelectionManager.WhereFrom = SelSource.None;
            _gotmousedown = false;
            _mousedownpos = NIL_POINT;
        }

        void TextAreaClick(object sender, EventArgs e)
        {
            Point mousepos;
            mousepos = _textArea.MousePos;

            if (!_dodragdrop)
            {
                if (_clickedOnSelectedText && _textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
                {
                    _textArea.SelectionManager.ClearSelection();
                    TextLocation clickPosition = _textArea.TextView.GetLogicalPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
                    _textArea.Caret.Position = clickPosition;
                    _textArea.SetDesiredColumn();
                }
            }
        }

        void TextAreaMouseMove(object sender, MouseEventArgs e)
        {
            _textArea.MousePos = e.Location;

            // honour the starting selection strategy
            switch (_textArea.SelectionManager.WhereFrom)
            {
                case SelSource.Gutter:
                    ExtendSelectionToMouse();
                    return;

                case SelSource.TArea:
                    break;
            }

            _textArea.ShowHiddenCursor(false);

            if (_dodragdrop)
            {
                _dodragdrop = false;
                return;
            }

            _doubleclick = false;
            _textArea.MousePos = new Point(e.X, e.Y);

            if (_clickedOnSelectedText)
            {
                if (Math.Abs(_mousedownpos.X - e.X) >= SystemInformation.DragSize.Width / 2 || Math.Abs(_mousedownpos.Y - e.Y) >= SystemInformation.DragSize.Height / 2)
                {
                    _clickedOnSelectedText = false;
                    //Selection selection = textArea.SelectionManager.GetSelectionAt(textArea.Caret.Offset);

                    if (_textArea.SelectionManager.IsSelected(_textArea.Caret.Offset))
                    {
                        string text = _textArea.SelectionManager.SelectedText;
                        bool isReadOnly = _textArea.SelectionManager.SelectionIsReadonly;

                        if (text != null && text.Length > 0)
                        {
                            DataObject dataObject = new DataObject();
                            dataObject.SetData(DataFormats.UnicodeText, true, text);
                            dataObject.SetData(_textArea.SelectionManager.SelectedText);
                            _dodragdrop = true;
                            _textArea.DoDragDrop(dataObject, isReadOnly ? DragDropEffects.All & ~DragDropEffects.Move : DragDropEffects.All);
                        }
                    }
                }

                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (_gotmousedown && _textArea.SelectionManager.WhereFrom == SelSource.TArea)
                {
                    ExtendSelectionToMouse();
                }
            }
        }

        void ExtendSelectionToMouse()
        {
            Point mousepos = _textArea.MousePos;
            TextLocation realmousepos = _textArea.TextView.GetLogicalPosition(Math.Max(0, mousepos.X - _textArea.TextView.DrawingPosition.X), mousepos.Y - _textArea.TextView.DrawingPosition.Y);
            int y = realmousepos.Y;
            realmousepos = _textArea.Caret.ValidatePosition(realmousepos);
            TextLocation oldPos = _textArea.Caret.Position;

            bool isRect = (Control.ModifierKeys & Keys.Alt) != 0;

            // Update caret.
            if (_textArea.SelectionManager.WhereFrom == SelSource.Gutter)
            {
                // the selection is from the gutter
                if (realmousepos.Y < _textArea.SelectionManager.StartPosition.Y)
                {
                    // the selection has moved above the startpoint
                    _textArea.Caret.Position = new TextLocation(0, realmousepos.Y);
                }
                else
                {
                    // the selection has moved below the startpoint
                    _textArea.Caret.Position = _textArea.SelectionManager.NextValidPosition(realmousepos.Y);
                }
            }
            else // from text
            {
                _textArea.Caret.Position = realmousepos;
            }

            _textArea.SelectionManager.ExtendSelection(_textArea.Caret.Position, isRect);

            _textArea.SetDesiredColumn();
        }

        void DoubleClickSelectionExtend()
        {
            Point mousepos;
            mousepos = _textArea.MousePos;

            _textArea.SelectionManager.ClearSelection();

            if (_textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                FoldMarker marker = _textArea.TextView.GetFoldMarkerFromPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
                if (marker != null && marker.IsFolded)
                {
                    marker.IsFolded = false;
                    _textArea.MotherTextAreaControl.AdjustScrollBars();
                }

                if (_textArea.Caret.Offset < _textArea.Document.TextLength)
                {
                    switch (_textArea.Document.GetCharAt(_textArea.Caret.Offset))
                    {
                        case '"':
                            if (_textArea.Caret.Offset < _textArea.Document.TextLength)
                            {
                                int next = FindNext(_textArea.Document, _textArea.Caret.Offset + 1, '"');
                                _minSelection = _textArea.Caret.Position;
                                if (next > _textArea.Caret.Offset && next < _textArea.Document.TextLength)
                                    next += 1;
                                _maxSelection = _textArea.Document.OffsetToPosition(next);
                            }
                            break;

                        default:
                            _minSelection = _textArea.Document.OffsetToPosition(FindWordStart(_textArea.Document, _textArea.Caret.Offset));
                            _maxSelection = _textArea.Document.OffsetToPosition(FindWordEnd(_textArea.Document, _textArea.Caret.Offset));
                            break;
                    }

                    _textArea.Caret.Position = _maxSelection;
                    _textArea.SelectionManager.SetSelection(_minSelection, _maxSelection, false);
                }

                // after a double-click selection, the caret is placed correctly, but it is not positioned internally.  The effect is when the cursor
                // is moved up or down a line, the caret will take on the column first clicked on for the double-click
                _textArea.SetDesiredColumn();

                // orig-HACK WARNING !!!
                // must refresh here, because when a error tooltip is showed and the underlined code is double clicked the textArea 
                // don't update corrctly, updateline doesn't work ... but the refresh does. Mike
                _textArea.Refresh();
            }
        }

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            Point mousepos;
            _textArea.MousePos = e.Location;
            mousepos = e.Location;

            if (_dodragdrop)
            {
                return;
            }

            if (_doubleclick)
            {
                _doubleclick = false;
                return;
            }

            if (_textArea.TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                _gotmousedown = true;
                _textArea.SelectionManager.WhereFrom = SelSource.TArea;
                _button = e.Button;

                // double-click
                if (_button == MouseButtons.Left && e.Clicks == 2)
                {
                    int deltaX = Math.Abs(_lastmousedownpos.X - e.X);
                    int deltaY = Math.Abs(_lastmousedownpos.Y - e.Y);

                    if (deltaX <= SystemInformation.DoubleClickSize.Width && deltaY <= SystemInformation.DoubleClickSize.Height)
                    {
                        DoubleClickSelectionExtend();
                        _lastmousedownpos = new Point(e.X, e.Y);

                        if (_textArea.SelectionManager.WhereFrom == SelSource.Gutter)
                        {
                            if (_minSelection.IsValid && _maxSelection.IsValid && _textArea.SelectionManager.IsValid)
                            {
                                _textArea.SelectionManager.StartPosition = _minSelection;
                                _textArea.SelectionManager.EndPosition = _maxSelection;

                                _minSelection = new TextLocation();
                                _maxSelection = new TextLocation();
                            }
                        }
                        return;
                    }
                }

                _minSelection = new TextLocation();
                _maxSelection = new TextLocation();

                _lastmousedownpos = _mousedownpos = new Point(e.X, e.Y);
                bool isRect = (Control.ModifierKeys & Keys.Alt) != 0;

                if (_button == MouseButtons.Left)
                {
                    FoldMarker marker = _textArea.TextView.GetFoldMarkerFromPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
                    if (marker != null && marker.IsFolded)
                    {
                        if (_textArea.SelectionManager.HasSomethingSelected)
                        {
                            _clickedOnSelectedText = true;
                        }

                        TextLocation startLocation = new TextLocation(marker.StartColumn, marker.StartLine);
                        TextLocation endLocation = new TextLocation(marker.EndColumn, marker.EndLine);
                        _textArea.SelectionManager.SetSelection(startLocation, endLocation, isRect);
                        _textArea.Caret.Position = startLocation;
                        _textArea.SetDesiredColumn();
                        _textArea.Focus();
                        return;
                    }

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) // Shift while selecting end point.
                    {
                        ExtendSelectionToMouse();
                    }
                    else
                    {
                        TextLocation realmousepos = _textArea.TextView.GetLogicalPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
                        _clickedOnSelectedText = false;

                        int offset = _textArea.Document.PositionToOffset(realmousepos);

                        if (_textArea.SelectionManager.HasSomethingSelected && _textArea.SelectionManager.IsSelected(offset))
                        {
                            _clickedOnSelectedText = true;
                        }
                        else
                        {
                            _textArea.SelectionManager.ClearSelection();
                            if (mousepos.Y > 0 && mousepos.Y < _textArea.TextView.DrawingPosition.Height)
                            {
                                TextLocation pos = new TextLocation();
                                pos.Y = Math.Min(_textArea.Document.TotalNumberOfLines - 1, realmousepos.Y);
                                pos.X = realmousepos.X;
                                _textArea.Caret.Position = pos;
                                _textArea.SelectionManager.SetSelection(pos, pos, isRect);
                                _textArea.SetDesiredColumn();
                            }
                        }
                    }
                }
                else if (_button == MouseButtons.Right)
                {
                    // Rightclick sets the cursor to the click position unless the previous selection was clicked
                    TextLocation realmousepos = _textArea.TextView.GetLogicalPosition(mousepos.X - _textArea.TextView.DrawingPosition.X, mousepos.Y - _textArea.TextView.DrawingPosition.Y);
                    int offset = _textArea.Document.PositionToOffset(realmousepos);
                    if (!_textArea.SelectionManager.HasSomethingSelected || !_textArea.SelectionManager.IsSelected(offset))
                    {
                        _textArea.SelectionManager.ClearSelection();
                        if (mousepos.Y > 0 && mousepos.Y < _textArea.TextView.DrawingPosition.Height)
                        {
                            TextLocation pos = new TextLocation();
                            pos.Y = Math.Min(_textArea.Document.TotalNumberOfLines - 1, realmousepos.Y);
                            pos.X = realmousepos.X;
                            _textArea.Caret.Position = pos;
                            _textArea.SetDesiredColumn();
                        }
                    }
                }
            }
            _textArea.Focus();
        }

        int FindNext(Document.Document document, int offset, char ch)
        {
            LineSegment line = document.GetLineSegmentForOffset(offset);
            int endPos = line.Offset + line.Length;

            while (offset < endPos && document.GetCharAt(offset) != ch)
            {
                ++offset;
            }
            return offset;
        }

        bool IsSelectableChar(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        int FindWordStart(Document.Document document, int offset)
        {
            LineSegment line = document.GetLineSegmentForOffset(offset);

            if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && Char.IsWhiteSpace(document.GetCharAt(offset)))
            {
                while (offset > line.Offset && Char.IsWhiteSpace(document.GetCharAt(offset - 1)))
                {
                    --offset;
                }
            }
            else if (IsSelectableChar(document.GetCharAt(offset)) || (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset)) && IsSelectableChar(document.GetCharAt(offset - 1))))
            {
                while (offset > line.Offset && IsSelectableChar(document.GetCharAt(offset - 1)))
                {
                    --offset;
                }
            }
            else
            {
                if (offset > 0 && !Char.IsWhiteSpace(document.GetCharAt(offset - 1)) && !IsSelectableChar(document.GetCharAt(offset - 1)))
                {
                    return Math.Max(0, offset - 1);
                }
            }
            return offset;
        }

        int FindWordEnd(Document.Document document, int offset)
        {
            LineSegment line = document.GetLineSegmentForOffset(offset);
            if (line.Length == 0)
                return offset;
            int endPos = line.Offset + line.Length;
            offset = Math.Min(offset, endPos - 1);

            if (IsSelectableChar(document.GetCharAt(offset)))
            {
                while (offset < endPos && IsSelectableChar(document.GetCharAt(offset)))
                {
                    ++offset;
                }
            }
            else if (Char.IsWhiteSpace(document.GetCharAt(offset)))
            {
                if (offset > 0 && Char.IsWhiteSpace(document.GetCharAt(offset - 1)))
                {
                    while (offset < endPos && Char.IsWhiteSpace(document.GetCharAt(offset)))
                    {
                        ++offset;
                    }
                }
            }
            else
            {
                return Math.Max(0, offset + 1);
            }

            return offset;
        }

        void OnDoubleClick(object sender, System.EventArgs e)
        {
            if (_dodragdrop)
            {
                return;
            }

            _textArea.SelectionManager.WhereFrom = SelSource.TArea;
            _doubleclick = true;
        }
    }
}
