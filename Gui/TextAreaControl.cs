// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// This class paints the textarea. plus scrollbars etc.
    /// </summary>
    [ToolboxItem(false)]
    public class TextAreaControl : Panel
    {
        const int LL_CACHE_ADD_SIZE = 100;

        TextEditorControl _motherTextEditorControl;

        HRuler _hRuler = null;

        bool _disposed;

        int[] _lineLengthCache;
        readonly int _scrollMarginHeight = 3;

        bool _adjustScrollBarsOnNextUpdate;

        Point _scrollToPosOnNextUpdate;

        readonly Util.MouseWheelHandler _mouseWheelHandler = new Util.MouseWheelHandler();


        public TextArea TextArea { get; }

        public SelectionManager SelectionManager { get { return TextArea.SelectionManager; } }

        public Caret Caret { get { return TextArea.Caret; } }

        [Browsable(false)]
        public Document.Document Document { get { return _motherTextEditorControl?.Document; } }

        public VScrollBar VScrollBar { get; private set; } = new VScrollBar();

        public HScrollBar HScrollBar { get; private set; } = new HScrollBar();

        public bool DoHandleMousewheel { get; set; } = true;

        public TextAreaControl(TextEditorControl motherTextEditorControl)
        {
            _motherTextEditorControl = motherTextEditorControl;

            TextArea = new TextArea(motherTextEditorControl, this);
            Controls.Add(TextArea);

            VScrollBar.ValueChanged += new EventHandler(VScrollBarValueChanged);
            Controls.Add(VScrollBar);

            HScrollBar.ValueChanged += new EventHandler(HScrollBarValueChanged);
            Controls.Add(HScrollBar);
            ResizeRedraw = true;

            Document.TextContentChanged += DocumentTextContentChanged;
            Document.DocumentChanged += AdjustScrollBarsOnDocumentChange;
            Document.UpdateCommited += DocumentUpdateCommitted;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Document.TextContentChanged -= DocumentTextContentChanged;
                    Document.DocumentChanged -= AdjustScrollBarsOnDocumentChange;
                    Document.UpdateCommited -= DocumentUpdateCommitted;
                    _motherTextEditorControl = null;
                    if (VScrollBar != null)
                    {
                        VScrollBar.Dispose();
                        VScrollBar = null;
                    }
                    if (HScrollBar != null)
                    {
                        HScrollBar.Dispose();
                        HScrollBar = null;
                    }
                    if (_hRuler != null)
                    {
                        _hRuler.Dispose();
                        _hRuler = null;
                    }
                }
            }
            base.Dispose(disposing);
        }

        void DocumentTextContentChanged(object sender, EventArgs e)
        {
            // after the text content is changed abruptly, we need to validate the
            // caret position - otherwise the caret position is invalid for a short amount
            // of time, which can break client code that expects that the caret position is always valid
            Caret.ValidateCaretPos();
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            ResizeTextArea();
        }

        public void ResizeTextArea()
        {
            int y = 0;
            int h = 0;
            if (_hRuler != null)
            {
                _hRuler.Bounds = new Rectangle(0, 0, Width - SystemInformation.HorizontalScrollBarArrowWidth, TextArea._FontHeight);

                y = _hRuler.Bounds.Bottom;
                h = _hRuler.Bounds.Height;
            }

            TextArea.Bounds = new Rectangle(0, y, Width - SystemInformation.HorizontalScrollBarArrowWidth, Height - SystemInformation.VerticalScrollBarArrowHeight - h);
            SetScrollBarBounds();
        }

        public void SetScrollBarBounds()
        {
            VScrollBar.Bounds = new Rectangle(TextArea.Bounds.Right, 0, SystemInformation.HorizontalScrollBarArrowWidth, Height - SystemInformation.VerticalScrollBarArrowHeight);
            HScrollBar.Bounds = new Rectangle(0, TextArea.Bounds.Bottom, Width - SystemInformation.HorizontalScrollBarArrowWidth, SystemInformation.VerticalScrollBarArrowHeight);
        }


        void AdjustScrollBarsOnDocumentChange(object sender, DocumentEventArgs e)
        {
            if (_motherTextEditorControl.IsInUpdate == false)
            {
                AdjustScrollBarsClearCache();
                AdjustScrollBars();
            }
            else
            {
                _adjustScrollBarsOnNextUpdate = true;
            }
        }

        void DocumentUpdateCommitted(object sender, EventArgs e)
        {
            if (_motherTextEditorControl.IsInUpdate == false)
            {
                Caret.ValidateCaretPos();

                // AdjustScrollBarsOnCommittedUpdate
                if (!_scrollToPosOnNextUpdate.IsEmpty)
                {
                    ScrollTo(_scrollToPosOnNextUpdate.Y, _scrollToPosOnNextUpdate.X);
                }

                if (_adjustScrollBarsOnNextUpdate)
                {
                    AdjustScrollBarsClearCache();
                    AdjustScrollBars();
                }
            }
        }

        void AdjustScrollBarsClearCache()
        {
            if (_lineLengthCache != null)
            {
                if (_lineLengthCache.Length < this.Document.TotalNumberOfLines + 2 * LL_CACHE_ADD_SIZE)
                {
                    _lineLengthCache = null;
                }
                else
                {
                    Array.Clear(_lineLengthCache, 0, _lineLengthCache.Length);
                }
            }
        }

        public void AdjustScrollBars()
        {
            _adjustScrollBarsOnNextUpdate = false;
            VScrollBar.Minimum = 0;
            // number of visible lines in document (folding!)
            VScrollBar.Maximum = TextArea.MaxVScrollValue;
            int max = 0;

            int firstLine = TextArea.FirstVisibleLine;
            int lastLine = Document.GetFirstLogicalLine(TextArea.FirstPhysicalLine + TextArea.VisibleLineCount);
            if (lastLine >= Document.TotalNumberOfLines)
                lastLine = Document.TotalNumberOfLines - 1;

            if (_lineLengthCache == null || _lineLengthCache.Length <= lastLine)
            {
                _lineLengthCache = new int[lastLine + LL_CACHE_ADD_SIZE];
            }

            for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
            {
                LineSegment lineSegment = Document.GetLineSegment(lineNumber);
                if (Document.FoldingManager.IsLineVisible(lineNumber))
                {
                    if (_lineLengthCache[lineNumber] > 0)
                    {
                        max = Math.Max(max, _lineLengthCache[lineNumber]);
                    }
                    else
                    {
                        int visualLength = TextArea.GetVisualColumnFast(lineSegment, lineSegment.Length);
                        _lineLengthCache[lineNumber] = Math.Max(1, visualLength);
                        max = Math.Max(max, visualLength);
                    }
                }
            }
            HScrollBar.Minimum = 0;
            HScrollBar.Maximum = (Math.Max(max + 20, TextArea.VisibleColumnCount - 1));

            VScrollBar.LargeChange = Math.Max(0, TextArea.DrawingPosition.Height);
            VScrollBar.SmallChange = Math.Max(0, TextArea._FontHeight);

            HScrollBar.LargeChange = Math.Max(0, TextArea.VisibleColumnCount - 1);
            HScrollBar.SmallChange = Math.Max(0, TextArea.SpaceWidth);
        }

        public void OptionsChanged()
        {
            TextArea.OptionsChanged();

            if (Shared.TEP.ShowHorizontalRuler)
            {
                if (_hRuler == null)
                {
                    _hRuler = new HRuler(TextArea);
                    Controls.Add(_hRuler);
                    ResizeTextArea();
                }
                else
                {
                    _hRuler.Invalidate();
                }
            }
            else
            {
                if (_hRuler != null)
                {
                    Controls.Remove(_hRuler);
                    _hRuler.Dispose();
                    _hRuler = null;
                    ResizeTextArea();
                }
            }

            AdjustScrollBars();
        }

        void VScrollBarValueChanged(object sender, EventArgs e)
        {
            TextArea.VirtualTop = new Point(TextArea.VirtualTop.X, VScrollBar.Value);
            TextArea.Invalidate();
            AdjustScrollBars();
        }

        void HScrollBarValueChanged(object sender, EventArgs e)
        {
            TextArea.VirtualTop = new Point(HScrollBar.Value * TextArea.WideSpaceWidth, TextArea.VirtualTop.Y);
            TextArea.Invalidate();
        }

        public void HandleMouseWheel(MouseEventArgs e)
        {
            int scrollDistance = _mouseWheelHandler.GetScrollAmount(e);
            if (scrollDistance == 0)
                return;

            if ((ModifierKeys & Keys.Control) != 0 && Shared.TEP.MouseWheelTextZoom)
            {
                if (scrollDistance > 0)
                {
                    _motherTextEditorControl.Font = new Font(_motherTextEditorControl.Font.Name, _motherTextEditorControl.Font.Size + 1);
                }
                else
                {
                    _motherTextEditorControl.Font = new Font(_motherTextEditorControl.Font.Name, Math.Max(6, _motherTextEditorControl.Font.Size - 1));
                }
            }
            else
            {
                if (Shared.TEP.MouseWheelScrollDown)
                    scrollDistance = -scrollDistance;

                int newValue = VScrollBar.Value + VScrollBar.SmallChange * scrollDistance;
                VScrollBar.Value = Math.Max(VScrollBar.Minimum, Math.Min(VScrollBar.Maximum - VScrollBar.LargeChange + 1, newValue));
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (DoHandleMousewheel)
            {
                HandleMouseWheel(e);
            }
        }

        public void ScrollToCaret()
        {
            ScrollTo(TextArea.Caret.Line, TextArea.Caret.Column);
        }

        public void ScrollTo(int line, int column)
        {
            if (_motherTextEditorControl.IsInUpdate)
            {
                _scrollToPosOnNextUpdate = new Point(column, line);
                return;
            }
            else
            {
                _scrollToPosOnNextUpdate = Point.Empty;
            }

            ScrollTo(line);

            int curCharMin = HScrollBar.Value - HScrollBar.Minimum;
            int curCharMax = curCharMin + TextArea.VisibleColumnCount;

            int pos = TextArea.GetVisualColumn(line, column);

            if (TextArea.VisibleColumnCount < 0)
            {
                HScrollBar.Value = 0;
            }
            else
            {
                if (pos < curCharMin)
                {
                    HScrollBar.Value = Math.Max(0, pos - _scrollMarginHeight);
                }
                else
                {
                    if (pos > curCharMax)
                    {
                        HScrollBar.Value = Math.Max(0, Math.Min(HScrollBar.Maximum, (pos - TextArea.VisibleColumnCount + _scrollMarginHeight)));
                    }
                }
            }
        }

        /// <summary>
        /// Ensure that line is visible.
        /// </summary>
        public void ScrollTo(int line)
        {
            line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
            line = Document.GetVisibleLine(line);
            int curLineMin = TextArea.FirstPhysicalLine;
            if (TextArea.LineHeightRemainder > 0)
            {
                curLineMin++;
            }

            if (line - _scrollMarginHeight + 3 < curLineMin)
            {
                this.VScrollBar.Value = Math.Max(0, Math.Min(this.VScrollBar.Maximum, (line - _scrollMarginHeight + 3) * TextArea._FontHeight));
                VScrollBarValueChanged(this, EventArgs.Empty);
            }
            else
            {
                int curLineMax = curLineMin + this.TextArea.VisibleLineCount;
                if (line + _scrollMarginHeight - 1 > curLineMax)
                {
                    if (this.TextArea.VisibleLineCount == 1)
                    {
                        this.VScrollBar.Value = Math.Max(0, Math.Min(this.VScrollBar.Maximum, (line - _scrollMarginHeight - 1) * TextArea._FontHeight));
                    }
                    else
                    {
                        this.VScrollBar.Value = Math.Min(this.VScrollBar.Maximum, (line - this.TextArea.VisibleLineCount + _scrollMarginHeight - 1) * TextArea._FontHeight);
                    }
                    VScrollBarValueChanged(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Scroll so that the specified line is centered.
        /// </summary>
        /// <param name="line">Line to center view on</param>
        /// <param name="treshold">If this action would cause scrolling by less than or equal to
        /// <paramref name="treshold"/> lines in any direction, don't scroll.
        /// Use -1 to always center the view.</param>
        public void CenterViewOn(int line, int treshold)
        {
            line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
            // convert line to visible line:
            line = Document.GetVisibleLine(line);
            // subtract half the visible line count
            line -= TextArea.VisibleLineCount / 2;

            int curLineMin = TextArea.FirstPhysicalLine;
            if (TextArea.LineHeightRemainder > 0)
            {
                curLineMin++;
            }
            if (Math.Abs(curLineMin - line) > treshold)
            {
                // scroll:
                this.VScrollBar.Value = Math.Max(0, Math.Min(this.VScrollBar.Maximum, (line - _scrollMarginHeight + 3) * TextArea._FontHeight));
                VScrollBarValueChanged(this, EventArgs.Empty);
            }
        }

        public void JumpTo(int line)
        {
            line = Math.Max(0, Math.Min(line, Document.TotalNumberOfLines - 1));
            string text = Document.GetText(Document.GetLineSegment(line));
            JumpTo(line, text.Length - text.TrimStart().Length);
        }

        public void JumpTo(int line, int column)
        {
            TextArea.Focus();
            TextArea.SelectionManager.ClearSelection();
            TextArea.Caret.Position = new TextLocation(column, line);
            TextArea.SetDesiredColumn();
            ScrollToCaret();
        }

        //TODO2- orig commented out.
        //public event MouseEventHandler ShowContextMenu;
        //protected override void WndProc(ref Message m)
        //{
        //    if (m.Msg == 0x007B)   // handle WM_CONTEXTMENU
        //    {
        //        if (ShowContextMenu != null)
        //        {
        //            long lParam = m.LParam.ToInt64();
        //            int x = unchecked((short)(lParam & 0xffff));
        //            int y = unchecked((short)((lParam & 0xffff0000) >> 16));
        //            if (x == -1 && y == -1)
        //            {
        //                Point pos = Caret.ScreenPosition;
        //                ShowContextMenu(this, new MouseEventArgs(MouseButtons.None, 0, pos.X, pos.Y + TextArea.FontHeight, 0));
        //            }
        //            else
        //            {
        //                Point pos = PointToClient(new Point(x, y));
        //                ShowContextMenu(this, new MouseEventArgs(MouseButtons.Right, 1, pos.X, pos.Y, 0));
        //            }
        //        }
        //    }
        //    base.WndProc(ref m);
        //}

        //protected override void OnEnter(EventArgs e)
        //{
        //    // SD2-1072 - Make sure the caret line is valid if anyone
        //    // has handlers for the Enter event.
        //    Caret.ValidateCaretPos();
        //    base.OnEnter(e);
        //}
    }
}
