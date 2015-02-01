// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
    public enum BracketMatchingStyle
    {
        Before,
        After
    }

    public class DefaultTextEditorProperties : ITextEditorProperties
    {
        public bool CaretLine { get; set; }
        public bool AutoInsertCurlyBracket { get; set; } 
        public bool HideMouseCursor { get; set; }
        public bool IsIconBarVisible { get; set; }
        public bool AllowCaretBeyondEOL { get; set; }
        public bool ShowMatchingBracket { get; set; }
        public bool CutCopyWholeLine { get; set; }
        public System.Drawing.Text.TextRenderingHint TextRenderingHint { get; set; }
        public bool MouseWheelScrollDown { get; set; }
        public bool MouseWheelTextZoom { get; set; }
        public string LineTerminator { get; set; }
        public LineViewerStyle LineViewerStyle { get; set; }
        public bool ShowInvalidLines { get; set; }
        public int VerticalRulerRow { get; set; }
        public bool ShowSpaces { get; set; }
        public bool ShowTabs { get; set; }
        public bool ShowEOLMarker { get; set; }
        public bool ConvertTabsToSpaces { get; set; } 
        public bool ShowHorizontalRuler { get; set; }
        public bool ShowVerticalRuler { get; set; }
        public Encoding Encoding { get; set; }
        public bool EnableFolding { get; set; }
        public bool ShowLineNumbers { get; set; }
        public int TabIndent { get; set; }
        public int IndentationSize { get; set; }
        public IndentStyle IndentStyle { get; set; }
        public DocumentSelectionMode DocumentSelectionMode { get; set; }
        public Font Font { get; set; }
        public FontContainer FontContainer { get; private set; }
        public BracketMatchingStyle BracketMatchingStyle { get; set; } 
        public bool SupportReadOnlySegments { get; set; }

        // Colors relocated from highlighting environment.
        public HighlightColor DefaultColor { get; set; }
        public HighlightColor CaretMarkerColor { get; set; }
        public HighlightColor SelectionColor { get; set; }
        public HighlightColor EOLMarkersColor { get; set; }
        public HighlightColor SpaceMarkersColor { get; set; }
        public HighlightColor TabMarkersColor { get; set; }
        public HighlightColor InvalidLinesColor { get; set; }
        public HighlightColor CaretLineColor { get; set; }
        public HighlightColor LineNumbersColor { get; set; }
        public HighlightColor FoldLineColor { get; set; }
        public HighlightColor FoldMarkerColor { get; set; }
        public HighlightColor SelectedFoldLineColor { get; set; }
        public HighlightColor VRulerColor { get; set; }

        //FontContainer fontContainer;
        static Font DefaultFont = new Font("Consolas", 10);

        public DefaultTextEditorProperties()
        {
            //DefaultFont = new Font("Consolas", 10);
            FontContainer = new FontContainer(DefaultFont);
            Font = DefaultFont;

            TabIndent = 4;
            IndentationSize = 4;
            IndentStyle = IndentStyle.Smart;
            DocumentSelectionMode = DocumentSelectionMode.Normal;
            Encoding = System.Text.Encoding.UTF8;
            BracketMatchingStyle  = BracketMatchingStyle.After;

            ShowMatchingBracket = true;
            ShowLineNumbers = true;
            EnableFolding = true;
            ShowVerticalRuler = true;
            TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            MouseWheelScrollDown = true;
            MouseWheelTextZoom = true;
            CutCopyWholeLine = true;

            VerticalRulerRow = 80;
            LineViewerStyle = LineViewerStyle.None;
            LineTerminator = "\r\n";
            AutoInsertCurlyBracket = true;

            DefaultColor = new HighlightBackground("WindowText", "Window", false, false);
            SelectionColor = new HighlightColor("HighlightText", "Highlight", false, false);
            VRulerColor = new HighlightColor("ControlLight", "Window", false, false);
            InvalidLinesColor = new HighlightColor(Color.Red, false, false);
            CaretMarkerColor = new HighlightColor(Color.Yellow, false, false);
            CaretLineColor = new HighlightBackground("ControlLight", "Window", false, false);
            LineNumbersColor = new HighlightBackground("ControlDark", "Window", false, false);
            FoldLineColor = new HighlightColor("ControlDark", false, false);
            FoldMarkerColor = new HighlightColor("WindowText", "Window", false, false);
            SelectedFoldLineColor = new HighlightColor("WindowText", false, false);
            EOLMarkersColor = new HighlightColor("ControlLight", "Window", false, false);
            SpaceMarkersColor = new HighlightColor("ControlLight", "Window", false, false);
            TabMarkersColor = new HighlightColor("ControlLight", "Window", false, false);
        }
    }
}
