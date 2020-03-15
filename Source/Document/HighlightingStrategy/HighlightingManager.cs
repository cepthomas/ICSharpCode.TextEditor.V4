// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
    public class HighlightingManager
    {
        #region Fields
        /// <summary>The one and only instance.</summary>
        static HighlightingManager _instance;
        #endregion

        #region Properties
        /// <summary>Public instance accessor.</summary>
        public static HighlightingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HighlightingManager();
                }
                return _instance;
            }
        }
        
        /// <summary>Maps extension to highlighting definition,</summary>
        public Dictionary<string, HighlightingStrategy> HighlightingDefinitions { get; private set; } = new Dictionary<string, HighlightingStrategy>();
        #endregion

        /// <summary>Private default constructor.</summary>
        HighlightingManager()
        {
            LoadSyntaxDefs();
        }

        /// <summary>
        /// 
        /// </summary>
        void LoadSyntaxDefs()
        {
            string[] files = Directory.GetFiles("SyntaxDefinition");

            foreach (string file in files)
            {
                if (Path.GetExtension(file).ToLower() == ".xshd")
                {
                    using (XmlTextReader reader = new XmlTextReader(file))
                    {
                        HighlightingStrategy highlightingStrategy = HighlightingDefinitionParser.Parse(reader);
                        if (highlightingStrategy != null)
                        {
                            foreach (string ext in highlightingStrategy.Extensions)
                            {
                                HighlightingDefinitions[ext.ToLower()] = highlightingStrategy;
                            }
                        }
                    }
                }
            }

            // Resolve references.
            foreach (var val in HighlightingDefinitions.Values)
            {
                val.ResolveReferences();
            }
        }

        public HighlightingStrategy FindHighlighter(string name) // by name
        {
            return HighlightingDefinitions.ContainsKey(name) ? HighlightingDefinitions[name] : new HighlightingStrategy("Default");
        }

        public HighlightingStrategy FindHighlighterForFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            var hdef = HighlightingDefinitions.ContainsKey(ext) ? HighlightingDefinitions[ext] : new HighlightingStrategy("Default");
            return hdef;
        }
    }
}
