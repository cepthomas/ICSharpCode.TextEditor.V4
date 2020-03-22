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
    public class TextArea : Control
    {
        public event ToolTipRequestEventHandler ToolTipRequest;
        public event KeyEventHandler KeyEventHandler;
        public event DialogKeyProcessor DoProcessDialogKey;

        bool _hiddenMouseCursor = false;

        /// <summary>
        /// The position where the mouse cursor was when it was hidden. Sometimes the text editor gets MouseMove
        /// events when typing text even if the mouse is not moved.
        /// </summary>
        Point _mouseCursorHidePosition;

        Point _virtualTop = new Point(0, 0);

        readonly List<BracketHighlightingSheme> _bracketSchemes = new List<BracketHighlightingSheme>();

        List<IMargin> _leftMargins = new List<IMargin>();

        bool _disposed;

        // static because the mouse can only be in one text area and we don't want to have
        // tooltips of text areas from inactive tabs floating around.
        //static DeclarationViewWindow toolTip;
        //static readonly string oldToolTip;

        IMargin _lastMouseInMargin;

        bool _toolTipActive;

        /// <summary>
        /// Rectangle in text area that caused the current tool tip.
        /// Prevents tooltip from re-showing when it was closed because of a click or keyboard
        /// input and the mouse was not used.
        /// </summary>
        Rectangle _toolTipRectangle;

        IMargin _updateMargin = null;



        public Point MousePos { get; set; }

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

        public TextAreaClipboardHandler ClipboardHandler { get; }

        public TextArea(TextEditorControl motherTextEditorControl, TextAreaControl motherTextAreaControl)
        {
            MotherTextAreaControl = motherTextAreaControl;
            MotherTextEditorControl = motherTextEditorControl;

            Caret = new Caret(this);
            SelectionManager = new SelectionManager(Document, this);
            MousePos = new Point(0, 0);

            ClipboardHandler = new TextAreaClipboardHandler(this);

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


            new TextAreaMouseHandler(this).Attach();
            new TextAreaDragDropHandler().Attach(this);

            _bracketSchemes.Add(new BracketHighlightingSheme('{', '}'));
            _bracketSchemes.Add(new BracketHighlightingSheme('(', ')'));
            _bracketSchemes.Add(new BracketHighlightingSheme('[', ']'));

            Caret.PositionChanged += new EventHandler(SearchMatchingBracket);
            Document.TextContentChanged += new EventHandler(TextContentChanged);
            Document.FoldingManager.FoldingsChanged += new EventHandler(DocumentFoldingsChanged);
        }

        public void UpdateMatchingBracket()
        {
            SearchMatchingBracket(null, null);
        }

        public void InsertLeftMargin(int index, IMargin margin)
        {
            _leftMargins.Insert(index, margin);
            Refresh();
        }

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

        public void OptionsChanged()
        {
            UpdateMatchingBracket();
            TextView.OptionsChanged();
            Caret.RecreateCaret();
            Caret.UpdateCaretPosition();
            Refresh();
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            if (_lastMouseInMargin != null)
            {
                _lastMouseInMargin.HandleMouseLeave(EventArgs.Empty);
                _lastMouseInMargin = null;
            }
            CloseToolTip();
        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            // this corrects weird problems when text is selected,
            // then a menu item is selected, then the text is
            // clicked again - it correctly synchronises the
            // click position
            MousePos = new Point(e.X, e.Y);

            base.OnMouseDown(e);
            CloseToolTip();

            foreach (IMargin margin in _leftMargins)
            {
                if (margin.DrawingPosition.Contains(e.X, e.Y))
                {
                    margin.HandleMouseDown(new Point(e.X, e.Y), e.Button);
                }
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


        void SetToolTip(string text, int lineNumber)
        {
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
        }

        protected virtual void OnToolTipRequest(ToolTipRequestEventArgs e)
        {
            ToolTipRequest?.Invoke(this, e);
        }

        void CloseToolTip()
        {
            if (_toolTipActive)
            {
                //Console.WriteLine("Closing tooltip");
                _toolTipActive = false;
                SetToolTip(null, -1);
            }
            ResetMouseEventArgs();
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
            //Console.WriteLine("Hover raised at " + PointToClient(Control.MousePosition));
            if (MouseButtons == MouseButtons.None)
            {
                RequestToolTip(PointToClient(Control.MousePosition));
            }
            else
            {
                CloseToolTip();
            }
        }

        protected void RequestToolTip(Point mousePos)
        {
            if (_toolTipRectangle.Contains(mousePos))
            {
                if (!_toolTipActive)
                    ResetMouseEventArgs();
                return;
            }

            //Console.WriteLine("Request tooltip for " + mousePos);

            _toolTipRectangle = new Rectangle(mousePos.X - 4, mousePos.Y - 4, 8, 8);

            TextLocation logicPos = TextView.GetLogicalPosition(mousePos.X - TextView.DrawingPosition.Left, mousePos.Y - TextView.DrawingPosition.Top);
            bool inDocument = TextView.DrawingPosition.Contains(mousePos) && logicPos.Y >= 0 && logicPos.Y < Document.TotalNumberOfLines;
            ToolTipRequestEventArgs args = new ToolTipRequestEventArgs(mousePos, logicPos, inDocument);
            OnToolTipRequest(args);
            if (args.ToolTipShown)
            {
                //Console.WriteLine("Set tooltip to " + args.toolTipText);
                _toolTipActive = true;
                SetToolTip(args.toolTipText, inDocument ? logicPos.Y + 1 : -1);
            }
            else
            {
                CloseToolTip();
            }
        }

        // external interface to the attached event
        internal void RaiseMouseMove(MouseEventArgs e)
        {
            OnMouseMove(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_toolTipRectangle.Contains(e.Location))
            {
                _toolTipRectangle = Rectangle.Empty;

                if (_toolTipActive)
                    RequestToolTip(e.Location);
            }

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

        public void Refresh(IMargin margin)
        {
            _updateMargin = margin;
            Invalidate(_updateMargin.DrawingPosition);
            Update();
            _updateMargin = null;
        }

        protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs pevent)
        {
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

        void DocumentFoldingsChanged(object sender, EventArgs e)
        {
            Caret.UpdateCaretPosition();
            Invalidate();
            MotherTextAreaControl.AdjustScrollBars();
        }

        #region keyboard handling methods
        /// <summary>
        /// This method is called on each Keypress
        /// </summary>
        /// <returns>
        /// True, if the key is handled by this method and should NOT be
        /// inserted in the textarea.
        /// </returns>
        protected internal virtual bool HandleKeyPress(char ch)
        {
            if (KeyEventHandler != null)
            {
                return KeyEventHandler(ch);
            }
            return false;
        }

        // Fixes SD2-747: Form containing the text editor and a button with a shortcut
        protected override bool IsInputChar(char charCode)
        {
            return true;
        }

        internal bool IsReadOnly(int offset)
        {
            if (Document.ReadOnly)
            {
                return true;
            }
            if (Shared.TEP.SupportReadOnlySegments)
            {
                return Document.MarkerStrategy.GetMarkers(offset).Exists(m => m.IsReadOnly);
            }
            else
            {
                return false;
            }
        }

        internal bool IsReadOnly(int offset, int length)
        {
            if (Document.ReadOnly)
            {
                return true;
            }
            if (Shared.TEP.SupportReadOnlySegments)
            {
                return Document.MarkerStrategy.GetMarkers(offset, length).Exists(m => m.IsReadOnly);
            }
            else
            {
                return false;
            }
        }

        public void SimulateKeyPress(char ch)
        {
            if (SelectionManager.HasSomethingSelected)
            {
                if (SelectionManager.SelectionIsReadonly)
                    return;
            }
            else if (IsReadOnly(Caret.Offset))
            {
                return;
            }

            if (ch < ' ')
            {
                return;
            }

            if (!_hiddenMouseCursor && Shared.TEP.HideMouseCursor)
            {
                if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
                {
                    _mouseCursorHidePosition = Cursor.Position;
                    _hiddenMouseCursor = true;
                    Cursor.Hide();
                }
            }
            CloseToolTip();

            BeginUpdate();
            Document.UndoStack.StartUndoGroup();

            try
            {
                // INSERT char
                if (!HandleKeyPress(ch))
                {
                    switch (Caret.CaretMode)
                    {
                        case CaretMode.InsertMode:
                            InsertChar(ch);
                            break;
                        case CaretMode.OverwriteMode:
                            ReplaceChar(ch);
                            break;
                        default:
                            //Debug.Assert(false, "Unknown caret mode " + Caret.CaretMode);
                            break;
                    }
                }

                int currentLineNr = Caret.Line;
                Document.FormattingStrategy.FormatLine(this, currentLineNr, Document.PositionToOffset(Caret.Position), ch);

                EndUpdate();
            }
            finally
            {
                Document.UndoStack.EndUndoGroup();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            SimulateKeyPress(e.KeyChar);
            e.Handled = true;
        }

        /// <summary>
        /// This method executes a dialog key
        /// </summary>
        public bool ExecuteDialogKey(Keys keyData)
        {
            // try, if a dialog key processor was set to use this
            if (DoProcessDialogKey != null && DoProcessDialogKey(keyData))
            {
                return true;
            }

            // if not (or the process was 'silent', use the standard edit actions
            IEditAction action = MotherTextEditorControl.GetEditAction(keyData);
            AutoClearSelection = true;

            if (action != null)
            {
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
                return true;
            }
            return false;
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            return ExecuteDialogKey(keyData) || base.ProcessDialogKey(keyData);
        }
        #endregion

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

        public bool EnableCutOrPaste
        {
            get
            {
                if (MotherTextAreaControl == null)
                    return false;
                if (SelectionManager.HasSomethingSelected)
                    return !SelectionManager.SelectionIsReadonly;
                else
                    return !IsReadOnly(Caret.Offset);
            }
        }

        string GenerateWhitespaceString(int length)
        {
            return new String(' ', length);
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

            // I prefer to set NOT the standard column, if you type something
            //			++Caret.DesiredColumn;
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

                    Document.TextContentChanged -= new EventHandler(TextContentChanged);
                    Document.FoldingManager.FoldingsChanged -= new EventHandler(DocumentFoldingsChanged);
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

        #region UPDATE Commands
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
    }
}
