// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Drawing.Text;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
    public enum BracketMatchingStyle
    {
        Before,
        After
    }

    public class TextEditorProperties
    {
        public bool CaretLine { get; set; }
        public bool AutoInsertCurlyBracket { get; set; } = true;
        public bool HideMouseCursor { get; set; }
        public bool IsIconBarVisible { get; set; }
        public bool AllowCaretBeyondEOL { get; set; }
        public bool ShowMatchingBracket { get; set; } = true;
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;
        public bool MouseWheelScrollDown { get; set; } = true;
        public bool MouseWheelTextZoom { get; set; } = true;
        public string LineTerminator { get; set; } = Environment.NewLine;
        public LineViewerStyle LineViewerStyle { get; set; } = LineViewerStyle.None;
        public bool ShowInvalidLines { get; set; }
        public int VerticalRulerRow { get; set; } = 80;
        public bool ShowSpaces { get; set; }
        public bool ShowTabs { get; set; }
        public bool ShowEOLMarker { get; set; }
        public bool ConvertTabsToSpaces { get; set; } 
        public bool ShowHorizontalRuler { get; set; }
        public bool ShowVerticalRuler { get; set; } = true;
        public Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
        public bool EnableFolding { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public int TabIndent { get; set; } = 4;
        public int IndentationSize { get; set; } = 4;
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Smart;
        public DocumentSelectionMode DocumentSelectionMode { get; set; } = DocumentSelectionMode.Normal;
        public Font Font { get; set; } = DefaultFont;
        public FontContainer FontContainer { get; private set; } = new FontContainer(DefaultFont);
        public BracketMatchingStyle BracketMatchingStyle { get; set; } = BracketMatchingStyle.After;
        public bool SupportReadOnlySegments { get; set; }

        // Colors relocated from highlighting environment. TODO I did this.
        public HighlightColor DefaultColor { get; set; } = new HighlightBackground("WindowText", "Window", false, false);
        public HighlightColor CaretMarkerColor { get; set; } = new HighlightColor(Color.Yellow, false, false);
        public HighlightColor SelectionColor { get; set; } = new HighlightColor("HighlightText", "Highlight", false, false);
        public HighlightColor EOLMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor SpaceMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor TabMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor InvalidLinesColor { get; set; } = new HighlightColor(Color.Red, false, false);
        public HighlightColor CaretLineColor { get; set; } = new HighlightBackground("ControlLight", "Window", false, false);
        public HighlightColor LineNumbersColor { get; set; } = new HighlightBackground("ControlDark", "Window", false, false);
        public HighlightColor FoldLineColor { get; set; } = new HighlightColor("ControlDark", false, false);
        public HighlightColor FoldMarkerColor { get; set; } = new HighlightColor("WindowText", "Window", false, false);
        public HighlightColor SelectedFoldLineColor { get; set; } = new HighlightColor("WindowText", false, false);
        public HighlightColor VRulerColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);

        static Font DefaultFont = new Font("Consolas", 10);
    }
}
