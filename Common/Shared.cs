using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.TextEditor.Common
{
    /// <summary>
    /// Temp class to hold global and glue stuff. TODO Probably needs a better home.
    /// </summary>
    public class Shared
    {
        public static TextEditorProperties TEP { get; set; } = new TextEditorProperties();

        public static List<string> Init(string appDir)
        {
            List<string> errors = new List<string>();
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();

            TEP = TextEditorProperties.Load(appDir);

            List<string> userActions = new List<string>();

            errors.AddRange(CMM.LoadMaps(@".\Resources\default.ctlmap", Path.Combine(appDir, "Settings", "custom.ctlmap"),
                Directory.GetFiles(Path.Combine(appDir, "Actions"), "*.cs").ToList()));

            FontRegistry.SetFont(TEP.Font);

            return errors;
        }

        public static ControlMapManager CMM { get; set; } = new ControlMapManager();
    }

    public class FontRegistry
    {
        #region Fonts
        static Font _regularFont;
        static Font _boldFont;
        static Font _italicFont;
        static Font _boldItalicFont;

        public static Font GetFont(bool bold = false, bool italic = false)
        {
            if (bold)
            {
                return italic ? _boldItalicFont : _boldFont;
            }
            return italic ? _italicFont : _regularFont;
        }

        public static void SetFont(Font font)
        {
            _regularFont = font;
            _boldFont = new Font(_regularFont, FontStyle.Bold);
            _italicFont = new Font(_regularFont, FontStyle.Italic);
            _boldItalicFont = new Font(_regularFont, FontStyle.Bold | FontStyle.Italic);
        }
        #endregion
    }
}
