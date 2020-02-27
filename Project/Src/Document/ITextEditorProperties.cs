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
    public interface ITextEditorProperties // TODO1 needed?
    {
        bool CaretLine { get; set; }
        bool AutoInsertCurlyBracket { get; set; }   // is wrapped in text editor control
        bool HideMouseCursor { get; set; }   // is wrapped in text editor control
        bool IsIconBarVisible { get; set; }   // is wrapped in text editor control
        bool AllowCaretBeyondEOL { get; set; }
        bool ShowMatchingBracket { get; set; }   // is wrapped in text editor control
        TextRenderingHint TextRenderingHint { get; set; }   // is wrapped in text editor control
        bool MouseWheelScrollDown { get; set; }
        bool MouseWheelTextZoom { get; set; }
        string LineTerminator { get; set; }
        LineViewerStyle LineViewerStyle { get; set; }   // is wrapped in text editor control
        bool ShowInvalidLines { get; set; }   // is wrapped in text editor control
        int VerticalRulerRow { get; set; }   // is wrapped in text editor control
        bool ShowSpaces { get; set; }   // is wrapped in text editor control
        bool ShowTabs { get; set; }   // is wrapped in text editor control
        bool ShowEOLMarker { get; set; }   // is wrapped in text editor control
        bool ConvertTabsToSpaces { get; set; }   // is wrapped in text editor control
        bool ShowHorizontalRuler { get; set; }   // is wrapped in text editor control
        bool ShowVerticalRuler { get; set; }   // is wrapped in text editor control
        Encoding Encoding { get; set; }
        bool EnableFolding { get; set; }   // is wrapped in text editor control
        bool ShowLineNumbers { get; set; }   // is wrapped in text editor control
        int TabIndent { get; set; }   // is wrapped in text editor control
        int IndentationSize { get; set; } // The amount of spaces a tab is converted to if ConvertTabsToSpaces is true.
        IndentStyle IndentStyle { get; set; }   // is wrapped in text editor control
        DocumentSelectionMode DocumentSelectionMode { get; set; }
        Font Font { get; set; }   // is wrapped in text editor control
        FontContainer FontContainer { get; }
        BracketMatchingStyle  BracketMatchingStyle { get; set; }   // is wrapped in text editor control
        bool SupportReadOnlySegments { get; set; }

        // Colors relocated from highlighting environment. TODO1 I did this.
        HighlightColor DefaultColor { get; set; }
        HighlightColor CaretMarkerColor { get; set; }
        HighlightColor SelectionColor { get; set; }
        HighlightColor EOLMarkersColor { get; set; }
        HighlightColor SpaceMarkersColor { get; set; }
        HighlightColor TabMarkersColor { get; set; }
        HighlightColor InvalidLinesColor { get; set; }
        HighlightColor CaretLineColor { get; set; }
        HighlightColor LineNumbersColor { get; set; }
        HighlightColor FoldLineColor { get; set; }
        HighlightColor FoldMarkerColor { get; set; }
        HighlightColor SelectedFoldLineColor { get; set; }
        HighlightColor VRulerColor { get; set; }
    }
}
