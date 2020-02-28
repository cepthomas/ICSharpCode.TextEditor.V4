// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
    public class FileSyntaxModeProvider : ISyntaxModeFileProvider
    {
        string _directory;

        public ICollection<SyntaxMode> SyntaxModes { get; private set; }

        public FileSyntaxModeProvider(string directory)
        {
            _directory = directory;
            SyntaxModes = ScanDirectory(_directory);
        }

        public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
        {
            string syntaxModeFile = Path.Combine(_directory, syntaxMode.FileName);
            if (!File.Exists(syntaxModeFile))
            {
                throw new HighlightingDefinitionInvalidException("Can't load highlighting definition " + syntaxModeFile + " (file not found)!");
            }
            return new XmlTextReader(File.OpenRead(syntaxModeFile));
        }

        List<SyntaxMode> ScanDirectory(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<SyntaxMode> modes = new List<SyntaxMode>();

            foreach (string file in files)
            {
                if (Path.GetExtension(file).ToUpper() == ".XSHD")
                {
                    using (XmlTextReader reader = new XmlTextReader(file))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "SyntaxDefinition":
                                        modes.Add(new SyntaxMode(Path.GetFileName(file),
                                            reader.GetAttribute("name"), reader.GetAttribute("folding"), reader.GetAttribute("extensions")));
                                        break;

                                    default:
                                        break; //throw new HighlightingDefinitionInvalidException("Unknown root node in syntax highlighting file :" + reader.Name);
                                }
                            }
                        }
                    }
                }
            }
            return modes;
        }
    }
}
