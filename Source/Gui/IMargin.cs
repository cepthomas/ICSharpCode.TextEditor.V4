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
    public interface IMargin //TODO0 refactor? Not sure this is needed. and the text view is one also, not a margin.
    {
        event MarginPaintEventHandler Painted;
        event MarginMouseEventHandler MouseDown;
        event MarginMouseEventHandler MouseMove;
        event EventHandler MouseLeave;

        Rectangle DrawingPosition { get; set; }

        TextArea TextArea { get; }

        Cursor Cursor { get; set; }

        Size Size { get; }

        bool IsVisible { get; }

        void HandleMouseDown(Point mousepos, MouseButtons mouseButtons);

        void HandleMouseMove(Point mousepos, MouseButtons mouseButtons);

        void HandleMouseLeave(EventArgs e);

        void Paint(Graphics g, Rectangle rect);
    }
}
