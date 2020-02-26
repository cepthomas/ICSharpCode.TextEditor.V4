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

namespace ICSharpCode.TextEditor.Document
{
    public class HighlightingManager
    {
        /// <summary>The one and only instance.</summary>
        private static HighlightingManager _instance;

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

        /// <summary>Public instance accessor.
        /// hash table from extension name to highlighting definition,
        ///   OR from extension name to Pair SyntaxMode,ISyntaxModeFileProvider
        /// </summary>
        public Dictionary<string, object> HighlightingDefinitions { get; private set; }
        
        /// <summary>Alll extension names.</summary>
        public List<string> AllExtensions { get; private set; }
        
        Dictionary<string, string> _extensionsToName;

        List<ISyntaxModeFileProvider> _syntaxModeFileProviders = new List<ISyntaxModeFileProvider>();

        public event EventHandler ReloadSyntaxHighlighting;

        /// <summary>Private default constructor.</summary>
        private HighlightingManager()
        {
            HighlightingDefinitions = new Dictionary<string, object>();
            _extensionsToName = new Dictionary<string, string>();
            AllExtensions = new List<string>();
            //Manager = new HighlightingManager();
            // was: highlightingManager.AddSyntaxModeFileProvider(new ResourceSyntaxModeProvider());
            CreateDefaultHighlightingStrategy();
            AddSyntaxModeFileProvider(new FileSyntaxModeProvider("SyntaxDefinition"));
        }

        void AddSyntaxModeFileProvider(ISyntaxModeFileProvider syntaxModeFileProvider)
        {
            foreach (SyntaxMode syntaxMode in syntaxModeFileProvider.SyntaxModes)
            {
                HighlightingDefinitions[syntaxMode.Name] = new Tuple<SyntaxMode, ISyntaxModeFileProvider>(syntaxMode, syntaxModeFileProvider);
                foreach (string extension in syntaxMode.Extensions)
                {
                    _extensionsToName[extension.ToUpperInvariant()] = syntaxMode.Name;
                }
            }

            if (!_syntaxModeFileProviders.Contains(syntaxModeFileProvider))
            {
                _syntaxModeFileProviders.Add(syntaxModeFileProvider);
            }

            foreach (string s in HighlightingDefinitions.Keys)
            {
                AllExtensions.Add(s);
            }
            AllExtensions.Sort();
        }

        void CreateDefaultHighlightingStrategy()
        {
            DefaultHighlightingStrategy defaultHighlightingStrategy = new DefaultHighlightingStrategy();
            defaultHighlightingStrategy.Extensions = new string[] {};
            defaultHighlightingStrategy.Rules.Add(new HighlightRuleSet());
            HighlightingDefinitions["Default"] = defaultHighlightingStrategy;
        }

        IHighlightingStrategy LoadDefinition(Tuple<SyntaxMode, ISyntaxModeFileProvider> entry)
        {
            SyntaxMode              syntaxMode             = entry.Item1;
            ISyntaxModeFileProvider syntaxModeFileProvider = entry.Item2;

            DefaultHighlightingStrategy highlightingStrategy = null;
            try
            {
                var reader = syntaxModeFileProvider.GetSyntaxModeFile(syntaxMode);
                if (reader == null)
                    throw new HighlightingDefinitionInvalidException("Could not get syntax mode file for " + syntaxMode.Name);
                highlightingStrategy = HighlightingDefinitionParser.Parse(syntaxMode, reader);
                if (highlightingStrategy.Name != syntaxMode.Name)
                {
                    throw new HighlightingDefinitionInvalidException("The name specified in the .xshd '" + highlightingStrategy.Name + "' must be equal the syntax mode name '" + syntaxMode.Name + "'");
                }
            }
            finally
            {
                if (highlightingStrategy == null)
                {
                    highlightingStrategy = DefaultHighlighting;
                }
                HighlightingDefinitions[syntaxMode.Name] = highlightingStrategy;
                highlightingStrategy.ResolveReferences();
            }
            return highlightingStrategy;
        }

        public DefaultHighlightingStrategy DefaultHighlighting
        {
            get
            {
                return (DefaultHighlightingStrategy)HighlightingDefinitions["Default"];
            }
        }

        internal KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> FindHighlighterEntry(string name)
        {
            foreach (ISyntaxModeFileProvider provider in _syntaxModeFileProviders)
            {
                foreach (SyntaxMode mode in provider.SyntaxModes)
                {
                    if (mode.Name == name)
                    {
                        return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(mode, provider);
                    }
                }
            }
            return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(null, null);
        }

        public IHighlightingStrategy FindHighlighter(string name)
        {
            object def = HighlightingDefinitions[name];
            if (def is Tuple<SyntaxMode, ISyntaxModeFileProvider>)
            {
                return LoadDefinition(def as Tuple<SyntaxMode, ISyntaxModeFileProvider>);
            }
            return def == null ? DefaultHighlighting : (IHighlightingStrategy)def;
        }

        public IHighlightingStrategy FindHighlighterForFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToUpperInvariant();
            if(_extensionsToName.TryGetValue(ext, out string highlighterName))
            {
                object def = HighlightingDefinitions[highlighterName];
                if (def is Tuple<SyntaxMode, ISyntaxModeFileProvider>)
                {
                    return LoadDefinition(def as Tuple<SyntaxMode, ISyntaxModeFileProvider>);
                }
                return def == null ? DefaultHighlighting : (IHighlightingStrategy)def;
            }
            else
            {
                return DefaultHighlighting;
            }
        }

        protected virtual void OnReloadSyntaxHighlighting(EventArgs e)
        {
            if (ReloadSyntaxHighlighting != null)
            {
                ReloadSyntaxHighlighting(this, e);
            }
        }
    }
}
