// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// This class views the line numbers and folding markers.
    /// </summary>
    public class GutterMargin : IMargin, IDisposable
    {
        StringFormat _numberStringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();

        public event MarginPaintEventHandler Painted;
        public event MarginMouseEventHandler MouseDown;
        public event MarginMouseEventHandler MouseMove;
        public event EventHandler MouseLeave;

        public Rectangle DrawingPosition { get; set; }

        public TextArea TextArea { get; }

        public Cursor Cursor { get; set; } = Cursors.Default;

        public Size Size { get { return new Size(TextArea.TextView.WideSpaceWidth * Math.Max(3, (int)Math.Log10(TextArea.Document.TotalNumberOfLines) + 1), -1); } }

        public bool IsVisible { get { return Shared.TEP.ShowLineNumbers; } }

        public GutterMargin(TextArea textArea)
        {
            TextArea = textArea;

            _numberStringFormat.LineAlignment = StringAlignment.Far;
            _numberStringFormat.FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
        }

        public void Dispose()
        {
            _numberStringFormat.Dispose();
        }

        public void Paint(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            HighlightColor lineNumberPainterColor = Shared.TEP.LineNumbersColor;
            int fontHeight = TextArea.TextView.FontHeight;
            Brush fillBrush = TextArea.Enabled ? BrushRegistry.GetBrush(lineNumberPainterColor.BackgroundColor) : SystemBrushes.InactiveBorder;
            Brush drawBrush = BrushRegistry.GetBrush(lineNumberPainterColor.Color);
            
            for (int y = 0; y < (DrawingPosition.Height + TextArea.TextView.VisibleLineDrawingRemainder) / fontHeight + 1; ++y)
            {
                int ypos = DrawingPosition.Y + fontHeight * y - TextArea.TextView.VisibleLineDrawingRemainder;
                Rectangle backgroundRectangle = new Rectangle(DrawingPosition.X, ypos, DrawingPosition.Width, fontHeight);
                if (rect.IntersectsWith(backgroundRectangle))
                {
                    g.FillRectangle(fillBrush, backgroundRectangle);
                    int curLine = TextArea.Document.GetFirstLogicalLine(TextArea.Document.GetVisibleLine(TextArea.TextView.FirstVisibleLine) + y);

                    if (curLine < TextArea.Document.TotalNumberOfLines)
                    {
                        g.DrawString((curLine + 1).ToString(),
                                     FontRegistry.GetFont(lineNumberPainterColor.Bold, lineNumberPainterColor.Italic),
                                     drawBrush,
                                     backgroundRectangle,
                                     _numberStringFormat);
                    }
                }
            }
        }

        public void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
        {
            TextLocation selectionStartPos;

            TextArea.SelectionManager.WhereFrom = SelSource.Gutter;
            int realline = TextArea.TextView.GetLogicalLine(mousepos.Y);
            bool isRect = (Control.ModifierKeys & Keys.Alt) != 0;

            if (realline >= 0 && realline < TextArea.Document.TotalNumberOfLines)
            {
                // shift-select
                if ((Control.ModifierKeys & Keys.Shift) != 0)
                {
                    if (!TextArea.SelectionManager.HasSomethingSelected && realline != TextArea.Caret.Position.Y)
                    {
                        if (realline >= TextArea.Caret.Position.Y)
                        {
                            // at or below starting selection, place the cursor on the next line
                            // nothing is selected so make a new selection from cursor
                            selectionStartPos = TextArea.Caret.Position;

                            // whole line selection - start of line to start of next line
                            if (realline < TextArea.Document.TotalNumberOfLines - 1)
                            {
                                TextArea.SelectionManager.SetSelection(selectionStartPos, new TextLocation(0, realline + 1), isRect);
                                TextArea.Caret.Position = new TextLocation(0, realline + 1);
                            }
                            else
                            {
                                TextArea.SelectionManager.SetSelection(selectionStartPos, new TextLocation(TextArea.Document.GetLineSegment(realline).Length + 1, realline), isRect);
                                TextArea.Caret.Position = new TextLocation(TextArea.Document.GetLineSegment(realline).Length + 1, realline);
                            }
                        }
                        else
                        {
                            // prior lines to starting selection, place the cursor on the same line as the new selection
                            // nothing is selected so make a new selection from cursor
                            selectionStartPos = TextArea.Caret.Position;

                            // whole line selection - start of line to start of next line
                            TextArea.SelectionManager.SetSelection(selectionStartPos, new TextLocation(0, realline + 1), isRect);
                            //            textArea.SelectionManager.SetSelection(selectionStartPos, new TextLocation(selectionStartPos.X, selectionStartPos.Y), isRect);
                            //            textArea.SelectionManager.ExtendSelection(new TextLocation(selectionStartPos.X, selectionStartPos.Y), new TextLocation(0, realline), false);
                            TextArea.Caret.Position = new TextLocation(0, realline);
                        }
                    }
                    else
                    {
                        // let MouseMove handle a shift-click in a gutter
                        MouseEventArgs e = new MouseEventArgs(mouseButtons, 1, mousepos.X, mousepos.Y, 0);
                        TextArea.RaiseMouseMove(e);
                    }
                }
                else // this is a new selection with no shift-key
                {
                    // sync the textareamousehandler mouse location
                    // (fixes problem with clicking out into a menu then back to the gutter whilst there is a selection)
                    TextArea.MousePos = mousepos;

                    selectionStartPos = new TextLocation(0, realline);
                    //textArea.SelectionManager.ClearSelection();

                    // whole line selection - start of line to start of next line
                    if (realline < TextArea.Document.TotalNumberOfLines - 1)
                    {
                        TextArea.SelectionManager.SetSelection(selectionStartPos, new TextLocation(selectionStartPos.X, selectionStartPos.Y + 1), isRect);
                        TextArea.Caret.Position = new TextLocation(selectionStartPos.X, selectionStartPos.Y + 1);
                    }
                    else
                    {
                        TextArea.SelectionManager.SetSelection(new TextLocation(0, realline), new TextLocation(TextArea.Document.GetLineSegment(realline).Length + 1, selectionStartPos.Y), isRect);
                        TextArea.Caret.Position = new TextLocation(TextArea.Document.GetLineSegment(realline).Length + 1, selectionStartPos.Y);
                    }
                }
            }
        }

        public void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
        {
            MouseMove?.Invoke(this, mousepos, mouseButtons);
        }

        public void HandleMouseLeave(EventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }
    }
}
