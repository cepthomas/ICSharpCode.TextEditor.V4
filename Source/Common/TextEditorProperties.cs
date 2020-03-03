// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ICSharpCode.TextEditor.Document;


namespace ICSharpCode.TextEditor.Common
{
    public enum BracketMatchingStyle
    {
        Before,
        After
    }

    /// <summary>
    /// Properties that apply to all open text editor windows.
    /// </summary>
    [Serializable]
    public class TextEditorProperties
    {
        //TODO2 Add doc like: [Category("\tFavorites"), DisplayName("Filter Favorites"), Description("Filter strings or regular expressions.")]

        #region Properties same for all controls
        public bool CaretLine { get; set; } = false;
        public bool AutoInsertCurlyBracket { get; set; } = true;
        public bool HideMouseCursor { get; set; } = false;
        public bool IsIconBarVisible { get; set; } = false;
        public bool AllowCaretBeyondEOL { get; set; } = true;
        public bool ShowMatchingBracket { get; set; } = true;
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;
        public bool MouseWheelScrollDown { get; set; } = true;
        public bool MouseWheelTextZoom { get; set; } = true;
        public string LineTerminator { get; set; } = Environment.NewLine;
        public LineViewerStyle LineViewerStyle { get; set; } = LineViewerStyle.FullRow; //TODO1 just gutter opt
        public bool ShowInvalidLines { get; set; } = false;
        public int VerticalRulerRow { get; set; } = 80;
        public bool ShowHorizontalRuler { get; set; } = false;
        public bool ShowVerticalRuler { get; set; } = false;
        public bool EnableFolding { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public DocumentSelectionMode DocumentSelectionMode { get; set; } = DocumentSelectionMode.Normal;
        public BracketMatchingStyle BracketMatchingStyle { get; set; } = BracketMatchingStyle.After;
        public bool SupportReadOnlySegments { get; set; } = false;
        #endregion

        #region Stuff json or serialization can't handle TODO1
        [JsonIgnore]
        // TODO1   ASCIIEncoding UnicodeEncoding UTF32Encoding UTF7Encoding UTF8Encoding
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public Font Font { get { return _font; } set { _font = value; Shared.SetContainerFont(_font); } }
        #endregion


        #region Properties that can be overriden per control TODO1
        public bool ShowSpaces { get; set; } = false;
        public bool ShowTabs { get; set; } = false;
        public bool ShowEOLMarker { get; set; } = false;
        public bool ConvertTabsToSpaces { get; set; } = true;
        public int TabIndent { get; set; } = 4;
        public int IndentationSize { get; set; } = 4;
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Smart;
        #endregion

        #region Properties for colors TODOsyntax relocated from highlighting environment, put in a style thing when syntax colors get fixed.
        public HighlightColor DefaultColor { get; set; } = new HighlightColor("WindowText", "Window", false, false);
        public HighlightColor CaretMarkerColor { get; set; } = new HighlightColor(Color.WhiteSmoke, false, false); // the selection line
        public HighlightColor SelectionColor { get; set; } = new HighlightColor("HighlightText", "Highlight", false, false);
        public HighlightColor EOLMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor SpaceMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor TabMarkersColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor InvalidLinesColor { get; set; } = new HighlightColor(Color.Red, false, false);
        public HighlightColor CaretLineColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);
        public HighlightColor LineNumbersColor { get; set; } = new HighlightColor("ControlDark", "Window", false, false);
        public HighlightColor FoldLineColor { get; set; } = new HighlightColor("ControlDark", false, false);
        public HighlightColor FoldMarkerColor { get; set; } = new HighlightColor("WindowText", "Window", false, false);
        public HighlightColor SelectedFoldLineColor { get; set; } = new HighlightColor("WindowText", false, false);
        public HighlightColor VRulerColor { get; set; } = new HighlightColor("ControlLight", "Window", false, false);

        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "";

        /// <summary>The font.</summary>
        Font _font = new Font("Consolas", 9);
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static TextEditorProperties Load(string appDir)
        {
            string fn = Path.Combine(appDir, "editor.settings");

            TextEditorProperties tep;
            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                tep = JsonConvert.DeserializeObject<TextEditorProperties>(json);
            }
            else
            {
                tep = new TextEditorProperties(); // default
            }

            tep._fn = fn;

            return tep;
        }
        #endregion
    }
}
