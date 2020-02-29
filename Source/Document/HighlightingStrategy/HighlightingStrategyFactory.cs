// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>


//#define NEW_SYNTAX


using System;
using System.Collections.Generic;
using System.IO;


namespace ICSharpCode.TextEditor.Document
{
    public class HighlightingStrategyFactory
    {
#if NEW_SYNTAX
        static Dictionary<string, Syntax> _syntaxes = new Dictionary<string, Syntax>();
        static Syntax _defSyntax = new Syntax();
        static void Init()
        {
            if(_syntaxes.Count == 0)
            {
                foreach (string fn in Directory.GetFiles("SyntaxDefinition", "*.syntax"))
                {
                    Syntax syn = Syntax.Load(fn);
                    if (syn.Name == "default")
                    {
                        _defSyntax = syn;
                    }
                    else
                    {
                        syn.Extensions.ForEach(e => _syntaxes.Add(e, syn));
                    }
                }
            }
        }

        public static Syntax CreateHighlightingStrategy() // default
        {
            Init();
            return _defSyntax;
        }

        public static Syntax CreateHighlightingStrategy(string name) // by syntax name
        {
            Init();
            var ret = _syntaxes.ContainsKey(name) ? _syntaxes[name] : _defSyntax;
            return ret;
        }

        public static Syntax CreateHighlightingStrategyForFile(string fileName) // by filename.ext
        {
            Init();
            string ext = Path.GetExtension(fileName).ToLower().Replace(".", "");
            var ret = _syntaxes.ContainsKey(ext) ? _syntaxes[ext] : _defSyntax;
            return ret;
        }

#else

        public static IHighlightingStrategy CreateHighlightingStrategy() // default
        {
            return (IHighlightingStrategy)HighlightingManager.Instance.HighlightingDefinitions["Default"];
        }

        public static IHighlightingStrategy CreateHighlightingStrategy(string name) // by syntax name
        {
            IHighlightingStrategy highlightingStrategy = HighlightingManager.Instance.FindHighlighter(name);
            return highlightingStrategy == null ? CreateHighlightingStrategy() : highlightingStrategy;
        }

        public static IHighlightingStrategy CreateHighlightingStrategyForFile(string fileName) // by filename.ext
        {
            IHighlightingStrategy highlightingStrategy = HighlightingManager.Instance.FindHighlighterForFile(fileName);
            return highlightingStrategy == null ? CreateHighlightingStrategy() : highlightingStrategy;
        }
#endif

    }
}
