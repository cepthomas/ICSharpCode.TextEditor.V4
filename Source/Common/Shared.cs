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


        public static FontContainer FontContainer { get; set; }

        public static void SetContainerFont(Font font)
        {
            FontContainer = new FontContainer(font);
        }

        public static void Init(string appDir)
        {
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();

            TEP = TextEditorProperties.Load(appDir);

            SetContainerFont(TEP.Font);
        }

        public static void Save()
        {
            TEP.Save();
        }
    }
}
