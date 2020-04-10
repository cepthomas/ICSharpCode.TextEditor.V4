// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// This class draws icons e.g. bookmarks.
    /// </summary>
    public class IconBarMargin : IMargin
    {
        const int ICON_BAR_WIDTH = 18;

        static readonly Size _iconBarSize = new Size(ICON_BAR_WIDTH, -1);

        public event MarginPaintEventHandler Painted;
        public event MarginMouseEventHandler MouseDown;
        public event MarginMouseEventHandler MouseMove;
        public event EventHandler MouseLeave;


        public Rectangle DrawingPosition { get; set; }

        public TextArea TextArea { get; }

        public Cursor Cursor { get; set; } = Cursors.Default;

        public Size Size { get { return _iconBarSize; } }

        public bool IsVisible { get { return Shared.TEP.IsIconBarVisible; } }


        public IconBarMargin(TextArea textArea)
        {
            TextArea = textArea;
        }

        public void Paint(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }
            // paint background
            g.FillRectangle(SystemBrushes.Control, new Rectangle(DrawingPosition.X, rect.Top, DrawingPosition.Width - 1, rect.Height));
            g.DrawLine(SystemPens.ControlDark, DrawingPosition.Right - 1, rect.Top, DrawingPosition.Right - 1, rect.Bottom);

            // paint icons
            foreach (Bookmark mark in TextArea.Document.BookmarkManager.Marks)
            {
                int lineNumber = TextArea.Document.GetVisibleLine(mark.LineNumber);
                int lineHeight = TextArea.TextView.FontHeight;
                int yPos = lineNumber * lineHeight - TextArea.VirtualTop.Y;
                if (IsLineInsideRegion(yPos, yPos + lineHeight, rect.Y, rect.Bottom))
                {
                    if (lineNumber == TextArea.Document.GetVisibleLine(mark.LineNumber - 1))
                    {
                        // marker is inside folded region, do not draw it
                        continue;
                    }
                    mark.Draw(this, g, new Point(0, yPos));
                }
            }

            Painted?.Invoke(this, g, rect);
        }

        public void HandleMouseDown(Point mousePos, MouseButtons mouseButtons)
        {
            int clickedVisibleLine = (mousePos.Y + TextArea.VirtualTop.Y) / TextArea.TextView.FontHeight;
            int lineNumber = TextArea.Document.GetFirstLogicalLine(clickedVisibleLine);

            if ((mouseButtons & MouseButtons.Right) == MouseButtons.Right)
            {
                if (TextArea.Caret.Line != lineNumber)
                {
                    TextArea.Caret.Line = lineNumber;
                }
            }

            IList<Bookmark> marks = TextArea.Document.BookmarkManager.Marks;
            List<Bookmark> marksInLine = new List<Bookmark>();
            int oldCount = marks.Count;

            foreach (Bookmark mark in marks)
            {
                if (mark.LineNumber == lineNumber)
                {
                    marksInLine.Add(mark);
                }
            }

            for (int i = marksInLine.Count - 1; i >= 0; i--)
            {
                Bookmark mark = marksInLine[i];
                if (mark.Click(TextArea, new MouseEventArgs(mouseButtons, 1, mousePos.X, mousePos.Y, 0)))
                {
                    if (oldCount != marks.Count)
                    {
                        TextArea.UpdateLine(lineNumber);
                    }
                    return;
                }
            }

            MouseDown?.Invoke(this, mousePos, mouseButtons);
        }

        public void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
        {
            MouseMove?.Invoke(this, mousepos, mouseButtons);
        }

        public void HandleMouseLeave(EventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }


        #region Drawing functions
        public void DrawBreakpoint(Graphics g, int y, bool isEnabled, bool isHealthy)
        {
            int diameter = Math.Min(ICON_BAR_WIDTH - 2, TextArea.TextView.FontHeight);
            Rectangle rect = new Rectangle(1, y + (TextArea.TextView.FontHeight - diameter) / 2, diameter, diameter);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(rect);
                using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
                {
                    pthGrBrush.CenterPoint = new PointF(rect.Left + rect.Width / 3, rect.Top + rect.Height / 3);
                    pthGrBrush.CenterColor = Color.MistyRose;
                    Color[] colors = { isHealthy ? Color.Firebrick : Color.Olive };
                    pthGrBrush.SurroundColors = colors;

                    if (isEnabled)
                    {
                        g.FillEllipse(pthGrBrush, rect);
                    }
                    else
                    {
                        g.FillEllipse(SystemBrushes.Control, rect);
                        using (Pen pen = new Pen(pthGrBrush))
                        {
                            g.DrawEllipse(pen, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2));
                        }
                    }
                }
            }
        }

        public void DrawBookmark(Graphics g, int y, bool isEnabled)
        {
            int delta = TextArea.TextView.FontHeight / 8;
            Rectangle rect = new Rectangle(1, y + delta, DrawingPosition.Width - 4, TextArea.TextView.FontHeight - delta * 2);

            if (isEnabled)
            {
                using (Brush brush = new LinearGradientBrush(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom), Color.SkyBlue, Color.White))
                {
                    FillRoundRect(g, brush, rect);
                }
            }
            else
            {
                FillRoundRect(g, Brushes.White, rect);
            }
            using (Brush brush = new LinearGradientBrush(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom), Color.SkyBlue, Color.Blue))
            {
                using (Pen pen = new Pen(brush))
                {
                    DrawRoundRect(g, pen, rect);
                }
            }
        }

        public void DrawArrow(Graphics g, int y)
        {
            int delta = TextArea.TextView.FontHeight / 8;
            Rectangle rect = new Rectangle(1, y + delta, DrawingPosition.Width - 4, TextArea.TextView.FontHeight - delta * 2);
            using (Brush brush = new LinearGradientBrush(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom), Color.LightYellow, Color.Yellow))
            {
                FillArrow(g, brush, rect);
            }

            using (Brush brush = new LinearGradientBrush(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom), Color.Yellow, Color.Brown))
            {
                using (Pen pen = new Pen(brush))
                {
                    DrawArrow(g, pen, rect);
                }
            }
        }

        GraphicsPath CreateArrowGraphicsPath(Rectangle r)
        {
            GraphicsPath gp = new GraphicsPath();
            int halfX = r.Width / 2;
            int halfY = r.Height / 2;
            gp.AddLine(r.X, r.Y + halfY / 2, r.X + halfX, r.Y + halfY / 2);
            gp.AddLine(r.X + halfX, r.Y + halfY / 2, r.X + halfX, r.Y);
            gp.AddLine(r.X + halfX, r.Y, r.Right, r.Y + halfY);
            gp.AddLine(r.Right, r.Y + halfY, r.X + halfX, r.Bottom);
            gp.AddLine(r.X + halfX, r.Bottom, r.X + halfX, r.Bottom - halfY / 2);
            gp.AddLine(r.X + halfX, r.Bottom - halfY / 2, r.X, r.Bottom - halfY / 2);
            gp.AddLine(r.X, r.Bottom - halfY / 2, r.X, r.Y + halfY / 2);
            gp.CloseFigure();
            return gp;
        }

        GraphicsPath CreateRoundRectGraphicsPath(Rectangle r)
        {
            GraphicsPath gp = new GraphicsPath();
            int radius = r.Width / 2;
            gp.AddLine(r.X + radius, r.Y, r.Right - radius, r.Y);
            gp.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);

            gp.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom - radius);
            gp.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);

            gp.AddLine(r.Right - radius, r.Bottom, r.X + radius, r.Bottom);
            gp.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);

            gp.AddLine(r.X, r.Bottom - radius, r.X, r.Y + radius);
            gp.AddArc(r.X, r.Y, radius, radius, 180, 90);

            gp.CloseFigure();
            return gp;
        }

        void DrawRoundRect(Graphics g, Pen p, Rectangle r)
        {
            using (GraphicsPath gp = CreateRoundRectGraphicsPath(r))
            {
                g.DrawPath(p, gp);
            }
        }

        void FillRoundRect(Graphics g, Brush b, Rectangle r)
        {
            using (GraphicsPath gp = CreateRoundRectGraphicsPath(r))
            {
                g.FillPath(b, gp);
            }
        }

        void DrawArrow(Graphics g, Pen p, Rectangle r)
        {
            using (GraphicsPath gp = CreateArrowGraphicsPath(r))
            {
                g.DrawPath(p, gp);
            }
        }

        void FillArrow(Graphics g, Brush b, Rectangle r)
        {
            using (GraphicsPath gp = CreateArrowGraphicsPath(r))
            {
                g.FillPath(b, gp);
            }
        }

        #endregion

        bool IsLineInsideRegion(int top, int bottom, int regionTop, int regionBottom)
        {
            if (top >= regionTop && top <= regionBottom)
            {
                // Region overlaps the line's top edge.
                return true;
            }
            else if (regionTop > top && regionTop < bottom)
            {
                // Region's top edge inside line.
                return true;
            }
            return false;
        }
    }
}
