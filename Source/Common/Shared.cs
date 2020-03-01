using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.TextEditor.Common
{
    public class Shared
    {
        public static TextEditorProperties TEP { get; set; } = new TextEditorProperties();

        public static void Init(string appDir)
        {
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();

            TEP = TextEditorProperties.Load(appDir);
        }


    }
}
