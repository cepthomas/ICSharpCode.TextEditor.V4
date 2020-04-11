// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// In this enumeration are all caret modes listed.
    /// </summary>
    public enum CaretMode { InsertMode, OverwriteMode }


    public class Caret : IDisposable
    {
        int _line = 0;
        int _column = 0;
        CaretMode _caretMode;

        static bool _caretCreated = false;
        bool _hidden = true;
        TextArea _textArea;
        Point _currentPos = new Point(-1, -1);
        CaretImplementation _caretImplementation;

        int _oldLine = -1;
        bool _outstandingUpdate;


        bool _firePositionChangedAfterUpdateEnd;


        //////// events

        /// <remarks>
        /// Is called each time the caret is moved.
        /// </remarks>
        public event EventHandler PositionChanged;

        /// <remarks>
        /// Is called each time the CaretMode has changed.
        /// </remarks>
        public event EventHandler CaretModeChanged;


        ////////////////// properties

        /// <value>
        /// The 'prefered' xPos in which the caret moves, when it is moved
        /// up/down. Measured in pixels, not in characters!
        /// </value>
        public int DesiredColumn { get; set; } = 0;

        /// <value>
        /// The current caret mode.
        /// </value>
        public CaretMode CaretMode
        {
            get
            {
                return _caretMode;
            }
            set
            {
                _caretMode = value;
                OnCaretModeChanged(EventArgs.Empty);
            }
        }

        public int Line
        {
            get
            {
                return _line;
            }
            set
            {
                _line = value;
                ValidateCaretPos();
                UpdateCaretPosition();
                OnPositionChanged(EventArgs.Empty);
            }
        }

        public int Column
        {
            get
            {
                return _column;
            }
            set
            {
                _column = value;
                ValidateCaretPos();
                UpdateCaretPosition();
                OnPositionChanged(EventArgs.Empty);
            }
        }

        public TextLocation Position
        {
            get
            {
                return new TextLocation(_column, _line);
            }
            set
            {
                _line = value.Y;
                _column = value.X;
                ValidateCaretPos();
                UpdateCaretPosition();
                OnPositionChanged(EventArgs.Empty);
            }
        }

        public int Offset
        {
            get
            {
                return _textArea.Document.PositionToOffset(Position);
            }
        }

        public Caret(TextArea textArea)
        {
            _textArea = textArea;
            textArea.GotFocus += new EventHandler(GotFocus);
            textArea.LostFocus += new EventHandler(LostFocus);

            // Avalon says:
            // Create Win32 caret so that Windows knows where our managed caret is. This is necessary for
            // features like 'Follow text editing' in the Windows Magnifier.

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                _caretImplementation = new ManagedCaret(this);
            else
                _caretImplementation = new Win32Caret(this);
        }

        public void Dispose()
        {
            _textArea.GotFocus -= new EventHandler(GotFocus);
            _textArea.LostFocus -= new EventHandler(LostFocus);
            _textArea = null;
            _caretImplementation.Dispose();
        }

        public TextLocation ValidatePosition(TextLocation pos)
        {
            int line = Math.Max(0, Math.Min(_textArea.Document.TotalNumberOfLines - 1, pos.Y));
            int column = Math.Max(0, pos.X);

            if (column == int.MaxValue || !Shared.TEP.AllowCaretBeyondEOL)
            {
                LineSegment lineSegment = _textArea.Document.GetLineSegment(line);
                column = Math.Min(column, lineSegment.Length);
            }
            return new TextLocation(column, line);
        }

        /// <remarks>
        /// If the caret position is outside the document text bounds
        /// it is set to the correct position by calling ValidateCaretPos.
        /// </remarks>
        public void ValidateCaretPos()
        {
            _line = Math.Max(0, Math.Min(_textArea.Document.TotalNumberOfLines - 1, _line));
            _column = Math.Max(0, _column);

            if (_column == int.MaxValue || !Shared.TEP.AllowCaretBeyondEOL)
            {
                LineSegment lineSegment = _textArea.Document.GetLineSegment(_line);
                _column = Math.Min(_column, lineSegment.Length);
            }
        }

        void CreateCaret()
        {
            while (!_caretCreated)
            {
                switch (_caretMode)
                {
                    case CaretMode.InsertMode:
                        _caretCreated = _caretImplementation.Create(2, _textArea._FontHeight);
                        break;
                    case CaretMode.OverwriteMode:
                        _caretCreated = _caretImplementation.Create((int)_textArea.SpaceWidth, _textArea._FontHeight);
                        break;
                }
            }
            if (_currentPos.X < 0)
            {
                ValidateCaretPos();
                _currentPos = ScreenPosition;
            }
            _caretImplementation.SetPosition(_currentPos.X, _currentPos.Y);
            _caretImplementation.Show();
        }

        public void RecreateCaret()
        {
            DisposeCaret();
            if (!_hidden)
            {
                CreateCaret();
            }
        }

        void DisposeCaret()
        {
            if (_caretCreated)
            {
                _caretCreated = false;
                _caretImplementation.Hide();
                _caretImplementation.Destroy();
            }
        }

        void GotFocus(object sender, EventArgs e)
        {
            _hidden = false;
            if (!_textArea.MotherTextEditorControl.IsInUpdate)
            {
                CreateCaret();
                UpdateCaretPosition();
            }
        }

        void LostFocus(object sender, EventArgs e)
        {
            _hidden = true;
            DisposeCaret();
        }

        public Point ScreenPosition
        {
            get
            {
                int xpos = _textArea.GetDrawingXPos(_line, _column);
                return new Point(_textArea.DrawingPosition.X + xpos,
                                 _textArea.DrawingPosition.Y
                                 + _textArea.Document.GetVisibleLine(_line) * _textArea._FontHeight
                                 - _textArea.VirtualTop.Y);
            }
        }

        internal void OnEndUpdate()
        {
            if (_outstandingUpdate)
                UpdateCaretPosition();
        }

        void PaintCaretLine(Graphics g)
        {
            if (Shared.TEP.CaretLine)
            {
                HighlightColor caretLineColor = Shared.TEP.CaretLineColor;

                g.DrawLine(BrushRegistry.GetDotPen(caretLineColor.Color),
                           _currentPos.X,
                           0,
                           _currentPos.X,
                           _textArea.DisplayRectangle.Height);
            }
        }

        public void UpdateCaretPosition()
        {
            if (Shared.TEP.CaretLine)
            {
                _textArea.Invalidate();
            }
            else
            {
                if (_caretImplementation.RequireRedrawOnPositionChange)
                {
                    _textArea.UpdateLine(_oldLine);
                    if (_line != _oldLine)
                        _textArea.UpdateLine(_line);
                }
                else
                {
                    if (Shared.TEP.LineViewerStyle == LineViewerStyle.FullRow && _oldLine != _line)
                    {
                        _textArea.UpdateLine(_oldLine);
                        _textArea.UpdateLine(_line);
                    }
                }
            }
            _oldLine = _line;

            if (_hidden || _textArea.MotherTextEditorControl.IsInUpdate)
            {
                _outstandingUpdate = true;
                return;
            }
            else
            {
                _outstandingUpdate = false;
            }

            ValidateCaretPos();
            int lineNr = _line;
            int xpos = _textArea.GetDrawingXPos(lineNr, this._column);
            //LineSegment lineSegment = textArea.Document.GetLineSegment(lineNr);
            Point pos = ScreenPosition;

            if (xpos >= 0)
            {
                CreateCaret();
                bool success = _caretImplementation.SetPosition(pos.X, pos.Y);
                if (!success)
                {
                    _caretImplementation.Destroy();
                    _caretCreated = false;
                    UpdateCaretPosition();
                }
            }
            else
            {
                _caretImplementation.Destroy();
            }

            //// set the input method editor location
            //if (ime == null)
            //{
            //    ime = new Ime(textArea.Handle, Shared.FontContainer.DefaultFont);
            //}
            //else
            //{
            //    ime.HWnd = textArea.Handle;
            //    ime.Font = Shared.FontContainer.DefaultFont;
            //}
            //ime.SetIMEWindowLocation(pos.X, pos.Y);

            _currentPos = pos;
        }

        #region Caret implementation
        internal void PaintCaret(Graphics g)
        {
            _caretImplementation.PaintCaret(g);
            PaintCaretLine(g);
        }

        abstract class CaretImplementation : IDisposable
        {
            public bool RequireRedrawOnPositionChange;

            public abstract bool Create(int width, int height);
            public abstract void Hide();
            public abstract void Show();
            public abstract bool SetPosition(int x, int y);
            public abstract void PaintCaret(Graphics g);
            public abstract void Destroy();

            public virtual void Dispose()
            {
                Destroy();
            }
        }

        class ManagedCaret : CaretImplementation
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
            bool visible;
            bool blink = true;
            int x, y, width, height;
            TextArea textArea;
            Caret parentCaret;

            public ManagedCaret(Caret caret)
            {
                base.RequireRedrawOnPositionChange = true;
                this.textArea = caret._textArea;
                this.parentCaret = caret;
                timer.Tick += CaretTimerTick;
            }

            void CaretTimerTick(object sender, EventArgs e)
            {
                blink = !blink;
                if (visible)
                    textArea.UpdateLine(parentCaret.Line);
            }

            public override bool Create(int width, int height)
            {
                this.visible = true;
                this.width = width - 2;
                this.height = height;
                timer.Enabled = true;
                return true;
            }
            public override void Hide()
            {
                visible = false;
            }
            public override void Show()
            {
                visible = true;
            }
            public override bool SetPosition(int x, int y)
            {
                this.x = x - 1;
                this.y = y;
                return true;
            }
            public override void PaintCaret(Graphics g)
            {
                if (visible && blink)
                    g.DrawRectangle(Pens.Gray, x, y, width, height);
            }
            public override void Destroy()
            {
                visible = false;
                timer.Enabled = false;
            }
            public override void Dispose()
            {
                base.Dispose();
                timer.Dispose();
            }
        }

        class Win32Caret : CaretImplementation
        {
            [DllImport("User32.dll")]
            static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);

            [DllImport("User32.dll")]
            static extern bool SetCaretPos(int x, int y);

            [DllImport("User32.dll")]
            static extern bool DestroyCaret();

            [DllImport("User32.dll")]
            static extern bool ShowCaret(IntPtr hWnd);

            [DllImport("User32.dll")]
            static extern bool HideCaret(IntPtr hWnd);

            TextArea textArea;

            public Win32Caret(Caret caret)
            {
                this.textArea = caret._textArea;
            }

            public override bool Create(int width, int height)
            {
                return CreateCaret(textArea.Handle, 0, width, height);
            }
            public override void Hide()
            {
                HideCaret(textArea.Handle);
            }
            public override void Show()
            {
                ShowCaret(textArea.Handle);
            }
            public override bool SetPosition(int x, int y)
            {
                return SetCaretPos(x, y);
            }
            public override void PaintCaret(Graphics g)
            {
            }
            public override void Destroy()
            {
                DestroyCaret();
            }
        }
        #endregion


        void FirePositionChangedAfterUpdateEnd(object sender, EventArgs e)
        {
            OnPositionChanged(EventArgs.Empty);
        }

        protected virtual void OnPositionChanged(EventArgs e)
        {
            if (_textArea.MotherTextEditorControl.IsInUpdate)
            {
                if (_firePositionChangedAfterUpdateEnd == false)
                {
                    _firePositionChangedAfterUpdateEnd = true;
                    _textArea.Document.UpdateCommited += FirePositionChangedAfterUpdateEnd;
                }
                return;
            }
            else if (_firePositionChangedAfterUpdateEnd)
            {
                _textArea.Document.UpdateCommited -= FirePositionChangedAfterUpdateEnd;
                _firePositionChangedAfterUpdateEnd = false;
            }

            List<FoldMarker> foldings = _textArea.Document.FoldingManager.GetFoldingsFromPosition(_line, _column);
            bool shouldUpdate = false;
            foreach (FoldMarker foldMarker in foldings)
            {
                shouldUpdate |= foldMarker.IsFolded;
                foldMarker.IsFolded = false;
            }

            if (shouldUpdate)
            {
                _textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
            }

            PositionChanged?.Invoke(this, e);
            _textArea.ScrollToCaret();
        }

        protected virtual void OnCaretModeChanged(EventArgs e)
        {
            CaretModeChanged?.Invoke(this, e);
            _caretImplementation.Hide();
            _caretImplementation.Destroy();
            _caretCreated = false;
            CreateCaret();
            _caretImplementation.Show();
        }
    }
}
