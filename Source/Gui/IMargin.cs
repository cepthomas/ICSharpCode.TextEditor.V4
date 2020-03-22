// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    public delegate void MarginMouseEventHandler(IMargin sender, Point mousepos, MouseButtons mouseButtons);
    public delegate void MarginPaintEventHandler(IMargin sender, Graphics g, Rectangle rect);


    /// <summary>
    /// This class views the line numbers and folding markers.
    /// </summary>
    public interface IMargin //TODO0 refactor?
    {
        event MarginPaintEventHandler Painted;
        event MarginMouseEventHandler MouseDown;
        event MarginMouseEventHandler MouseMove;
        event EventHandler MouseLeave;

        //[CLSCompliant(false)]
        //protected Rectangle drawingPosition = new Rectangle(0, 0, 0, 0);
        //[CLSCompliant(false)]
        //protected TextArea textArea;

        Rectangle DrawingPosition { get; set; }

        TextArea TextArea { get; }

       // public Document.Document Document { get { return TextArea.Document; } }

        Cursor Cursor { get; set; }

        Size Size { get; }

        bool IsVisible { get; }

        //protected AbstractMargin(TextArea textArea);

        void HandleMouseDown(Point mousepos, MouseButtons mouseButtons);

        void HandleMouseMove(Point mousepos, MouseButtons mouseButtons);

        void HandleMouseLeave(EventArgs e);

        void Paint(Graphics g, Rectangle rect);
    }


    //public abstract class AbstractMargin //TODO0 refactor?
    //{
    //    public event MarginPaintEventHandler Painted;
    //    public event MarginMouseEventHandler MouseDown;
    //    public event MarginMouseEventHandler MouseMove;
    //    public event EventHandler MouseLeave;

    //    //[CLSCompliant(false)]
    //    //protected Rectangle drawingPosition = new Rectangle(0, 0, 0, 0);
    //    //[CLSCompliant(false)]
    //    //protected TextArea textArea;

    //    public Rectangle DrawingPosition { get; set; }

    //    public TextArea TextArea { get; }

    //    public Document.Document Document { get { return TextArea.Document; } }

    //    public Cursor Cursor { get; set; } = Cursors.Default;

    //    public Size Size { get { return new Size(-1, -1); } }

    //    public bool IsVisible { get { return true; } }

    //    protected AbstractMargin(TextArea textArea)
    //    {
    //        TextArea = textArea;
    //    }

    //    public virtual void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
    //    {
    //        MouseDown?.Invoke(this, mousepos, mouseButtons);
    //    }
    //    public virtual void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
    //    {
    //        MouseMove?.Invoke(this, mousepos, mouseButtons);
    //    }
    //    public virtual void HandleMouseLeave(EventArgs e)
    //    {
    //        MouseLeave?.Invoke(this, e);
    //    }

    //    public virtual void Paint(Graphics g, Rectangle rect)
    //    {
    //        Painted?.Invoke(this, g, rect);
    //    }
    //}
}
