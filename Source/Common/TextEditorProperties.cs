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
        //[Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(System.Drawing.Design.UITypeEditor))]

        #region Editable properties common for all controls

        [Category("Appearance")]
        [Description("Editor font")]
        public Font Font { get; set; } = new Font("Consolas", 9.0f);

        [Category("Appearance")]
        [Description("If true the horizontal ruler is shown in the textarea")]
        public bool ShowHRuler { get; set; } = false;

        [Category("Appearance")]
        [Description("If true the vertical ruler is shown in the textarea")]
        public bool ShowVRuler { get; set; }

        [Category("Appearance")]
        [Description("The row in which the vertical ruler is displayed")]
        public int VRulerRow { get; set; } = 80;

        [Category("Appearance")]
        [Description("Show the caret line")]
        public bool CaretLine { get; set; } = true;

        [Category("Behavior")]
        [Description("Hide the mouse cursor while typing")]
        public bool HideMouseCursor { get; set; } = false;

        [Category("Appearance")]
        [Description("If true the icon bar is displayed")]
        public bool IsIconBarVisible { get; set; } = true;

        [Category("Behavior")]
        [Description("Allows the caret to be placed beyond the end of line")]
        public bool AllowCaretBeyondEOL { get; set; } = false;

        [Category("Appearance")]
        [Description("If true matching brackets are highlighted")]
        public bool ShowMatchingBracket { get; set; } = true;

        [Category("Appearance")]
        [Description("Specifies the quality of text rendering (whether to use hinting and/or anti-aliasing).")]
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;

        [Category("Appearance")]
        [Description("The line viewer style")]
        public LineViewerStyle LineViewerStyle { get; set; } = LineViewerStyle.None;

        [Category("Appearance")]
        [Description("If true invalid lines are marked in the textarea")]
        public bool ShowInvalidLines { get; set; } = false;

        [Category("Appearance")]
        [Description("If true line numbers are shown in the textarea")]
        public bool ShowLineNumbers { get; set; } = true;

        [Category("Appearance")]
        [Description("If true folding is enabled in the textarea")]
        public bool EnableFolding { get; set; } = true;

        [Category("Behavior")]
        [Description("Specifies if the bracket matching should match the bracket before or after the caret.")]
        public BracketMatchingStyle BracketMatchingStyle { get; set; } = BracketMatchingStyle.After;
        #endregion

        #region TODO1 These?
        public DocumentSelectionMode DocumentSelectionMode { get; set; } = DocumentSelectionMode.Normal;

        public int VerticalRulerRow { get; set; } = 80;

        public bool ShowHorizontalRuler { get; set; } = false;

        public bool ShowVerticalRuler { get; set; } = false;
        
        public bool SupportReadOnlySegments { get; set; } = false;

        public bool MouseWheelScrollDown { get; set; } = true;

        public bool MouseWheelTextZoom { get; set; } = true;

        public string LineTerminator { get; set; } = Environment.NewLine;
        
        //public bool AutoInsertCurlyBracket { get; set; } = true;
        #endregion

        #region Stuff json or serialization can't handle TODO1
        [JsonIgnore]
        // TODO1   ASCIIEncoding UnicodeEncoding UTF32Encoding UTF7Encoding UTF8Encoding
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        //public Font Font { get { return _font; } set { _font = value; Shared.SetContainerFont(_font); } }
        #endregion

        #region Properties that can be overriden per control
        [Category("Appearance")]
        [Description("If true spaces are shown in the textarea")]
        public bool ShowSpaces { get; set; } = false;

        [Category("Appearance")]
        [Description("If true tabs are shown in the textarea")]
        public bool ShowTabs { get; set; } = false;

        [Category("Appearance")]
        [Description("If true EOL markers are shown in the textarea")]
        public bool ShowEOLMarker { get; set; } = false;

        [Category("Behavior")]
        [Description("Converts tabs to spaces while typing")]
        public bool ConvertTabsToSpaces { get; set; } = false;

        [Category("Appearance")]
        [Description("The width in spaces of a tab character")]
        public int TabIndent { get; set; } = 4;

        [Category("Appearance")]
        [Description("Used for auto indentation")]
        public int IndentationSize { get; set; } = 4;

        [Category("Behavior")]
        [Description("The indent style")]
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Smart;
        #endregion

        #region Properties for colors TODOsyntax relocated from highlighting environment, put in a style thing when syntax colors get fixed.
        public HighlightColor DefaultColor { get; set; } = new HighlightColor(SystemColors.WindowText, SystemColors.Window, false, false);
        public HighlightColor CaretMarkerColor { get; set; } = new HighlightColor(Color.WhiteSmoke, false, false); // the selection line
        public HighlightColor SelectionColor { get; set; } = new HighlightColor(SystemColors.HighlightText, SystemColors.Highlight, false, false);
        public HighlightColor EOLMarkersColor { get; set; } = new HighlightColor(SystemColors.ControlLight, SystemColors.Window, false, false);
        public HighlightColor SpaceMarkersColor { get; set; } = new HighlightColor(SystemColors.ControlLight, SystemColors.Window, false, false);
        public HighlightColor TabMarkersColor { get; set; } = new HighlightColor(SystemColors.ControlLight, SystemColors.Window, false, false);
        public HighlightColor InvalidLinesColor { get; set; } = new HighlightColor(Color.Red, false, false);
        public HighlightColor CaretLineColor { get; set; } = new HighlightColor(SystemColors.ControlLight, SystemColors.Window, false, false);
        public HighlightColor LineNumbersColor { get; set; } = new HighlightColor(SystemColors.ControlDark, SystemColors.Window, false, false);
        public HighlightColor FoldLineColor { get; set; } = new HighlightColor(SystemColors.ControlDark, false, false);
        public HighlightColor FoldMarkerColor { get; set; } = new HighlightColor(SystemColors.WindowText, SystemColors.Window, false, false);
        public HighlightColor SelectedFoldLineColor { get; set; } = new HighlightColor(SystemColors.WindowText, false, false);
        public HighlightColor VRulerColor { get; set; } = new HighlightColor(SystemColors.ControlLight, SystemColors.Window, false, false);

        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "";

        /// <summary>The font.</summary>
//        Font _font = new Font("Consolas", 9);
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
