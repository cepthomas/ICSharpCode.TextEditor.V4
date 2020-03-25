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
    /// Temp class to hold glue stuff. TODO1
    /// </summary>
    public class Shared
    {
        public static TextEditorProperties TEP { get; set; } = new TextEditorProperties();

        public static void Init(string appDir)
        {
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();

            TEP = TextEditorProperties.Load(appDir);

            CMM.LoadMaps(Path.Combine(appDir, "ctlmap.settings"), new List<string>());

            FontRegistry.SetFont(TEP.Font);
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
