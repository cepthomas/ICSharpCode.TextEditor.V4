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

        /// <summary>The position where the mouse cursor was when it was hidden. Sometimes the text editor gets MouseMove
        /// events when typing text even if the mouse is not moved. </summary>
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
        static readonly Point NIL_POINT = new Point(-1, -1);
        Point _mouseDownPos = NIL_POINT;
        Point _lastMouseDownPos = NIL_POINT;
        bool _gotMouseDown = false;
        ////////////////////// end From TextAreaMouseHandler////////////////////////////

        ////////////////////// From TextView////////////////////////////
        // original:  split words after 1000 characters. Fixes GDI+ crash on very longs words, for example a 
        // 100 KB Base64-file without any line breaks.
        const int MAX_WORD_LEN = 1000;
        const int MAX_CACHE_SIZE = 2000;

        const int ADDITIONAL_FOLD_TEXT_SIZE = 1;

        const int MIN_TAB_WIDTH = 4;

        // original: Important: Some flags combinations work on WinXP, but not on Win2000. Make sure to test changes here on all operating systems.
        const TextFormatFlags TEXT_FORMAT_FLAGS = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping;

        int _physicalColumn = 0; // used for calculating physical column during paint

        Font _font;

        readonly Dictionary<Font, Dictionary<char, int>> _fontBoundCharWidth = new Dictionary<Font, Dictionary<char, int>>();

        readonly Dictionary<(string word, Font font), int> _measureCache = new Dictionary<(string word, Font font), int>();

        readonly List<(TextMarker marker, RectangleF drawingRect)> _markersToDraw = new List<(TextMarker marker, RectangleF drawingRect)>();


        //public event MarginPaintEventHandler Painted;
        //public event MarginMouseEventHandler MouseDown;
        //public event MarginMouseEventHandler MouseMove;
        //public event EventHandler MouseLeave;

        public Rectangle DrawingPosition { get; set; }

//        public TextArea TextArea { get; }

//        public Cursor Cursor { get; set; } = Cursors.Default;

        //public Size Size { get { return new Size(-1, -1); } }

        //public bool IsVisible { get { return true; } }

        //public Document.Document Document { get { return Document; } }

        public Highlight Highlight { get; set; }

        public int FirstPhysicalLine { get { return VirtualTop.Y / _FontHeight; } }

        public int LineHeightRemainder { get { return VirtualTop.Y % _FontHeight; } }

        /// <summary>Gets the first visible <b>logical</b> line.</summary>
        public int FirstVisibleLine
        {
            get { return Document.GetFirstLogicalLine(VirtualTop.Y / _FontHeight); }
            set { if (FirstVisibleLine != value) VirtualTop = new Point(VirtualTop.X, Document.GetVisibleLine(value) * _FontHeight); }
        }

        public int VisibleLineDrawingRemainder { get { return VirtualTop.Y % _FontHeight; } }

        public int _FontHeight { get; private set; }

        public int VisibleLineCount { get { return 1 + DrawingPosition.Height / _FontHeight; } }

        public int VisibleColumnCount { get { return DrawingPosition.Width / WideSpaceWidth - 1; } }

        /// <summary>
        /// Gets the width of a space character.
        /// This value can be quite small in some fonts - consider using WideSpaceWidth instead.
        /// </summary>
        public int SpaceWidth { get; private set; }

        /// <summary>
        /// Gets the width of a 'wide space' (=one quarter of a tab, if tab is set to 4 spaces).
        /// On monospaced fonts, this is the same value as spaceWidth.
        /// </summary>
        public int WideSpaceWidth { get; private set; }

        //struct MarkerToDraw // (TextMarker marker, RectangleF drawingRect)
        //{
        //    internal TextMarker marker;
        //    internal RectangleF drawingRect;

        //    public MarkerToDraw(TextMarker marker, RectangleF drawingRect)
        //    {
        //        this.marker = marker;
        //        this.drawingRect = drawingRect;
        //    }
        //}

        //struct WordFontPair // (string word, Font font)
        //{
        //    public string word;
        //    public Font font;
        //    public WordFontPair(string word, Font font)
        //    {
        //        this.word = word;
        //        this.font = font;
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        WordFontPair myWordFontPair = (WordFontPair)obj;
        //        if (!word.Equals(myWordFontPair.word))
        //            return false;
        //        return font.Equals(myWordFontPair.font);
        //    }

        //    //public override int GetHashCode()
        //    //{
        //    //    return word.GetHashCode() ^ font.GetHashCode();
        //    //}
        //}


        ////////////////////// end From TextView////////////////////////////



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

//        public TextView TextView { get; }

        public GutterMargin GutterMargin { get; }

        public FoldMargin FoldMargin { get; }

        public IconBarMargin IconBarMargin { get; }

        public Encoding Encoding { get { return MotherTextEditorControl.Encoding; } }

        public int MaxVScrollValue { get { return (Document.GetVisibleLine(Document.TotalNumberOfLines - 1) + 1 + VisibleLineCount * 2 / 3) * _FontHeight; } }

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

//            TextView = new TextView(this);

            GutterMargin = new GutterMargin(this);
            FoldMargin = new FoldMargin(this);
            IconBarMargin = new IconBarMargin(this);
            _leftMargins.AddRange(new IMargin[] { IconBarMargin, GutterMargin, FoldMargin });
            OptionsChanged();

            // From TextAreaMouseHandler mouse handlers:
            Click += MH_Click;
            MouseMove += MH_MouseMove;
            MouseDown += MH_MouseDown;
            DoubleClick += MH_DoubleClick;
            MouseLeave += MH_MouseLeave;
            MouseUp += MH_MouseUp;
            LostFocus += MH_LostFocus;

            _bracketSchemes.Add(new BracketHighlightingSheme('{', '}'));
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
            Caret.DesiredColumn = GetDrawingXPos(Caret.Line, Caret.Column) + VirtualTop.X;
        }

        public void SetCaretToDesiredColumn()
        {
            FoldMarker dummy;
            Caret.Position = GetLogicalColumn(Caret.Line, Caret.DesiredColumn + VirtualTop.X, out dummy);
        }


        public void OptionsChanged()
        {
            UpdateMatchingBracket();
            _font = FontRegistry.GetFont();

            //_FontHeight = GetFontHeight(_lastFont);
            //static int GetFontHeight(Font font)
            //{
            //    int height1 = TextRenderer.MeasureText("_", font).Height;
            //    int height2 = (int)Math.Ceiling(font.GetHeight());
            //    return Math.Max(height1, height2) + 1;
            //}
            int height1 = TextRenderer.MeasureText("_", _font).Height;
            int height2 = (int)Math.Ceiling(_font.GetHeight());
            _FontHeight = Math.Max(height1, height2) + 1;



            // use minimum width - in some fonts, space has no width but kerning is used instead
            // -> DivideByZeroException
            SpaceWidth = Math.Max(GetWidth(' ', _font), 1);
            // tab should have the width of 4*'x'
            WideSpaceWidth = Math.Max(SpaceWidth, GetWidth('x', _font));

            
            //OptionsChanged();
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

            // original:?? I prefer to set NOT the standard column, if you type something ++Caret.DesiredColumn;
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
                Highlight = null;
                return;
            }

            int oldLine1 = -1, oldLine2 = -1;
            if (Highlight != null && Highlight.OpenBrace.Y >= 0 && Highlight.OpenBrace.Y < Document.TotalNumberOfLines)
            {
                oldLine1 = Highlight.OpenBrace.Y;
            }

            if (Highlight != null && Highlight.CloseBrace.Y >= 0 && Highlight.CloseBrace.Y < Document.TotalNumberOfLines)
            {
                oldLine2 = Highlight.CloseBrace.Y;
            }

            Highlight = FindMatchingBracketHighlight();

            if (oldLine1 >= 0)
                UpdateLine(oldLine1);

            if (oldLine2 >= 0 && oldLine2 != oldLine1)
                UpdateLine(oldLine2);

            if (Highlight != null)
            {
                int newLine1 = Highlight.OpenBrace.Y;
                int newLine2 = Highlight.CloseBrace.Y;

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
            //        p.Y = (p.Y - cp.Y) + (lineNumber * FontHeight) - _virtualTop.Y;
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

        //    TextLocation logicPos = GetLogicalPosition(mousePos.X - DrawingPosition.Left, mousePos.Y - DrawingPosition.Top);
        //    bool inDocument = DrawingPosition.Contains(mousePos) && logicPos.Y >= 0 && logicPos.Y < Document.TotalNumberOfLines;
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
            if (textViewArea != DrawingPosition)
            {
                adjustScrollBars = true;
                DrawingPosition = textViewArea;
                // update caret position (but outside of WM_PAINT!)
                BeginInvoke((MethodInvoker)Caret.UpdateCaretPosition);
            }

            if (clipRectangle.IntersectsWith(textViewArea))
            {
                textViewArea.Intersect(clipRectangle);
                if (!textViewArea.IsEmpty)
                {
                    _Paint(g, textViewArea);
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

                    //Dispose();
                    _measureCache.Clear();
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

            if (DrawingPosition.Contains(e.X, e.Y))
            {
                TextLocation realmousepos = GetLogicalPosition(e.X - DrawingPosition.X, e.Y - DrawingPosition.Y);
                if (SelectionManager.IsSelected(Document.PositionToOffset(realmousepos)) && MouseButtons == MouseButtons.None)
                {
                    // mouse is hovering over a selection, so show default mouse
                    Cursor = Cursors.Default;
                }
                else
                {
                    // mouse is hovering over text area, not a selection, so show the textView cursor
                    Cursor = Cursor;
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
            // For example, the CodeCompletionWindow gets focus when it is shown, but immediately gives back focus to the 
            BeginInvoke(new MethodInvoker(ShowHiddenCursorIfMovedOrLeft));
        }

        void MH_Click(object sender, EventArgs e)
        {
            Point mousepos;
            mousepos = MousePos;

            if (_clickedOnSelectedText && DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                SelectionManager.ClearSelection();
                TextLocation clickPosition = GetLogicalPosition(mousepos.X - DrawingPosition.X, mousepos.Y - DrawingPosition.Y);
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

            if (DrawingPosition.Contains(MousePos.X, MousePos.Y))
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
                    FoldMarker marker = GetFoldMarkerFromPosition(MousePos.X - DrawingPosition.X, MousePos.Y - DrawingPosition.Y);
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
                        TextLocation realmousepos = GetLogicalPosition(MousePos.X - DrawingPosition.X, MousePos.Y - DrawingPosition.Y);
                        _clickedOnSelectedText = false;

                        int offset = Document.PositionToOffset(realmousepos);

                        if (SelectionManager.HasSomethingSelected && SelectionManager.IsSelected(offset))
                        {
                            _clickedOnSelectedText = true;
                        }
                        else
                        {
                            SelectionManager.ClearSelection();
                            if (MousePos.Y > 0 && MousePos.Y < DrawingPosition.Height)
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
                    TextLocation realmousepos = GetLogicalPosition(MousePos.X - DrawingPosition.X, MousePos.Y - DrawingPosition.Y);
                    int offset = Document.PositionToOffset(realmousepos);
                    if (!SelectionManager.HasSomethingSelected || !SelectionManager.IsSelected(offset))
                    {
                        SelectionManager.ClearSelection();
                        if (MousePos.Y > 0 && MousePos.Y < DrawingPosition.Height)
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

                try
                {
                    // INSERT char
                    switch (Caret.CaretMode)
                    {
                        case CaretMode.InsertMode:
                            InsertChar(e.KeyChar); //TODO doesn't work for empty docs. Caret goes beyond eol.
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
            int y = Math.Max(0, lineBegin * _FontHeight);
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

        //int FirstPhysicalLine()
        //{
        //    return VirtualTop.Y / _FontHeight;
        //}

        internal void UpdateLines(int xPos, int lineBegin, int lineEnd)
        {
            //			if (lineEnd < FirstPhysicalLine || lineBegin > FirstPhysicalLine + textView.VisibleLineCount) {
            //				return;
            //			}

            InvalidateLines((int)(xPos * WideSpaceWidth), lineBegin, lineEnd);
        }

        void InvalidateLines(int xPos, int lineBegin, int lineEnd)
        {
            lineBegin = Math.Max(Document.GetVisibleLine(lineBegin), FirstPhysicalLine);
            lineEnd = Math.Min(Document.GetVisibleLine(lineEnd), FirstPhysicalLine + VisibleLineCount);
            int y = Math.Max(0, (int)(lineBegin * _FontHeight));
            int height = Math.Min(DrawingPosition.Height, (int)((1 + lineEnd - lineBegin) * (_FontHeight + 1)));

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
            TextLocation realmousepos = GetLogicalPosition(Math.Max(0, mousepos.X - DrawingPosition.X), mousepos.Y - DrawingPosition.Y);
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

            if (DrawingPosition.Contains(mousepos.X, mousepos.Y))
            {
                FoldMarker marker = GetFoldMarkerFromPosition(mousepos.X - DrawingPosition.X, mousepos.Y - DrawingPosition.Y);
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
                                int next = Document.FindNext(Caret.Offset + 1, '"');
                                _minSelection = Caret.Position;
                                if (next > Caret.Offset && next < Document.TextLength)
                                    next += 1;
                                _maxSelection = Document.OffsetToPosition(next);
                            }
                            break;

                        default:
                            _minSelection = Document.OffsetToPosition(Document.FindWordStart(Caret.Offset));
                            _maxSelection = Document.OffsetToPosition(Document.FindWordEnd(Caret.Offset));
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

        ////////////////////// From TextView////////////////////////////

        #region Paint functions
        public void _Paint(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            //// Just to ensure that fontHeight and char widths are always correct...
            //if (_lastFont != FontRegistry.GetFont())
            //{
            //    OptionsChanged();
            //    Invalidate();
            //}

            int horizontalDelta = VirtualTop.X;
            if (horizontalDelta > 0)
            {
                g.SetClip(DrawingPosition);
            }

            for (int y = 0; y < (DrawingPosition.Height + VisibleLineDrawingRemainder) / _FontHeight + 1; ++y)
            {
                Rectangle lineRectangle = new Rectangle(DrawingPosition.X - horizontalDelta, DrawingPosition.Top + y * _FontHeight - VisibleLineDrawingRemainder, DrawingPosition.Width + horizontalDelta, _FontHeight);

                if (rect.IntersectsWith(lineRectangle))
                {
                    int fvl = Document.GetVisibleLine(FirstVisibleLine);
                    int currentLine = Document.GetFirstLogicalLine(Document.GetVisibleLine(FirstVisibleLine) + y);
                    PaintDocumentLine(g, currentLine, lineRectangle); //this redraws everything, twice
                }
            }

            DrawMarkerDraw(g);

            if (horizontalDelta > 0)
            {
                g.ResetClip();
            }

            Caret.PaintCaret(g);
        }

        void PaintDocumentLine(Graphics g, int lineNumber, Rectangle lineRectangle)
        {
            //Debug.Assert(lineNumber >= 0);
            Brush bgColorBrush = GetBgColorBrush(lineNumber);
            Brush backgroundBrush = Enabled ? bgColorBrush : SystemBrushes.InactiveBorder;

            if (lineNumber >= Document.TotalNumberOfLines)
            {
                g.FillRectangle(backgroundBrush, lineRectangle);

                if (Shared.TEP.ShowInvalidLines)
                {
                    DrawInvalidLineMarker(g, lineRectangle.Left, lineRectangle.Top);
                }

                if (Shared.TEP.ShowVerticalRuler)
                {
                    DrawVerticalRuler(g, lineRectangle);
                }

                //              bgColorBrush.Dispose();
                return;
            }

            int physicalXPos = lineRectangle.X;
            int column = 0;
            _physicalColumn = 0;

            // Handle folding.
            if (Shared.TEP.EnableFolding)
            {
                // there can't be a folding wich starts in an above line and ends here, because the line is a new one, there must be a return before this line.
                while (true)
                {
                    List<FoldMarker> starts = Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, column - 1);
                    if (starts == null || starts.Count <= 0)
                    {
                        // No foldings.
                        if (lineNumber < Document.TotalNumberOfLines)
                        {
                            physicalXPos = PaintLinePart(g, lineNumber, column, Document.GetLineSegment(lineNumber).Length, lineRectangle, physicalXPos);
                        }
                        break;
                    }

                    // search the first starting folding
                    FoldMarker firstFolding = (FoldMarker)starts[0];
                    foreach (FoldMarker fm in starts)
                    {
                        if (fm.StartColumn < firstFolding.StartColumn)
                        {
                            firstFolding = fm;
                        }
                    }
                    starts.Clear();

                    physicalXPos = PaintLinePart(g, lineNumber, column, firstFolding.StartColumn, lineRectangle, physicalXPos);
                    column = firstFolding.EndColumn;
                    lineNumber = firstFolding.EndLine;
                    if (lineNumber >= Document.TotalNumberOfLines)
                    {
                        //Debug.Assert(false, "Folding ends after document end");
                        break;
                    }

                    ColumnRange selectionRange2 = SelectionManager.GetSelectionAtLine(lineNumber);
                    bool drawSelected = ColumnRange.WHOLE_COLUMN.Equals(selectionRange2) || firstFolding.StartColumn >= selectionRange2.StartColumn && firstFolding.EndColumn <= selectionRange2.EndColumn;

                    physicalXPos = PaintFoldingText(g, lineNumber, physicalXPos, lineRectangle, firstFolding.FoldText, drawSelected);
                }
            }
            else // simple paint
            {
                physicalXPos = PaintLinePart(g, lineNumber, 0, Document.GetLineSegment(lineNumber).Length, lineRectangle, physicalXPos);
            }

            if (lineNumber < Document.TotalNumberOfLines)
            {
                // Paint things after end of line
                LineSegment currentLine = Document.GetLineSegment(lineNumber);
                HighlightColor selectionColor = Shared.TEP.SelectionColor;
                ColumnRange selectionRange = SelectionManager.GetSelectionAtLine(lineNumber);

                bool selectionBeyondEOL = selectionRange.EndColumn > currentLine.Length || ColumnRange.WHOLE_COLUMN.Equals(selectionRange);

                if (Shared.TEP.ShowEOLMarker)
                {
                    HighlightColor eolMarkerColor = Shared.TEP.EOLMarkersColor;
                    physicalXPos += DrawEOLMarker(g, eolMarkerColor.Color, selectionBeyondEOL ? bgColorBrush : backgroundBrush, physicalXPos, lineRectangle.Y);
                }
                else
                {
                    if (selectionBeyondEOL)
                    {
                        g.FillRectangle(BrushRegistry.GetBrush(selectionColor.BackgroundColor), new RectangleF(physicalXPos, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
                        physicalXPos += WideSpaceWidth;
                    }
                }

                Brush fillBrush = selectionBeyondEOL && Shared.TEP.AllowCaretBeyondEOL ? bgColorBrush : backgroundBrush;
                g.FillRectangle(fillBrush, new RectangleF(physicalXPos, lineRectangle.Y, lineRectangle.Width - physicalXPos + lineRectangle.X, lineRectangle.Height));
            }

            if (Shared.TEP.ShowVerticalRuler)
            {
                DrawVerticalRuler(g, lineRectangle);
            }
            //          bgColorBrush.Dispose();
        }

        bool DrawLineMarkerAtLine(int lineNumber)
        {
            return lineNumber == Caret.Line && Shared.TEP.LineViewerStyle == LineViewerStyle.FullRow;
        }

        Brush GetBgColorBrush(int lineNumber)
        {
            if (DrawLineMarkerAtLine(lineNumber))
            {
                HighlightColor caretLine = Shared.TEP.CaretMarkerColor;
                return BrushRegistry.GetBrush(caretLine.Color);
            }

            HighlightColor background = Shared.TEP.DefaultColor;
            Color bgColor = background.BackgroundColor;
            return BrushRegistry.GetBrush(bgColor);
        }

        int PaintFoldingText(Graphics g, int lineNumber, int physicalXPos, Rectangle lineRectangle, string text, bool drawSelected)
        {
            HighlightColor selectionColor = Shared.TEP.SelectionColor;
            Brush bgColorBrush = drawSelected ? BrushRegistry.GetBrush(selectionColor.BackgroundColor) : GetBgColorBrush(lineNumber);
            Brush backgroundBrush = Enabled ? bgColorBrush : SystemBrushes.InactiveBorder;

            Font font = FontRegistry.GetFont();

            int wordWidth = MeasureStringWidth(g, text, font) + ADDITIONAL_FOLD_TEXT_SIZE;
            Rectangle rect = new Rectangle(physicalXPos, lineRectangle.Y, wordWidth, lineRectangle.Height - 1);

            g.FillRectangle(backgroundBrush, rect);

            _physicalColumn += text.Length;
            DrawString(g, text, font, drawSelected ? selectionColor.Color : Color.Gray, rect.X + 1, rect.Y);
            g.DrawRectangle(BrushRegistry.GetPen(drawSelected ? Color.DarkGray : Color.Gray), rect.X, rect.Y, rect.Width, rect.Height);

            return physicalXPos + wordWidth + 1;
        }

        void DrawMarker(Graphics g, TextMarker marker, RectangleF drawingRect)
        {
            // draw markers later so they can overdraw the following text
            _markersToDraw.Add((marker, drawingRect));
        }

        void DrawMarkerDraw(Graphics g)
        {
            foreach (var m in _markersToDraw)
            {
                TextMarker marker = m.marker;
                RectangleF drawingRect = m.drawingRect;
                float drawYPos = drawingRect.Bottom - 1;
                switch (marker.TextMarkerType)
                {
                    case TextMarkerType.Underlined:
                        g.DrawLine(BrushRegistry.GetPen(marker.Color), drawingRect.X, drawYPos, drawingRect.Right, drawYPos);
                        break;
                    case TextMarkerType.WaveLine:
                        int reminder = ((int)drawingRect.X) % 6;
                        for (float i = (int)drawingRect.X - reminder; i < drawingRect.Right; i += 6)
                        {
                            g.DrawLine(BrushRegistry.GetPen(marker.Color), i, drawYPos + 3 - 4, i + 3, drawYPos + 1 - 4);
                            if (i + 3 < drawingRect.Right)
                            {
                                g.DrawLine(BrushRegistry.GetPen(marker.Color), i + 3, drawYPos + 1 - 4, i + 6, drawYPos + 3 - 4);
                            }
                        }
                        break;
                    case TextMarkerType.SolidBlock:
                        g.FillRectangle(BrushRegistry.GetBrush(marker.Color), drawingRect);
                        break;
                }
            }
            _markersToDraw.Clear();
        }

        /// <summary>Get the marker brush (for solid block markers) at a given position.</summary>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="markers">All markers that have been found.</param>
        /// <returns>The Brush or null when no marker was found.</returns>
        Brush GetMarkerBrushAt(int offset, int length, ref Color foreColor, out IList<TextMarker> markers)
        {
            markers = Document.MarkerStrategy.GetMarkers(offset, length);
            foreach (TextMarker marker in markers)
            {
                if (marker.TextMarkerType == TextMarkerType.SolidBlock)
                {
                    if (marker.OverrideForeColor)
                    {
                        foreColor = marker.ForeColor;
                    }
                    return BrushRegistry.GetBrush(marker.Color);
                }
            }
            return null;
        }

        int PaintLinePart(Graphics g, int lineNumber, int startColumn, int endColumn, Rectangle lineRectangle, int physicalXPos)
        {
            bool drawLineMarker = DrawLineMarkerAtLine(lineNumber);
            Brush backgroundBrush = Enabled ? GetBgColorBrush(lineNumber) : SystemBrushes.InactiveBorder;

            HighlightColor selectionColor = Shared.TEP.SelectionColor;
            ColumnRange selectionRange = SelectionManager.GetSelectionAtLine(lineNumber);
            HighlightColor tabMarkerColor = Shared.TEP.TabMarkersColor;
            HighlightColor spaceMarkerColor = Shared.TEP.SpaceMarkersColor;

            LineSegment currentLine = Document.GetLineSegment(lineNumber);

            Brush selectionBackgroundBrush = BrushRegistry.GetBrush(selectionColor.BackgroundColor);

            if (currentLine.Words == null)
            {
                return physicalXPos;
            }

            int currentWordOffset = 0; // we cannot use currentWord.Offset because it is not set on space words
            TextWord currentWord;
            TextWord nextCurrentWord = null;

            for (int wordIdx = 0; wordIdx < currentLine.Words.Count; wordIdx++)
            {
                currentWord = currentLine.Words[wordIdx];
                if (currentWordOffset < startColumn)
                {
                    // original: maybe we need to split at startColumn when we support fold markers inside words
                    currentWordOffset += currentWord.Length;
                    continue;
                }

            repeatDrawCurrentWord:
                //physicalXPos += 10; // leave room between drawn words - useful for debugging the drawing code
                if (currentWordOffset >= endColumn || physicalXPos >= lineRectangle.Right)
                {
                    break;
                }

                int currentWordEndOffset = currentWordOffset + currentWord.Length - 1;
                TextWordType currentWordType = currentWord.Type;

                IList<TextMarker> markers;
                Color wordForeColor;
                if (currentWordType == TextWordType.Space)
                    wordForeColor = spaceMarkerColor.Color;
                else if (currentWordType == TextWordType.Tab)
                    wordForeColor = tabMarkerColor.Color;
                else
                    wordForeColor = currentWord.Color;

                Brush wordBackBrush = GetMarkerBrushAt(currentLine.Offset + currentWordOffset, currentWord.Length, ref wordForeColor, out markers);

                // It is possible that we have to split the current word because a marker/the selection begins/ends inside it
                if (currentWord.Length > 1)
                {
                    int splitPos = int.MaxValue;
                    if (Highlight != null)
                    {
                        // split both before and after highlight
                        if (Highlight.OpenBrace.Y == lineNumber)
                        {
                            if (Highlight.OpenBrace.X >= currentWordOffset && Highlight.OpenBrace.X <= currentWordEndOffset)
                            {
                                splitPos = Math.Min(splitPos, Highlight.OpenBrace.X - currentWordOffset);
                            }
                        }

                        if (Highlight.CloseBrace.Y == lineNumber)
                        {
                            if (Highlight.CloseBrace.X >= currentWordOffset && Highlight.CloseBrace.X <= currentWordEndOffset)
                            {
                                splitPos = Math.Min(splitPos, Highlight.CloseBrace.X - currentWordOffset);
                            }
                        }

                        if (splitPos == 0)
                        {
                            splitPos = 1; // split after highlight
                        }
                    }

                    if (endColumn < currentWordEndOffset)   // split when endColumn is reached
                    {
                        splitPos = Math.Min(splitPos, endColumn - currentWordOffset);
                    }

                    if (selectionRange.StartColumn > currentWordOffset && selectionRange.StartColumn <= currentWordEndOffset)
                    {
                        splitPos = Math.Min(splitPos, selectionRange.StartColumn - currentWordOffset);
                    }
                    else if (selectionRange.EndColumn > currentWordOffset && selectionRange.EndColumn <= currentWordEndOffset)
                    {
                        splitPos = Math.Min(splitPos, selectionRange.EndColumn - currentWordOffset);
                    }

                    foreach (TextMarker marker in markers)
                    {
                        int markerColumn = marker.Offset - currentLine.Offset;
                        int markerEndColumn = marker.EndOffset - currentLine.Offset + 1; // make end offset exclusive
                        if (markerColumn > currentWordOffset && markerColumn <= currentWordEndOffset)
                        {
                            splitPos = Math.Min(splitPos, markerColumn - currentWordOffset);
                        }
                        else if (markerEndColumn > currentWordOffset && markerEndColumn <= currentWordEndOffset)
                        {
                            splitPos = Math.Min(splitPos, markerEndColumn - currentWordOffset);
                        }
                    }

                    if (splitPos != int.MaxValue)
                    {
                        if (nextCurrentWord != null)
                            throw new ApplicationException("split part invalid: first part cannot be splitted further");
                        nextCurrentWord = TextWord.Split(ref currentWord, splitPos);
                        goto repeatDrawCurrentWord; // get markers for first word part
                    }
                }

                // get colors from selection status:
                if (ColumnRange.WHOLE_COLUMN.Equals(selectionRange) || (selectionRange.StartColumn <= currentWordOffset && selectionRange.EndColumn > currentWordEndOffset))
                {
                    // word is completely selected 
                    //Debug.WriteLine("YYY :{0}:{1}:{2}:{3}", selectionRange.StartColumn, currentWordOffset, selectionRange.EndColumn, currentWordEndOffset);

                    wordBackBrush = selectionBackgroundBrush;
                    if (selectionColor.HasForeground)
                    {
                        wordForeColor = selectionColor.Color;
                    }
                }
                else if (drawLineMarker)
                {
                    wordBackBrush = backgroundBrush;
                }

                if (wordBackBrush == null)   // use default background if no other background is set
                {
                    if (currentWord.SyntaxColor != null && currentWord.SyntaxColor.HasBackground)
                        wordBackBrush = BrushRegistry.GetBrush(currentWord.SyntaxColor.BackgroundColor);
                    else
                        wordBackBrush = backgroundBrush;
                }

                RectangleF wordRectangle;

                if (currentWord.Type == TextWordType.Space)
                {
                    ++_physicalColumn;

                    wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, SpaceWidth, lineRectangle.Height);
                    g.FillRectangle(wordBackBrush, wordRectangle);

                    if (Shared.TEP.ShowSpaces)
                    {
                        DrawSpaceMarker(g, wordForeColor, physicalXPos, lineRectangle.Y);
                    }
                    physicalXPos += SpaceWidth;
                }
                else if (currentWord.Type == TextWordType.Tab)
                {

                    _physicalColumn += Shared.TEP.TabIndent;
                    _physicalColumn = (_physicalColumn / Shared.TEP.TabIndent) * Shared.TEP.TabIndent;
                    // go to next tabstop
                    int physicalTabEnd = ((physicalXPos + MIN_TAB_WIDTH - lineRectangle.X)
                                          / WideSpaceWidth / Shared.TEP.TabIndent)
                                         * WideSpaceWidth * Shared.TEP.TabIndent + lineRectangle.X;
                    physicalTabEnd += WideSpaceWidth * Shared.TEP.TabIndent;

                    wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, physicalTabEnd - physicalXPos, lineRectangle.Height);
                    g.FillRectangle(wordBackBrush, wordRectangle);

                    if (Shared.TEP.ShowTabs)
                    {
                        DrawTabMarker(g, wordForeColor, physicalXPos, lineRectangle.Y);
                    }
                    physicalXPos = physicalTabEnd;
                }
                else
                {
                    int wordWidth = DrawDocumentWord(g, currentWord.Word, new Point(physicalXPos, lineRectangle.Y), currentWord.GetFont(), wordForeColor, wordBackBrush);
                    wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, wordWidth, lineRectangle.Height);
                    physicalXPos += wordWidth;
                }

                foreach (TextMarker marker in markers)
                {
                    if (marker.TextMarkerType != TextMarkerType.SolidBlock)
                    {
                        DrawMarker(g, marker, wordRectangle);
                    }
                }

                // draw bracket highlight
                if (Highlight != null)
                {
                    if (Highlight.OpenBrace.Y == lineNumber && Highlight.OpenBrace.X == currentWordOffset || Highlight.CloseBrace.Y == lineNumber && Highlight.CloseBrace.X == currentWordOffset)
                    {
                        DrawBracketHighlight(g, new Rectangle((int)wordRectangle.X, lineRectangle.Y, (int)wordRectangle.Width - 1, lineRectangle.Height - 1));
                    }
                }

                currentWordOffset += currentWord.Length;

                if (nextCurrentWord != null)
                {
                    currentWord = nextCurrentWord;
                    nextCurrentWord = null;
                    goto repeatDrawCurrentWord;
                }
            }

            if (physicalXPos < lineRectangle.Right && endColumn >= currentLine.Length)
            {
                // draw markers at line end
                IList<TextMarker> markers = Document.MarkerStrategy.GetMarkers(currentLine.Offset + currentLine.Length);
                foreach (TextMarker marker in markers)
                {
                    if (marker.TextMarkerType != TextMarkerType.SolidBlock)
                    {
                        DrawMarker(g, marker, new RectangleF(physicalXPos, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
                    }
                }
            }

            return physicalXPos;
        }

        int DrawDocumentWord(Graphics g, string word, Point position, Font font, Color foreColor, Brush backBrush)
        {
            if (word == null || word.Length == 0)
            {
                return 0;
            }

            if (word.Length > MAX_WORD_LEN)
            {
                int width = 0;
                for (int i = 0; i < word.Length; i += MAX_WORD_LEN)
                {
                    Point pos = position;
                    pos.X += width;
                    if (i + MAX_WORD_LEN < word.Length)
                        width += DrawDocumentWord(g, word.Substring(i, MAX_WORD_LEN), pos, font, foreColor, backBrush);
                    else
                        width += DrawDocumentWord(g, word.Substring(i, word.Length - i), pos, font, foreColor, backBrush);
                }
                return width;
            }

            int wordWidth = MeasureStringWidth(g, word, font);

            //num = ++num % 3;
            //g.FillRectangle(backBrush, //num == 0 ? Brushes.LightBlue : num == 1 ? Brushes.LightGreen : Brushes.Yellow,
            //                new RectangleF(position.X, position.Y, wordWidth + 1, _FontHeight));
            g.FillRectangle(backBrush, new RectangleF(position.X, position.Y, wordWidth + 1, _FontHeight));

            DrawString(g, word, font, foreColor, position.X, position.Y);
            return wordWidth;
        }

        int MeasureStringWidth(Graphics g, string word, Font font)
        {
            int width;

            if (word == null || word.Length == 0)
                return 0;

            if (word.Length > MAX_WORD_LEN)
            {
                width = 0;
                for (int i = 0; i < word.Length; i += MAX_WORD_LEN)
                {
                    if (i + MAX_WORD_LEN < word.Length)
                        width += MeasureStringWidth(g, word.Substring(i, MAX_WORD_LEN), font);
                    else
                        width += MeasureStringWidth(g, word.Substring(i, word.Length - i), font);
                }
                return width;
            }

            if (_measureCache.TryGetValue((word, font), out width))
            {
                return width;
            }

            if (_measureCache.Count > MAX_CACHE_SIZE)
            {
                _measureCache.Clear();
            }

            // This code here provides better results than MeasureString!
            // Example line that is measured wrong:
            // txt.GetPositionFromCharIndex(txt.SelectionStart)
            // (Verdana 10, highlighting makes GetP... bold) -> note the space between 'x' and '('
            // this also fixes "jumping" characters when selecting in non-monospace fonts
            // [...]
            // Replaced GDI+ measurement with GDI measurement: faster and even more exact
            width = TextRenderer.MeasureText(g, word, font, new Size(short.MaxValue, short.MaxValue), TEXT_FORMAT_FLAGS).Width;
            _measureCache.Add((word, font), width);
            return width;
        }

        #endregion

        #region Conversion Functions
        public int GetWidth(char ch, Font font)
        {
            if (!_fontBoundCharWidth.ContainsKey(font))
            {
                _fontBoundCharWidth.Add(font, new Dictionary<char, int>());
            }

            if (!_fontBoundCharWidth[font].ContainsKey(ch))
            {
                using (Graphics g = CreateGraphics())
                {
                    return GetWidth(g, ch, font);
                }
            }

            return _fontBoundCharWidth[font][ch];
        }

        public int GetWidth(Graphics g, char ch, Font font)
        {
            if (!_fontBoundCharWidth.ContainsKey(font))
            {
                _fontBoundCharWidth.Add(font, new Dictionary<char, int>());
            }

            if (!_fontBoundCharWidth[font].ContainsKey(ch))
            {
                //Console.WriteLine("Calculate character width: " + ch);
                _fontBoundCharWidth[font].Add(ch, MeasureStringWidth(g, ch.ToString(), font));
            }

            return _fontBoundCharWidth[font][ch];
        }

        public int GetVisualColumn(int logicalLine, int logicalColumn)
        {
            int column = 0;
            using (Graphics g = CreateGraphics())
            {
                CountColumns(ref column, 0, logicalColumn, logicalLine, g);
            }

            return column;
        }

        public int GetVisualColumnFast(LineSegment line, int logicalColumn)
        {
            int lineOffset = line.Offset;
            int tabIndent = Shared.TEP.TabIndent;
            int guessedColumn = 0;

            for (int i = 0; i < logicalColumn; ++i)
            {
                char ch;
                if (i >= line.Length)
                {
                    ch = ' ';
                }
                else
                {
                    ch = Document.GetCharAt(lineOffset + i);
                }

                switch (ch)
                {
                    case '\t':
                        guessedColumn += tabIndent;
                        guessedColumn = (guessedColumn / tabIndent) * tabIndent;
                        break;

                    default:
                        ++guessedColumn;
                        break;
                }
            }
            return guessedColumn;
        }

        /// <summary>
        /// returns line/column for a visual point position
        /// </summary>
        public TextLocation GetLogicalPosition(Point mousePosition)
        {
            return GetLogicalColumn(GetLogicalLine(mousePosition.Y), mousePosition.X, out FoldMarker dummy);
        }

        /// <summary>
        /// returns line/column for a visual point position
        /// </summary>
        public TextLocation GetLogicalPosition(int visualPosX, int visualPosY)
        {
            return GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out FoldMarker dummy);
        }

        /// <summary>
        /// returns line/column for a visual point position
        /// </summary>
        public FoldMarker GetFoldMarkerFromPosition(int visualPosX, int visualPosY)
        {
            GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out FoldMarker foldMarker);
            return foldMarker;
        }

        /// <summary>
        /// returns logical line number for a visual point
        /// </summary>
        public int GetLogicalLine(int visualPosY)
        {
            int clickedVisualLine = Math.Max(0, (visualPosY + VirtualTop.Y) / _FontHeight);
            return Document.GetFirstLogicalLine(clickedVisualLine);
        }

        internal TextLocation GetLogicalColumn(int lineNumber, int visualPosX, out FoldMarker inFoldMarker)
        {
            visualPosX += VirtualTop.X;

            inFoldMarker = null;
            if (lineNumber >= Document.TotalNumberOfLines)
            {
                return new TextLocation((int)(visualPosX / WideSpaceWidth), lineNumber);
            }

            if (visualPosX <= 0)
            {
                return new TextLocation(0, lineNumber);
            }

            int start = 0; // column
            int posX = 0; // visual position

            int result;
            using (Graphics g = CreateGraphics())
            {
                // call GetLogicalColumnInternal to skip over text,
                // then skip over fold markers
                // and repeat as necessary.
                // The loop terminates once the correct logical column is reached in
                // GetLogicalColumnInternal or inside a fold marker.
                while (true)
                {
                    LineSegment line = Document.GetLineSegment(lineNumber);
                    FoldMarker nextFolding = FindNextFoldedFoldingOnLineAfterColumn(lineNumber, start - 1);
                    int end = nextFolding != null ? nextFolding.StartColumn : int.MaxValue;
                    result = GetLogicalColumnInternal(g, line, start, end, ref posX, visualPosX);

                    // break when GetLogicalColumnInternal found the result column
                    if (result < end)
                        break;

                    // reached fold marker
                    lineNumber = nextFolding.EndLine;
                    start = nextFolding.EndColumn;
                    int newPosX = posX + 1 + MeasureStringWidth(g, nextFolding.FoldText, FontRegistry.GetFont());
                    if (newPosX >= visualPosX)
                    {
                        inFoldMarker = nextFolding;
                        if (IsNearerToAThanB(visualPosX, posX, newPosX))
                            return new TextLocation(nextFolding.StartColumn, nextFolding.StartLine);
                        else
                            return new TextLocation(nextFolding.EndColumn, nextFolding.EndLine);
                    }

                    posX = newPosX;
                }
            }

            return new TextLocation(result, lineNumber);
        }

        int GetLogicalColumnInternal(Graphics g, LineSegment line, int start, int end, ref int drawingPos, int targetVisualPosX)
        {
            if (start == end)
                return end;
            //Debug.Assert(start < end);
            //Debug.Assert(drawingPos < targetVisualPosX);

            int tabIndent = Shared.TEP.TabIndent;

            /*float spaceWidth = SpaceWidth;
            float drawingPos = 0;
            LineSegment currentLine = Document.GetLineSegment(logicalLine);
            List<TextWord> words = currentLine.Words;
            if (words == null) return 0;
            int wordCount = words.Count;
            int wordOffset = 0;
            FontContainer fontContainer = TextEditorProperties.FontContainer;
             */


            List<TextWord> words = line.Words;
            if (words == null) return 0;
            int wordOffset = 0;
            for (int i = 0; i < words.Count; i++)
            {
                TextWord word = words[i];
                if (wordOffset >= end)
                {
                    return wordOffset;
                }

                if (wordOffset + word.Length >= start)
                {
                    int newDrawingPos;
                    switch (word.Type)
                    {
                        case TextWordType.Space:
                            newDrawingPos = drawingPos + SpaceWidth;
                            if (newDrawingPos >= targetVisualPosX)
                                return IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos) ? wordOffset : wordOffset + 1;
                            break;

                        case TextWordType.Tab:
                            // go to next tab position
                            drawingPos = (int)((drawingPos + MIN_TAB_WIDTH) / tabIndent / WideSpaceWidth) * tabIndent * WideSpaceWidth;
                            newDrawingPos = drawingPos + tabIndent * WideSpaceWidth;
                            if (newDrawingPos >= targetVisualPosX)
                                return IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos) ? wordOffset : wordOffset + 1;
                            break;

                        case TextWordType.Word:
                            int wordStart = Math.Max(wordOffset, start);
                            int wordLength = Math.Min(wordOffset + word.Length, end) - wordStart;
                            string text = Document.GetText(line.Offset + wordStart, wordLength);
                            Font font = word.GetFont() ?? FontRegistry.GetFont();
                            newDrawingPos = drawingPos + MeasureStringWidth(g, text, font);
                            if (newDrawingPos >= targetVisualPosX)
                            {
                                for (int j = 0; j < text.Length; j++)
                                {
                                    newDrawingPos = drawingPos + MeasureStringWidth(g, text[j].ToString(), font);
                                    if (newDrawingPos >= targetVisualPosX)
                                    {
                                        if (IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos))
                                            return wordStart + j;
                                        else
                                            return wordStart + j + 1;
                                    }
                                    drawingPos = newDrawingPos;
                                }
                                return wordStart + text.Length;
                            }
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    drawingPos = newDrawingPos;
                }
                wordOffset += word.Length;
            }
            return wordOffset;
        }

        static bool IsNearerToAThanB(int num, int a, int b)
        {
            return Math.Abs(a - num) < Math.Abs(b - num);
        }

        FoldMarker FindNextFoldedFoldingOnLineAfterColumn(int lineNumber, int column)
        {
            List<FoldMarker> list = Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, column);
            if (list.Count != 0)
                return list[0];
            else
                return null;
        }

        float CountColumns(ref int column, int start, int end, int logicalLine, Graphics g)
        {
            if (start > end)
                throw new ArgumentException("start > end");
            if (start == end)
                return 0;

            float spaceWidth = SpaceWidth;
            float drawingPos = 0;
            int tabIndent = Shared.TEP.TabIndent;
            LineSegment currentLine = Document.GetLineSegment(logicalLine);
            List<TextWord> words = currentLine.Words;

            if (words == null)
                return 0;

            int wordCount = words.Count;
            int wordOffset = 0;

            for (int i = 0; i < wordCount; i++)
            {
                TextWord word = words[i];
                if (wordOffset >= end)
                    break;
                if (wordOffset + word.Length >= start)
                {
                    switch (word.Type)
                    {
                        case TextWordType.Space:
                            drawingPos += spaceWidth;
                            break;

                        case TextWordType.Tab:
                            // go to next tab position
                            drawingPos = (int)((drawingPos + MIN_TAB_WIDTH) / tabIndent / WideSpaceWidth) * tabIndent * WideSpaceWidth;
                            drawingPos += tabIndent * WideSpaceWidth;
                            break;

                        case TextWordType.Word:
                            int wordStart = Math.Max(wordOffset, start);
                            int wordLength = Math.Min(wordOffset + word.Length, end) - wordStart;
                            string text = Document.GetText(currentLine.Offset + wordStart, wordLength);
                            drawingPos += MeasureStringWidth(g, text, word.GetFont() ?? FontRegistry.GetFont());
                            break;
                    }
                }
                wordOffset += word.Length;
            }

            for (int j = currentLine.Length; j < end; j++)
            {
                drawingPos += WideSpaceWidth;
            }

            // add one pixel in column calculation to account for floating point calculation errors
            column += (int)((drawingPos + 1) / WideSpaceWidth);

            /* original: OLD Code (does not work for fonts like Verdana)
            for (int j = start; j < end; ++j) {
                char ch;
                if (j >= line.Length) {
                    ch = ' ';
                } else {
                    ch = Document.GetCharAt(line.Offset + j);
                }

                switch (ch) {
                    case '\t':
                        int oldColumn = column;
                        column += tabIndent;
                        column = (column / tabIndent) * tabIndent;
                        drawingPos += (column - oldColumn) * spaceWidth;
                        break;
                    default:
                        ++column;
                        TextWord word = line.GetWord(j);
                        if (word == null || word.Font == null) {
                            drawingPos += GetWidth(ch, TextEditorProperties.Font);
                        } else {
                            drawingPos += GetWidth(ch, word.Font);
                        }
                        break;
                }
            }
            */

            return drawingPos;
        }

        public int GetDrawingXPos(int logicalLine, int logicalColumn)
        {
            List<FoldMarker> foldings = Document.FoldingManager.GetTopLevelFoldedFoldings();
            int i;
            FoldMarker f = null;

            // search the last folding that's interresting
            for (i = foldings.Count - 1; i >= 0; --i)
            {
                f = foldings[i];
                if (f.StartLine < logicalLine || f.StartLine == logicalLine && f.StartColumn < logicalColumn)
                {
                    break;
                }

                FoldMarker f2 = foldings[i / 2];
                if (f2.StartLine > logicalLine || f2.StartLine == logicalLine && f2.StartColumn >= logicalColumn)
                {
                    i /= 2;
                }
            }

            int lastFolding = 0;
            int firstFolding = 0;
            int column = 0;
            int tabIndent = Shared.TEP.TabIndent;
            float drawingPos;
            Graphics g = CreateGraphics();

            // if no folding is interresting
            if (f == null || !(f.StartLine < logicalLine || f.StartLine == logicalLine && f.StartColumn < logicalColumn))
            {
                drawingPos = CountColumns(ref column, 0, logicalColumn, logicalLine, g);
                return (int)(drawingPos - VirtualTop.X);
            }

            // if logicalLine/logicalColumn is in folding
            if (f.EndLine > logicalLine || f.EndLine == logicalLine && f.EndColumn > logicalColumn)
            {
                logicalColumn = f.StartColumn;
                logicalLine = f.StartLine;
                --i;
            }
            lastFolding = i;

            // search backwards until a new visible line is reched
            for (; i >= 0; --i)
            {
                f = foldings[i];
                if (f.EndLine < logicalLine)   // reached the begin of a new visible line
                {
                    break;
                }
            }

            firstFolding = i + 1;

            if (lastFolding < firstFolding)
            {
                drawingPos = CountColumns(ref column, 0, logicalColumn, logicalLine, g);
                return (int)(drawingPos - VirtualTop.X);
            }

            int foldEnd = 0;
            drawingPos = 0;

            for (i = firstFolding; i <= lastFolding; ++i)
            {
                f = foldings[i];
                drawingPos += CountColumns(ref column, foldEnd, f.StartColumn, f.StartLine, g);
                foldEnd = f.EndColumn;
                column += f.FoldText.Length;
                drawingPos += ADDITIONAL_FOLD_TEXT_SIZE;
                drawingPos += MeasureStringWidth(g, f.FoldText, FontRegistry.GetFont());
            }

            drawingPos += CountColumns(ref column, foldEnd, logicalColumn, logicalLine, g);
            g.Dispose();
            return (int)(drawingPos - VirtualTop.X);
        }
        #endregion

        #region DrawHelper functions
        void DrawBracketHighlight(Graphics g, Rectangle rect)
        {
            g.FillRectangle(BrushRegistry.GetBrush(Color.FromArgb(50, 0, 0, 255)), rect);
            g.DrawRectangle(Pens.Blue, rect);
        }

        void DrawString(Graphics g, string text, Font font, Color color, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), color, TEXT_FORMAT_FLAGS);
        }

        void DrawInvalidLineMarker(Graphics g, int x, int y)
        {
            HighlightColor invalidLinesColor = Shared.TEP.InvalidLinesColor;
            DrawString(g, "~", FontRegistry.GetFont(invalidLinesColor.Bold, invalidLinesColor.Italic), invalidLinesColor.Color, x, y);
        }

        void DrawSpaceMarker(Graphics g, Color color, int x, int y)
        {
            HighlightColor spaceMarkerColor = Shared.TEP.SpaceMarkersColor;
            DrawString(g, "\u00B7", FontRegistry.GetFont(spaceMarkerColor.Bold, spaceMarkerColor.Italic), color, x, y);
        }

        void DrawTabMarker(Graphics g, Color color, int x, int y)
        {
            HighlightColor tabMarkerColor = Shared.TEP.TabMarkersColor;
            DrawString(g, "\u00BB", FontRegistry.GetFont(tabMarkerColor.Bold, tabMarkerColor.Italic), color, x, y);
        }

        int DrawEOLMarker(Graphics g, Color color, Brush backBrush, int x, int y)
        {
            HighlightColor eolMarkerColor = Shared.TEP.EOLMarkersColor;

            int width = GetWidth('\u00B6', FontRegistry.GetFont(eolMarkerColor.Bold, eolMarkerColor.Italic));
            g.FillRectangle(backBrush, new RectangleF(x, y, width, _FontHeight));

            DrawString(g, "\u00B6", FontRegistry.GetFont(eolMarkerColor.Bold, eolMarkerColor.Italic), color, x, y);
            return width;
        }

        void DrawVerticalRuler(Graphics g, Rectangle lineRectangle)
        {
            int xpos = WideSpaceWidth * Shared.TEP.VerticalRulerRow - VirtualTop.X;
            if (xpos <= 0)
            {
                return;
            }

            HighlightColor vRulerColor = Shared.TEP.VRulerColor;
            g.DrawLine(BrushRegistry.GetPen(vRulerColor.Color), DrawingPosition.Left + xpos, lineRectangle.Top, DrawingPosition.Left + xpos, lineRectangle.Bottom);
        }
        #endregion



        // not used?
        //public virtual void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
        //{
        //    MouseDown?.Invoke(this, mousepos, mouseButtons);
        //}
        //public void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
        //{
        //    MouseMove?.Invoke(this, mousepos, mouseButtons);
        //}
        //public void HandleMouseLeave(EventArgs e)
        //{
        //    MouseLeave?.Invoke(this, e);
        //}




        ////////////////////// end From TextView////////////////////////////

    }
}
