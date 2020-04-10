// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    public delegate bool KeyEventHandler(char ch);
    public delegate bool DialogKeyProcessor(Keys keyData);

    /// <summary>
    /// This class paints the textarea.
    /// </summary>
    [ToolboxItem(false)]
    [Browsable(false)]
    public class TextArea : Control
    {
        #region Fields
        bool _hiddenMouseCursor = false;

        /// <summary>
        /// The position where the mouse cursor was when it was hidden. Sometimes the text editor gets MouseMove
        /// events when typing text even if the mouse is not moved.
        /// </summary>
        Point _mouseCursorHidePosition;

        Point _virtualTop = new Point(0, 0);

        readonly List<BracketHighlightingSheme> _bracketSchemes = new List<BracketHighlightingSheme>();

        readonly List<IMargin> _leftMargins = new List<IMargin>();

        bool _disposed = false;

        IMargin _lastMouseInMargin = null;

        IMargin _updateMargin = null;


        ////////////////////// From TextAreaMouseHandler////////////////////////////
        TextLocation _minSelection = new TextLocation();
        TextLocation _maxSelection = new TextLocation();
        bool _doubleClick = false;
        bool _clickedOnSelectedText = false;
        MouseButtons _button = MouseButtons.None;
        static readonly Point NIL_POINT = new Point(-1, -1); //TODO1 not really used?
        Point _mouseDownPos = NIL_POINT;
        Point _lastMouseDownPos = NIL_POINT;
        bool _gotMouseDown = false;

        #endregion

        #region Properties
        public Point MousePos { get; set; }

        public bool ReadOnly { get {  return Document == null || Document.ReadOnly; } }

        [Browsable(false)]
        public IList<IMargin> LeftMargins { get { return _leftMargins.AsReadOnly(); } }

        public TextEditorControl MotherTextEditorControl { get; private set; }

        public TextAreaControl MotherTextAreaControl { get; private set; }

        public SelectionManager SelectionManager { get; }

        public Caret Caret { get; }

        public TextView TextView { get; }

        public GutterMargin GutterMargin { get; }

        public FoldMargin FoldMargin { get; }

        public IconBarMargin IconBarMargin { get; }

        public Encoding Encoding { get { return MotherTextEditorControl.Encoding; } }

        public int MaxVScrollValue { get { return (Document.GetVisibleLine(Document.TotalNumberOfLines - 1) + 1 + TextView.VisibleLineCount * 2 / 3) * TextView.FontHeight; } }

        public Point VirtualTop
        {
            get
            {
                return _virtualTop;
            }
            set
            {
                Point newVirtualTop = new Point(value.X, Math.Min(MaxVScrollValue, Math.Max(0, value.Y)));
                if (_virtualTop != newVirtualTop)
                {
                    _virtualTop = newVirtualTop;
                    MotherTextAreaControl.VScrollBar.Value = _virtualTop.Y;
                    Invalidate();
                }
                Caret.UpdateCaretPosition();
            }
        }

        public bool AutoClearSelection { get; set; } = false;

        [Browsable(false)]
        public Document.Document Document { get { return MotherTextEditorControl.Document; } }

        public bool EnableCutOrPaste
        {
            get
            {
                if (MotherTextAreaControl == null)
                    return false;
                return !ReadOnly;
            }
        }

        #endregion

        #region Events
        //public event ToolTipRequestEventHandler ToolTipRequest;
        //public event KeyEventHandler KeyEventHandler;
        //public event DialogKeyProcessor DoProcessDialogKey;
        #endregion

        #region Lifecycle
        public TextArea(TextEditorControl motherTextEditorControl, TextAreaControl motherTextAreaControl)
        {
            MotherTextAreaControl = motherTextAreaControl;
            MotherTextEditorControl = motherTextEditorControl;

            Caret = new Caret(this);
            SelectionManager = new SelectionManager(Document, this);
            MousePos = new Point(0, 0);

            ResizeRedraw = true;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //			SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Opaque, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);

            TextView = new TextView(this);

            GutterMargin = new GutterMargin(this);
            FoldMargin = new FoldMargin(this);
            IconBarMargin = new IconBarMargin(this);
            _leftMargins.AddRange(new IMargin[] { IconBarMargin, GutterMargin, FoldMargin });
            OptionsChanged();

            // From old mouse handlers:
            Click += MH_Click;
            MouseMove += MH_MouseMove;
            MouseDown += MH_MouseDown;
            DoubleClick += MH_DoubleClick;
            MouseLeave += MH_MouseLeave;
            MouseUp += MH_MouseUp;
            LostFocus += MH_LostFocus;

            _bracketSchemes.Add(new BracketHighlightingSheme('{', '}')); //TODO1 hardcoded
            _bracketSchemes.Add(new BracketHighlightingSheme('(', ')'));
            _bracketSchemes.Add(new BracketHighlightingSheme('[', ']'));

            Caret.PositionChanged += SearchMatchingBracket;
            Document.TextContentChanged += TextContentChanged;
            Document.FoldingManager.FoldingsChanged += DocumentFoldingsChanged;
        }
        #endregion

        #region Public functions
        public void UpdateMatchingBracket()
        {
            SearchMatchingBracket(null, null);
        }

        public void InsertLeftMargin(int index, IMargin margin)
        {
            _leftMargins.Insert(index, margin);
            Refresh();
        }

        public Highlight FindMatchingBracketHighlight()
        {
            if (Caret.Offset == 0)
                return null;

            foreach (BracketHighlightingSheme bracketsheme in _bracketSchemes)
            {
                Highlight highlight = bracketsheme.GetHighlight(Document, Caret.Offset - 1);

                if (highlight != null)
                {
                    return highlight;
                }
            }
            return null;
        }

        public void SetDesiredColumn()
        {
            Caret.DesiredColumn = TextView.GetDrawingXPos(Caret.Line, Caret.Column) + VirtualTop.X;
        }

        public void SetCaretToDesiredColumn()
        {
            FoldMarker dummy;
            Caret.Position = TextView.GetLogicalColumn(Caret.Line, Caret.DesiredColumn + VirtualTop.X, out dummy);
        }


        ////////////////////////////////////////////////////////








        public void OptionsChanged()
        {
            UpdateMatchingBracket();
            TextView.OptionsChanged();
            Caret.RecreateCaret();
            Caret.UpdateCaretPosition();
            Refresh();
        }

        public void Refresh(IMargin margin)
        {
            _updateMargin = margin;
            Invalidate(_updateMargin.DrawingPosition);
            Update();
            _updateMargin = null;
        }

        public void ScrollToCaret()
        {
            MotherTextAreaControl.ScrollToCaret();
        }

        public void ScrollTo(int line)
        {
            MotherTextAreaControl.ScrollTo(line);
        }

        public void BeginUpdate()
        {
            MotherTextEditorControl.BeginUpdate();
        }

        public void EndUpdate()
        {
            MotherTextEditorControl.EndUpdate();
        }

        /// <remarks>
        /// Inserts a single character at the caret position
        /// </remarks>
        public void InsertChar(char ch)
        {
            bool updating = MotherTextEditorControl.IsInUpdate;
            if (!updating)
            {
                BeginUpdate();
            }

            // filter out forgein whitespace chars and replace them with standard space (ASCII 32)
            if (char.IsWhiteSpace(ch) && ch != '\t' && ch != '\n')
            {
                ch = ' ';
            }

            Document.UndoStack.StartUndoGroup();
            if (Shared.TEP.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.IsValid)
            {
                Caret.Position = SelectionManager.StartPosition;
                SelectionManager.RemoveSelectedText();
            }

            LineSegment caretLine = Document.GetLineSegment(Caret.Line);
            int offset = Caret.Offset;
            // use desired column for generated whitespaces
            int dc = Caret.Column;

            if (caretLine.Length < dc && ch != '\n')
            {
                Document.Insert(offset, GenerateWhitespaceString(dc - caretLine.Length) + ch);
            }
            else
            {
                Document.Insert(offset, ch.ToString());
            }

            Document.UndoStack.EndUndoGroup();
            ++Caret.Column;

            if (!updating)
            {
                EndUpdate();
                UpdateLineToEnd(Caret.Line, Caret.Column);
            }

            // TODO0 ?? I prefer to set NOT the standard column, if you type something ++Caret.DesiredColumn;
        }

        /// <remarks>
        /// Inserts a whole string at the caret position
        /// </remarks>
        public void InsertString(string str)
        {
            bool updating = MotherTextEditorControl.IsInUpdate;
            if (!updating)
            {
                BeginUpdate();
            }

            try
            {
                Document.UndoStack.StartUndoGroup();
                if (Shared.TEP.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.IsValid)
                {
                    Caret.Position = SelectionManager.StartPosition;
                    SelectionManager.RemoveSelectedText();
                }

                int oldOffset = Document.PositionToOffset(Caret.Position);
                int oldLine = Caret.Line;
                LineSegment caretLine = Document.GetLineSegment(Caret.Line);

                if (caretLine.Length < Caret.Column)
                {
                    int whiteSpaceLength = Caret.Column - caretLine.Length;
                    Document.Insert(oldOffset, GenerateWhitespaceString(whiteSpaceLength) + str);
                    Caret.Position = Document.OffsetToPosition(oldOffset + str.Length + whiteSpaceLength);
                }
                else
                {
                    Document.Insert(oldOffset, str);
                    Caret.Position = Document.OffsetToPosition(oldOffset + str.Length);
                }

                Document.UndoStack.EndUndoGroup();

                if (oldLine != Caret.Line)
                {
                    UpdateToEnd(oldLine);
                }
                else
                {
                    UpdateLineToEnd(Caret.Line, Caret.Column);
                }
            }
            finally
            {
                if (!updating)
                {
                    EndUpdate();
                }
            }
        }

        /// <remarks>
        /// Replaces a char at the caret position
        /// </remarks>
        public void ReplaceChar(char ch)
        {
            bool updating = MotherTextEditorControl.IsInUpdate;
            if (!updating)
            {
                BeginUpdate();
            }

            if (Shared.TEP.DocumentSelectionMode == DocumentSelectionMode.Normal && SelectionManager.IsValid)
            {
                Caret.Position = SelectionManager.StartPosition;
                SelectionManager.RemoveSelectedText();
            }

            int lineNr = Caret.Line;
            LineSegment line = Document.GetLineSegment(lineNr);
            int offset = Document.PositionToOffset(Caret.Position);

            if (offset < line.Offset + line.Length)
            {
                Document.Replace(offset, 1, ch.ToString());
            }
            else
            {
                Document.Insert(offset, ch.ToString());
            }

            if (!updating)
            {
                EndUpdate();
                UpdateLineToEnd(lineNr, Caret.Column);
            }

            ++Caret.Column;
            //			++Caret.DesiredColumn;
        }
        #endregion

        #region Private functions
        void TextContentChanged(object sender, EventArgs e)
        {
            Caret.Position = new TextLocation(0, 0);
            SelectionManager.ClearSelection();
        }

        void SearchMatchingBracket(object sender, EventArgs e)
        {
            if (!Shared.TEP.ShowMatchingBracket)
            {
                TextView.Highlight = null;
                return;
            }

            int oldLine1 = -1, oldLine2 = -1;
            if (TextView.Highlight != null && TextView.Highlight.OpenBrace.Y >= 0 && TextView.Highlight.OpenBrace.Y < Document.TotalNumberOfLines)
            {
                oldLine1 = TextView.Highlight.OpenBrace.Y;
            }

            if (TextView.Highlight != null && TextView.Highlight.CloseBrace.Y >= 0 && TextView.Highlight.CloseBrace.Y < Document.TotalNumberOfLines)
            {
                oldLine2 = TextView.Highlight.CloseBrace.Y;
            }

            TextView.Highlight = FindMatchingBracketHighlight();

            if (oldLine1 >= 0)
                UpdateLine(oldLine1);

            if (oldLine2 >= 0 && oldLine2 != oldLine1)
                UpdateLine(oldLine2);

            if (TextView.Highlight != null)
            {
                int newLine1 = TextView.Highlight.OpenBrace.Y;
                int newLine2 = TextView.Highlight.CloseBrace.Y;

                if (newLine1 != oldLine1 && newLine1 != oldLine2)
                    UpdateLine(newLine1);

                if (newLine2 != oldLine1 && newLine2 != oldLine2 && newLine2 != newLine1)
                    UpdateLine(newLine2);
            }
        }

        /// <summary>
        /// Shows the mouse cursor if it has been hidden.
        /// </summary>
        /// <param name="forceShow"><c>true</c> to always show the cursor or <c>false</c> to show it only if it has been moved since it was hidden.</param>
        internal void ShowHiddenCursor(bool forceShow)
        {
            if (_hiddenMouseCursor)
            {
                if (_mouseCursorHidePosition != Cursor.Position || forceShow)
                {
                    Cursor.Show();
                    _hiddenMouseCursor = false;
                }
            }
        }

        // void SetToolTip(string text, int lineNumber)
        // {
            //if (toolTip == null || toolTip.IsDisposed)
            //    toolTip = new DeclarationViewWindow(FindForm());
            //if (oldToolTip == text)
            //    return;
            //if (text == null)
            //{
            //    toolTip.Hide();
            //}
            //else
            //{
            //    Point p = Control.MousePosition;
            //    Point cp = PointToClient(p);
            //    if (lineNumber >= 0)
            //    {
            //        lineNumber = Document.GetVisibleLine(lineNumber);
            //        p.Y = (p.Y - cp.Y) + (lineNumber * TextView.FontHeight) - _virtualTop.Y;
            //    }
            //    p.Offset(3, 3);
            //    toolTip.Owner = FindForm();
            //    toolTip.Location = p;
            //    toolTip.Description = text;
            //    toolTip.HideOnClick = true;
            //    toolTip.Show();
            //}
            //oldToolTip = text;
        // }

        //void CloseToolTip()
        //{
        //    if (_toolTipActive)
        //    {
        //        //Console.WriteLine("Closing tooltip");
        //        _toolTipActive = false;
        //        SetToolTip(null, -1);
        //    }
        //    ResetMouseEventArgs();
        //}

        void DocumentFoldingsChanged(object sender, EventArgs e)
        {
            Caret.UpdateCaretPosition();
            Invalidate();
            MotherTextAreaControl.AdjustScrollBars();
        }

        //protected void RequestToolTip(Point mousePos)
        //{
        //    if (_toolTipRectangle.Contains(mousePos))
        //    {
        //        if (!_toolTipActive)
        //            ResetMouseEventArgs();
        //        return;
        //    }

        //    //Console.WriteLine("Request tooltip for " + mousePos);

        //    _toolTipRectangle = new Rectangle(mousePos.X - 4, mousePos.Y - 4, 8, 8);

        //    TextLocation logicPos = TextView.GetLogicalPosition(mousePos.X - TextView.DrawingPosition.Left, mousePos.Y - TextView.DrawingPosition.Top);
        //    bool inDocument = TextView.DrawingPosition.Contains(mousePos) && logicPos.Y >= 0 && logicPos.Y < Document.TotalNumberOfLines;
        //    ToolTipRequestEventArgs args = new ToolTipRequestEventArgs(mousePos, logicPos, inDocument);
        //    OnToolTipRequest(args);
        //    if (args.ToolTipShown)
        //    {
        //        //Console.WriteLine("Set tooltip to " + args.toolTipText);
        //        _toolTipActive = true;
        //        SetToolTip(args.toolTipText, inDocument ? logicPos.Y + 1 : -1);
        //    }
        //    else
        //    {
        //        CloseToolTip();
        //    }
        //}

        // external interface to the attached event
        internal void RaiseMouseMove(MouseEventArgs e)
        {
            OnMouseMove(e);
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            int currentXPos = 0;
            int currentYPos = 0;
            bool adjustScrollBars = false;
            Graphics g = e.Graphics;
            Rectangle clipRectangle = e.ClipRectangle;

            bool isFullRepaint = clipRectangle.X == 0 && clipRectangle.Y == 0 && clipRectangle.Width == Width && clipRectangle.Height == Height;

            g.TextRenderingHint = Shared.TEP.TextRenderingHint;

            if (_updateMargin != null)
            {
                _updateMargin.Paint(g, _updateMargin.DrawingPosition);
                //				clipRectangle.Intersect(updateMargin.DrawingPosition);
            }

            if (clipRectangle.Width <= 0 || clipRectangle.Height <= 0)
            {
                return;
            }

            foreach (IMargin margin in _leftMargins)
            {
                if (margin.IsVisible)
                {
                    Rectangle marginRectangle = new Rectangle(currentXPos, currentYPos, margin.Size.Width, Height - currentYPos);
                    if (marginRectangle != margin.DrawingPosition)
                    {
                        // margin changed size
                        if (!isFullRepaint && !clipRectangle.Contains(marginRectangle))
                        {
                            Invalidate(); // do a full repaint
                        }
                        adjustScrollBars = true;
                        margin.DrawingPosition = marginRectangle;
                    }

                    currentXPos += margin.DrawingPosition.Width;
                    if (clipRectangle.IntersectsWith(marginRectangle))
                    {
                        marginRectangle.Intersect(clipRectangle);
                        if (!marginRectangle.IsEmpty)
                        {
                            margin.Paint(g, marginRectangle);
                        }
                    }
                }
            }

            Rectangle textViewArea = new Rectangle(currentXPos, currentYPos, Width - currentXPos, Height - currentYPos);
            if (textViewArea != TextView.DrawingPosition)
            {
                adjustScrollBars = true;
                TextView.DrawingPosition = textViewArea;
                // update caret position (but outside of WM_PAINT!)
                BeginInvoke((MethodInvoker)Caret.UpdateCaretPosition);
            }

            if (clipRectangle.IntersectsWith(textViewArea))
            {
                textViewArea.Intersect(clipRectangle);
                if (!textViewArea.IsEmpty)
                {
                    TextView.Paint(g, textViewArea);
                }
            }

            if (adjustScrollBars)
            {
                MotherTextAreaControl.AdjustScrollBars();
            }

            // we cannot update the caret position here, it's not allowed to call the caret API inside WM_PAINT
            //Caret.UpdateCaretPosition();

            base.OnPaint(e);
        }

        string GenerateWhitespaceString(int length)
        {
            return new string(' ', length);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (Caret != null)
                    {
                        Caret.PositionChanged -= new EventHandler(SearchMatchingBracket);
                        Caret.Dispose();
                    }

                    if (SelectionManager != null)
                    {
                        SelectionManager.Dispose();
                    }

                    Document.TextContentChanged -= TextContentChanged;
                    Document.FoldingManager.FoldingsChanged -= DocumentFoldingsChanged;
                    MotherTextAreaControl = null;
                    MotherTextEditorControl = null;

                    foreach (IMargin margin in _leftMargins)
                    {
                        if (margin is IDisposable)
                            (margin as IDisposable).Dispose();
                    }

                    TextView.Dispose();
                }
            }
        }

        #endregion

        #region Event handlers
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            if (_lastMouseInMargin != null)
            {
                _lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
                _lastMouseInMargin = null;
            }
            //CloseToolTip();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //Debug.WriteLine("OnMouseDown");

            // this corrects weird problems when text is selected,
            // then a menu item is selected, then the text is
            // clicked again - it correctly synchronises the
            // click position
            MousePos = new Point(e.X, e.Y);

            base.OnMouseDown(e);
            //CloseToolTip();

            foreach (IMargin margin in _leftMargins)
            {
                if (margin.DrawingPosition.Contains(e.X, e.Y))
                {
                    margin.HandleMouseDown(new Point(e.X, e.Y), e.Button);
                }
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
            //Console.WriteLine("Hover raised at " + PointToClient(Control.MousePosition));
            if (MouseButtons == MouseButtons.None)
            {
                /////RequestToolTip(PointToClient(Control.MousePosition));
            }
            else
            {
                //CloseToolTip();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            //if (!_toolTipRectangle.Contains(e.Location))
            //{
            //    _toolTipRectangle = Rectangle.Empty;

            //    if (_toolTipActive)
            //    {
            //        RequestToolTip(e.Location);
            //    }
            //}

            foreach (IMargin margin in _leftMargins)
            {
                if (margin.DrawingPosition.Contains(e.X, e.Y))
                {
                    Cursor = margin.Cursor;
                    margin.HandleMouseMove(new Point(e.X, e.Y), e.Button);

                    if (_lastMouseInMargin != margin)
                    {
                        if (_lastMouseInMargin != null)
                        {
                            _lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
                        }
                        _lastMouseInMargin = margin;
                    }
                    return;
                }
            }

            if (_lastMouseInMargin != null)
            {
                _lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
                _lastMouseInMargin = null;
            }

            if (TextView.DrawingPosition.Contains(e.X, e.Y))
            {
                TextLocation realmousepos = TextView.GetLogicalPosition(e.X - TextView.DrawingPosition.X, e.Y - TextView.DrawingPosition.Y);
                if (SelectionManager.IsSelected(Document.PositionToOffset(realmousepos)) && MouseButtons == MouseButtons.None)
                {
                    // mouse is hovering over a selection, so show default mouse
                    Cursor = Cursors.Default;
                }
                else
                {
                    // mouse is hovering over text area, not a selection, so show the textView cursor
                    Cursor = TextView.Cursor;
                }
                return;
            }

            Cursor = Cursors.Default;
        }


        void MH_MouseLeave(object sender, EventArgs e)
        {
            ShowHiddenCursorIfMovedOrLeft();
            _gotMouseDown = false;
            _mouseDownPos = NIL_POINT;
        }

        void MH_MouseUp(object sender, MouseEventArgs e)
        {
            SelectionManager.WhereFrom = SelSource.None;
            _gotMouseDown = false;
            _mouseDownPos = NIL_POINT;
        }

        void MH_LostFocus(object sender, EventArgs e)
        {
            // The call to ShowHiddenCursorIfMovedOrLeft is delayed until pending messages have been processed
            // so that it can properly detect whether the TextArea has really lost focus.
            // For example, the CodeCompletionWindow gets focus when it is shown, but immediately gives back focus to the TextArea.
            BeginInvoke(new MethodInvoker(ShowHiddenCursorIfMovedOrLeft));
        }

        void MH_Click(object sender, EventArgs e)
        {
            Point mousepos;
            mousepos = MousePos;

            if (_clickedOnSelectedText && TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                SelectionManager.ClearSelection();
                TextLocation clickPosition = TextView.GetLogicalPosition(mousepos.X - TextView.DrawingPosition.X, mousepos.Y - TextView.DrawingPosition.Y);
                Caret.Position = clickPosition;
                SetDesiredColumn();
            }
        }

        void MH_MouseMove(object sender, MouseEventArgs e)
        {
            MousePos = e.Location;

            // honour the starting selection strategy
            switch (SelectionManager.WhereFrom)
            {
                case SelSource.Gutter:
                    ExtendSelectionToMouse();
                    return;

                case SelSource.TArea:
                    break;
            }

            ShowHiddenCursor(false);

            _doubleClick = false;
            MousePos = new Point(e.X, e.Y);

            if (_clickedOnSelectedText)
            {
                if (Math.Abs(_mouseDownPos.X - e.X) >= SystemInformation.DragSize.Width / 2 || Math.Abs(_mouseDownPos.Y - e.Y) >= SystemInformation.DragSize.Height / 2)
                {
                    _clickedOnSelectedText = false;
                    //Selection selection = textArea.SelectionManager.GetSelectionAt(textArea.Caret.Offset);

                    if (SelectionManager.IsSelected(Caret.Offset))
                    {
                        string text = SelectionManager.SelectedText;
                        //bool isReadOnly = SelectionManager.SelectionIsReadonly;

                        if (text != null && text.Length > 0)
                        {
                            DataObject dataObject = new DataObject();
                            dataObject.SetData(DataFormats.UnicodeText, true, text);
                            dataObject.SetData(SelectionManager.SelectedText);
                            //_dodragdrop = true;
                            //DoDragDrop(dataObject, isReadOnly ? DragDropEffects.All & ~DragDropEffects.Move : DragDropEffects.All);
                        }
                    }
                }

                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (_gotMouseDown && SelectionManager.WhereFrom == SelSource.TArea)
                {
                    ExtendSelectionToMouse();
                }
            }
        }

        void MH_MouseDown(object sender, MouseEventArgs e)
        {
            //Debug.WriteLine("MH_OnMouseDown");
            MousePos = e.Location;

            if (_doubleClick)
            {
                _doubleClick = false;
                return;
            }

            if (TextView.DrawingPosition.Contains(MousePos.X, MousePos.Y))
            {
                _gotMouseDown = true;
                SelectionManager.WhereFrom = SelSource.TArea;
                _button = e.Button;

                // double-click
                if (_button == MouseButtons.Left && e.Clicks == 2)
                {
                    int deltaX = Math.Abs(_lastMouseDownPos.X - e.X);
                    int deltaY = Math.Abs(_lastMouseDownPos.Y - e.Y);

                    if (deltaX <= SystemInformation.DoubleClickSize.Width && deltaY <= SystemInformation.DoubleClickSize.Height)
                    {
                        DoubleClickSelectionExtend();
                        _lastMouseDownPos = new Point(e.X, e.Y);

                        if (SelectionManager.WhereFrom == SelSource.Gutter)
                        {
                            if (_minSelection.IsValid && _maxSelection.IsValid && SelectionManager.IsValid)
                            {
                                SelectionManager.StartPosition = _minSelection;
                                SelectionManager.EndPosition = _maxSelection;

                                _minSelection = new TextLocation();
                                _maxSelection = new TextLocation();
                            }
                        }
                        return;
                    }
                }

                _minSelection = new TextLocation();
                _maxSelection = new TextLocation();

                _lastMouseDownPos = _mouseDownPos = new Point(e.X, e.Y);
                bool isRect = (Control.ModifierKeys & Keys.Alt) != 0;

                if (_button == MouseButtons.Left)
                {
                    FoldMarker marker = TextView.GetFoldMarkerFromPosition(MousePos.X - TextView.DrawingPosition.X, MousePos.Y - TextView.DrawingPosition.Y);
                    if (marker != null && marker.IsFolded)
                    {
                        if (SelectionManager.HasSomethingSelected)
                        {
                            _clickedOnSelectedText = true;
                        }

                        TextLocation startLocation = new TextLocation(marker.StartColumn, marker.StartLine);
                        TextLocation endLocation = new TextLocation(marker.EndColumn, marker.EndLine);
                        SelectionManager.SetSelection(startLocation, endLocation, isRect);
                        Caret.Position = startLocation;
                        SetDesiredColumn();
                        Focus();
                        return;
                    }

                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) // Shift while selecting end point.
                    {
                        ExtendSelectionToMouse();
                    }
                    else
                    {
                        TextLocation realmousepos = TextView.GetLogicalPosition(MousePos.X - TextView.DrawingPosition.X, MousePos.Y - TextView.DrawingPosition.Y);
                        _clickedOnSelectedText = false;

                        int offset = Document.PositionToOffset(realmousepos);

                        if (SelectionManager.HasSomethingSelected && SelectionManager.IsSelected(offset))
                        {
                            _clickedOnSelectedText = true;
                        }
                        else
                        {
                            SelectionManager.ClearSelection();
                            if (MousePos.Y > 0 && MousePos.Y < TextView.DrawingPosition.Height)
                            {
                                TextLocation pos = new TextLocation();
                                pos.Y = Math.Min(Document.TotalNumberOfLines - 1, realmousepos.Y);
                                pos.X = realmousepos.X;
                                Caret.Position = pos;
                                SelectionManager.SetSelection(pos, pos, isRect);
                                SetDesiredColumn();
                            }
                        }
                    }
                }
                else if (_button == MouseButtons.Right)
                {
                    // Rightclick sets the cursor to the click position unless the previous selection was clicked
                    TextLocation realmousepos = TextView.GetLogicalPosition(MousePos.X - TextView.DrawingPosition.X, MousePos.Y - TextView.DrawingPosition.Y);
                    int offset = Document.PositionToOffset(realmousepos);
                    if (!SelectionManager.HasSomethingSelected || !SelectionManager.IsSelected(offset))
                    {
                        SelectionManager.ClearSelection();
                        if (MousePos.Y > 0 && MousePos.Y < TextView.DrawingPosition.Height)
                        {
                            TextLocation pos = new TextLocation();
                            pos.Y = Math.Min(Document.TotalNumberOfLines - 1, realmousepos.Y);
                            pos.X = realmousepos.X;
                            Caret.Position = pos;
                            SetDesiredColumn();
                        }
                    }
                }
            }
            Focus();
        }

        void MH_DoubleClick(object sender, System.EventArgs e)
        {
            SelectionManager.WhereFrom = SelSource.TArea;
            _doubleClick = true;
        }


        #endregion

        public bool ExecuteAction(EditAction action)
        {
            bool ok = true;

            BeginUpdate();
            try
            {
                lock (Document)
                {
                    action.Execute(this);
                    if (SelectionManager.HasSomethingSelected && AutoClearSelection /*&& caretchanged*/)
                    {
                        if (Shared.TEP.DocumentSelectionMode == DocumentSelectionMode.Normal)
                        {
                            SelectionManager.ClearSelection();
                        }
                    }
                }
            }
            finally
            {
                EndUpdate();
                Caret.UpdateCaretPosition();
            }

            return ok;
        }


        #region Keyboard handling
        ///// <summary>
        ///// This method is called on each Keypress
        ///// </summary>
        ///// <returns>
        ///// True, if the key is handled by this method and should NOT be
        ///// inserted in the textarea.
        ///// </returns>
        //protected internal virtual bool HandleKeyPress(char ch)
        //{
        //    //if (KeyEventHandler != null)
        //    //{
        //    //    return KeyEventHandler(ch);
        //    //}

        //    return false;
        //}

        //// Fixes SD2-747: Form containing the text editor and a button with a shortcut
        //protected override bool IsInputChar(char charCode)
        //{
        //    return true;
        //}

        //internal bool IsReadOnly(int offset)
        //{
        //    if (Document.ReadOnly)
        //    {
        //        return true;
        //    }
        //    if (Shared.TEP.SupportReadOnlySegments)
        //    {
        //        return Document.MarkerStrategy.GetMarkers(offset).Exists(m => m.IsReadOnly);
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //internal bool IsReadOnly(int offset, int length)
        //{
        //    if (Document.ReadOnly)
        //    {
        //        return true;
        //    }
        //    if (Shared.TEP.SupportReadOnlySegments)
        //    {
        //        return Document.MarkerStrategy.GetMarkers(offset, length).Exists(m => m.IsReadOnly);
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            //was SimulateKeyPress(e.KeyChar);
            if (!ReadOnly && e.KeyChar >= ' ')
            {
                // Good to go.
                if (!_hiddenMouseCursor && Shared.TEP.HideMouseCursor)
                {
                    if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
                    {
                        _mouseCursorHidePosition = Cursor.Position;
                        _hiddenMouseCursor = true;
                        Cursor.Hide();
                    }
                }

                BeginUpdate();

                Document.UndoStack.StartUndoGroup();

                try // TODO0 really need this?
                {
                    // INSERT char
                    switch (Caret.CaretMode)
                    {
                        case CaretMode.InsertMode:
                            InsertChar(e.KeyChar); //TODO0 doesn't work for empty docs. Caret goes beyond eol.
                            break;
                        case CaretMode.OverwriteMode:
                            ReplaceChar(e.KeyChar);
                            break;
                    }

                    int currentLineNr = Caret.Line;
                    Document.FormattingStrategy.FormatLine(this, currentLineNr, Document.PositionToOffset(Caret.Position), e.KeyChar);

                    EndUpdate();
                }
                finally
                {
                    Document.UndoStack.EndUndoGroup();
                }
            }

            e.Handled = true;
        }



        /// <summary>
        /// This method executes a dialog key
        /// </summary>
        public bool ExecuteDialogKey(Keys keyData)
        {
            bool ok = true;

            AutoClearSelection = true;
            EditAction action = Shared.CMM.GetEditAction(keyData);

            if (action != null)
            {
                ok = ExecuteAction(action);
            }
            // else not in map

            return ok;
        }

        /// <summary>
        /// The ProcessDialogKey method overrides the base ContainerControl.ProcessDialogKey implementation to provide additional handling of the
        /// RETURN and ESCAPE keys in dialog boxes. The method performs no processing on keystrokes that include the ALT or CONTROL modifiers.
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            AutoClearSelection = true;
            bool ok = ExecuteDialogKey(keyData);

            ok |= base.ProcessDialogKey(keyData);

            return ok;
        }
        #endregion

        #region Update Commands
        internal void UpdateLine(int line)
        {
            UpdateLines(0, line, line);
        }

        internal void UpdateLines(int lineBegin, int lineEnd)
        {
            UpdateLines(0, lineBegin, lineEnd);
        }

        internal void UpdateToEnd(int lineBegin)
        {
            //			if (lineBegin > FirstPhysicalLine + textView.VisibleLineCount) {
            //				return;
            //			}

            lineBegin = Document.GetVisibleLine(lineBegin);
            int y = Math.Max(0, lineBegin * TextView.FontHeight);
            y = Math.Max(0, y - _virtualTop.Y);
            Rectangle r = new Rectangle(0, y, Width, Height - y);
            Invalidate(r);
        }

        internal void UpdateLineToEnd(int lineNr, int xStart)
        {
            UpdateLines(xStart, lineNr, lineNr);
        }

        internal void UpdateLine(int line, int begin, int end)
        {
            UpdateLines(line, line);
        }

        int FirstPhysicalLine()
        {
            return VirtualTop.Y / TextView.FontHeight;
        }

        internal void UpdateLines(int xPos, int lineBegin, int lineEnd)
        {
            //			if (lineEnd < FirstPhysicalLine || lineBegin > FirstPhysicalLine + textView.VisibleLineCount) {
            //				return;
            //			}

            InvalidateLines((int)(xPos * TextView.WideSpaceWidth), lineBegin, lineEnd);
        }

        void InvalidateLines(int xPos, int lineBegin, int lineEnd)
        {
            lineBegin = Math.Max(Document.GetVisibleLine(lineBegin), FirstPhysicalLine());
            lineEnd = Math.Min(Document.GetVisibleLine(lineEnd), FirstPhysicalLine() + TextView.VisibleLineCount);
            int y = Math.Max(0, (int)(lineBegin * TextView.FontHeight));
            int height = Math.Min(TextView.DrawingPosition.Height, (int)((1 + lineEnd - lineBegin) * (TextView.FontHeight + 1)));

            Rectangle r = new Rectangle(0, y - 1 - _virtualTop.Y, Width, height + 3);
            Invalidate(r);
        }
        #endregion

        void ShowHiddenCursorIfMovedOrLeft()
        {
            ShowHiddenCursor(!Focused || !ClientRectangle.Contains(PointToClient(Cursor.Position)));
        }

        void ExtendSelectionToMouse()
        {
            Point mousepos = MousePos;
            TextLocation realmousepos = TextView.GetLogicalPosition(Math.Max(0, mousepos.X - TextView.DrawingPosition.X), mousepos.Y - TextView.DrawingPosition.Y);
            int y = realmousepos.Y;
            realmousepos = Caret.ValidatePosition(realmousepos);
            TextLocation oldPos = Caret.Position;

            bool isRect = (Control.ModifierKeys & Keys.Alt) != 0;

            // Update caret.
            if (SelectionManager.WhereFrom == SelSource.Gutter)
            {
                // the selection is from the gutter
                if (realmousepos.Y < SelectionManager.StartPosition.Y)
                {
                    // the selection has moved above the startpoint
                    Caret.Position = new TextLocation(0, realmousepos.Y);
                }
                else
                {
                    // the selection has moved below the startpoint
                    Caret.Position = SelectionManager.NextValidPosition(realmousepos.Y);
                }
            }
            else // from text
            {
                Caret.Position = realmousepos;
            }

            SelectionManager.ExtendSelection(Caret.Position, isRect);

            SetDesiredColumn();
        }

        void DoubleClickSelectionExtend()
        {
            Point mousepos;
            mousepos = MousePos;

            SelectionManager.ClearSelection();

            if (TextView.DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                FoldMarker marker = TextView.GetFoldMarkerFromPosition(mousepos.X - TextView.DrawingPosition.X, mousepos.Y - TextView.DrawingPosition.Y);
                if (marker != null && marker.IsFolded)
                {
                    marker.IsFolded = false;
                    MotherTextAreaControl.AdjustScrollBars();
                }

                if (Caret.Offset < Document.TextLength)
                {
                    switch (Document.GetCharAt(Caret.Offset))
                    {
                        case '"':
                            if (Caret.Offset < Document.TextLength)
                            {
                                int next = FindNext(Document, Caret.Offset + 1, '"');
                                _minSelection = Caret.Position;
                                if (next > Caret.Offset && next < Document.TextLength)
                                    next += 1;
                                _maxSelection = Document.OffsetToPosition(next);
                            }
                            break;

                        default:
                            _minSelection = Document.OffsetToPosition(FindWordStart(Document, Caret.Offset));
                            _maxSelection = Document.OffsetToPosition(FindWordEnd(Document, Caret.Offset));
                            break;
                    }

                    Caret.Position = _maxSelection;
                    SelectionManager.SetSelection(_minSelection, _maxSelection, false);
                }

                // after a double-click selection, the caret is placed correctly, but it is not positioned internally.  The effect is when the cursor
                // is moved up or down a line, the caret will take on the column first clicked on for the double-click
                SetDesiredColumn();

                // orig-HACK WARNING !!!
                // must refresh here, because when a error tooltip is showed and the underlined code is double clicked the textArea 
                // don't update corrctly, updateline doesn't work ... but the refresh does. Mike
                Refresh();
            }
        }



        //////// TODO0 should these be in the Document class?
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

            if (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset - 1)) && char.IsWhiteSpace(document.GetCharAt(offset)))
            {
                while (offset > line.Offset && char.IsWhiteSpace(document.GetCharAt(offset - 1)))
                {
                    --offset;
                }
            }
            else if (IsSelectableChar(document.GetCharAt(offset)) || (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset)) && IsSelectableChar(document.GetCharAt(offset - 1))))
            {
                while (offset > line.Offset && IsSelectableChar(document.GetCharAt(offset - 1)))
                {
                    --offset;
                }
            }
            else
            {
                if (offset > 0 && !char.IsWhiteSpace(document.GetCharAt(offset - 1)) && !IsSelectableChar(document.GetCharAt(offset - 1)))
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
            else if (char.IsWhiteSpace(document.GetCharAt(offset)))
            {
                if (offset > 0 && char.IsWhiteSpace(document.GetCharAt(offset - 1)))
                {
                    while (offset < endPos && char.IsWhiteSpace(document.GetCharAt(offset)))
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
    }
}
