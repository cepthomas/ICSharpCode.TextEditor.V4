// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// This class draws the folding markers.
    /// </summary>
    public class FoldMargin : IMargin
    {
        int _selectedFoldLine = -1;

        public event MarginPaintEventHandler Painted;
        public event MarginMouseEventHandler MouseDown;
        public event MarginMouseEventHandler MouseMove;
        public event EventHandler MouseLeave;

        public Rectangle DrawingPosition { get; set; }

        public TextArea TextArea { get; }

        public Cursor Cursor { get; set; } = Cursors.Default;

        public Size Size { get { return new Size(TextArea._FontHeight, -1); } }

        public bool IsVisible { get { return Shared.TEP.EnableFolding; } }

        public FoldMargin(TextArea textArea)
        {
            TextArea = textArea;
        }

        public void Paint(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }
            HighlightColor lineNumberPainterColor = Shared.TEP.LineNumbersColor;

            for (int y = 0; y < (DrawingPosition.Height + TextArea.VisibleLineDrawingRemainder) / TextArea._FontHeight + 1; ++y)
            {
                Rectangle markerRectangle = new Rectangle(DrawingPosition.X,
                        DrawingPosition.Top + y * TextArea._FontHeight - TextArea.VisibleLineDrawingRemainder,
                        DrawingPosition.Width,
                        TextArea._FontHeight);

                if (rect.IntersectsWith(markerRectangle))
                {
                    // draw dotted separator line
                    if (Shared.TEP.ShowLineNumbers)
                    {
                        g.FillRectangle(BrushRegistry.GetBrush(TextArea.Enabled ? lineNumberPainterColor.BackgroundColor : SystemColors.InactiveBorder), markerRectangle);

                        g.DrawLine(BrushRegistry.GetDotPen(lineNumberPainterColor.Color), DrawingPosition.X, markerRectangle.Y, DrawingPosition.X, markerRectangle.Bottom);
                    }
                    else
                    {
                        g.FillRectangle(BrushRegistry.GetBrush(TextArea.Enabled ? lineNumberPainterColor.BackgroundColor : SystemColors.InactiveBorder), markerRectangle);
                    }

                    int currentLine = TextArea.Document.GetFirstLogicalLine(TextArea.FirstPhysicalLine + y);
                    if (currentLine < TextArea.Document.TotalNumberOfLines)
                    {
                        PaintFoldMarker(g, currentLine, markerRectangle);
                    }
                }
            }
        }

        bool SelectedFoldingFrom(List<FoldMarker> list)
        {
            if (list != null)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (_selectedFoldLine == list[i].StartLine)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void PaintFoldMarker(Graphics g, int lineNumber, Rectangle drawingRectangle)
        {
            HighlightColor foldLineColor = Shared.TEP.FoldLineColor;
            HighlightColor selectedFoldLine = Shared.TEP.SelectedFoldLineColor;

            List<FoldMarker> foldingsWithStart = TextArea.Document.FoldingManager.GetFoldingsWithStart(lineNumber);
            List<FoldMarker> foldingsBetween = TextArea.Document.FoldingManager.GetFoldingsContainsLineNumber(lineNumber);
            List<FoldMarker> foldingsWithEnd = TextArea.Document.FoldingManager.GetFoldingsWithEnd(lineNumber);

            bool isFoldStart = foldingsWithStart.Count > 0;
            bool isBetween = foldingsBetween.Count > 0;
            bool isFoldEnd = foldingsWithEnd.Count > 0;

            bool isStartSelected = SelectedFoldingFrom(foldingsWithStart);
            bool isBetweenSelected = SelectedFoldingFrom(foldingsBetween);
            bool isEndSelected = SelectedFoldingFrom(foldingsWithEnd);

            int foldMarkerSize = (int)Math.Round(TextArea._FontHeight * 0.57f);
            foldMarkerSize -= (foldMarkerSize) % 2;
            int foldMarkerYPos = drawingRectangle.Y + (int)((drawingRectangle.Height - foldMarkerSize) / 2);
            int xPos = drawingRectangle.X + (drawingRectangle.Width - foldMarkerSize) / 2 + foldMarkerSize / 2;

            if (isFoldStart)
            {
                bool isVisible = true;
                bool moreLinedOpenFold = false;
                foreach (FoldMarker foldMarker in foldingsWithStart)
                {
                    if (foldMarker.IsFolded)
                    {
                        isVisible = false;
                    }
                    else
                    {
                        moreLinedOpenFold = foldMarker.EndLine > foldMarker.StartLine;
                    }
                }

                bool isFoldEndFromUpperFold = false;
                foreach (FoldMarker foldMarker in foldingsWithEnd)
                {
                    if (foldMarker.EndLine > foldMarker.StartLine && !foldMarker.IsFolded)
                    {
                        isFoldEndFromUpperFold = true;
                    }
                }

                DrawFoldMarker(g, new RectangleF(drawingRectangle.X + (drawingRectangle.Width - foldMarkerSize) / 2,
                            foldMarkerYPos, foldMarkerSize, foldMarkerSize), isVisible, isStartSelected);

                // draw line above fold marker
                if (isBetween || isFoldEndFromUpperFold)
                {
                    g.DrawLine(BrushRegistry.GetPen(isBetweenSelected ? selectedFoldLine.Color : foldLineColor.Color),
                               xPos, drawingRectangle.Top, xPos, foldMarkerYPos - 1);
                }

                // draw line below fold marker
                if (isBetween || moreLinedOpenFold)
                {
                    g.DrawLine(BrushRegistry.GetPen(isEndSelected || (isStartSelected && isVisible) || isBetweenSelected ? selectedFoldLine.Color : foldLineColor.Color),
                               xPos, foldMarkerYPos + foldMarkerSize + 1, xPos, drawingRectangle.Bottom);
                }
            }
            else
            {
                if (isFoldEnd)
                {
                    int midy = drawingRectangle.Top + drawingRectangle.Height / 2;

                    // draw fold end marker
                    g.DrawLine(BrushRegistry.GetPen(isEndSelected ? selectedFoldLine.Color : foldLineColor.Color),
                               xPos,
                               midy,
                               xPos + foldMarkerSize / 2,
                               midy);

                    // draw line above fold end marker
                    // must be drawn after fold marker because it might have a different color than the fold marker
                    g.DrawLine(BrushRegistry.GetPen(isBetweenSelected || isEndSelected ? selectedFoldLine.Color : foldLineColor.Color),
                               xPos,
                               drawingRectangle.Top,
                               xPos,
                               midy);

                    // draw line below fold end marker
                    if (isBetween)
                    {
                        g.DrawLine(BrushRegistry.GetPen(isBetweenSelected ? selectedFoldLine.Color : foldLineColor.Color),
                                   xPos,
                                   midy + 1,
                                   xPos,
                                   drawingRectangle.Bottom);
                    }
                }
                else if (isBetween)
                {
                    // just draw the line :)
                    g.DrawLine(BrushRegistry.GetPen(isBetweenSelected ? selectedFoldLine.Color : foldLineColor.Color),
                               xPos,
                               drawingRectangle.Top,
                               xPos,
                               drawingRectangle.Bottom);
                }
            }
        }

        public void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
        {
            bool showFolding = Shared.TEP.EnableFolding;
            int physicalLine = +(int)((mousepos.Y + TextArea.VirtualTop.Y) / TextArea._FontHeight);
            int realline = TextArea.Document.GetFirstLogicalLine(physicalLine);

            if (!showFolding || realline < 0 || realline + 1 >= TextArea.Document.TotalNumberOfLines)
            {
                return;
            }

            List<FoldMarker> foldMarkers = TextArea.Document.FoldingManager.GetFoldingsWithStart(realline);
            int oldSelection = _selectedFoldLine;
            if (foldMarkers.Count > 0)
            {
                _selectedFoldLine = realline;
            }
            else
            {
                _selectedFoldLine = -1;
            }

            if (oldSelection != _selectedFoldLine)
            {
                TextArea.Refresh(this);
            }
        }

        public void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
        {
            bool showFolding = Shared.TEP.EnableFolding;
            int physicalLine = +(int)((mousepos.Y + TextArea.VirtualTop.Y) / TextArea._FontHeight);
            int realline = TextArea.Document.GetFirstLogicalLine(physicalLine);

            // focus the textarea if the user clicks on the line number view
            TextArea.Focus();

            if (!showFolding || realline < 0 || realline + 1 >= TextArea.Document.TotalNumberOfLines)
            {
                return;
            }

            List<FoldMarker> foldMarkers = TextArea.Document.FoldingManager.GetFoldingsWithStart(realline);
            foreach (FoldMarker fm in foldMarkers)
            {
                fm.IsFolded = !fm.IsFolded;
            }
            TextArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
        }

        public void HandleMouseLeave(EventArgs e)
        {
            if (_selectedFoldLine != -1)
            {
                _selectedFoldLine = -1;
                TextArea.Refresh(this);
            }
        }

        #region Drawing functions
        void DrawFoldMarker(Graphics g, RectangleF rectangle, bool isOpened, bool isSelected)
        {
            HighlightColor foldMarkerColor = Shared.TEP.FoldMarkerColor;
            HighlightColor foldLineColor = Shared.TEP.FoldLineColor;
            HighlightColor selectedFoldLine = Shared.TEP.SelectedFoldLineColor;

            Rectangle intRect = new Rectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);
            g.FillRectangle(BrushRegistry.GetBrush(foldMarkerColor.BackgroundColor), intRect);
            g.DrawRectangle(BrushRegistry.GetPen(isSelected ? selectedFoldLine.Color : foldLineColor.Color), intRect);

            int space = (int)Math.Round(((double)rectangle.Height) / 8d) + 1;
            int mid = intRect.Height / 2 + intRect.Height % 2;

            // draw minus
            g.DrawLine(BrushRegistry.GetPen(foldMarkerColor.Color),
                       rectangle.X + space,
                       rectangle.Y + mid,
                       rectangle.X + rectangle.Width - space,
                       rectangle.Y + mid);

            // draw plus
            if (!isOpened)
            {
                g.DrawLine(BrushRegistry.GetPen(foldMarkerColor.Color),
                           rectangle.X + mid,
                           rectangle.Y + space,
                           rectangle.X + mid,
                           rectangle.Y + rectangle.Height - space);
            }
        }
        #endregion
    }
}
