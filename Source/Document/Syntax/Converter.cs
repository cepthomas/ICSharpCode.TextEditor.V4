using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace ICSharpCode.TextEditor.Document
{
    public class SyntaxModesConverter
    {
        List<SyntaxDefinition> syntaxes = new List<SyntaxDefinition>();

        public void DoIt()
        {
            return;


            FileSyntaxModeProvider fsp = new FileSyntaxModeProvider(@"C:\Dev\repos\ICSharpCode.TextEditor.V4\Source\SyntaxDefinition");

            // IHighlightingStrategy stratx = HighlightingStrategyFactory.CreateHighlightingStrategy("C++");

            //        public IHighlightingStrategy FindHighlighterForFile(string fileName)


            Copy("default", "xxx");
            Copy("csharp", "cs");
            Copy("cpp", "cpp");
            Copy("html", "html");
            Copy("java", "java");
            Copy("js", "s");
            Copy("lua", "lua");
            Copy("md", "md");
            Copy("diff", "diff");
            Copy("php", "php");
            Copy("vbnet", "vb");
            Copy("xml", "xml");
            Copy("bat", "bat");



            void Copy(string name, string ext)//, IHighlightingStrategy strat)
            {
                var hl = HighlightingManager.Instance.FindHighlighterForFile("booga." + ext) as DefaultHighlightingStrategy;

                SyntaxDefinition syntax = new SyntaxDefinition();

                // Simple copies.
                syntax.Folding = hl.Folding;
                syntax.Properties = hl.Properties;
                syntax.Name = hl.Name;
                syntax.DigitColor = hl.DigitColor;
                syntax.DefaultTextColor = hl.DefaultTextColor;

                // Slightly complicated.
                foreach(string sext in hl.Extensions)
                {
                    syntax.Extensions.Add(sext.ToLower().Replace(".", ""));
                }

                // More complicated.
                foreach(var rule in hl.Rules)
                {
                    RuleSetX rules = new RuleSetX();

                    rules.Name = rule.Name;
                    //rules.KeyWords = rule.KeyWords;
                    //rules.Delimiters = rule.Delimiters;
                    rules.EscapeCharacter = rule.EscapeCharacter;
                    rules.IgnoreCase = rule.IgnoreCase;
                    rules.Reference = rule.Reference;

                    //rules.Spans
                    //rules.PrevMarkers
                    //rules.NextMarkers




                    syntax.Rules.Add(rules);

                }
                
                //syntax.Rules = hl.Rules;


                //"KeyWords": {
                //"Count": 142
                //},
                //"PrevMarkers": {
                //"Count": 1
                //},
                //"NextMarkers": {
                //"Count": 0
                //},

            //<MarkPrevious bold = "true" italic = "false" color = "MidnightBlue">(</MarkPrevious>
			//<MarkFollowing markmarker ="true" bold = "true" italic = "false" color = "MidnightBlue">\</MarkFollowing>
			//<KeyWords name = "Punctuation" bold = "false" italic = "false" color = "DarkGreen">
			//	<Key word = "?" />
			//	<Key word = "&lt;" />
			//	<Key word = "|" />
			//	<Key word = "&amp;" />
			//</KeyWords>


                //public class HighlightRuleSet
                //{
                //internal IHighlightingStrategyUsingRuleSets Highlighter; // TODO0 messed up binding
                //public LookupTable KeyWords { get; }
                //public LookupTable PrevMarkers { get; }
                //public LookupTable NextMarkers { get; }


                //nodes = el.GetElementsByTagName("KeyWords");
                //foreach (XmlElement el2 in nodes)
                //{
                //    HighlightColor color = new HighlightColor(el2);
                //    XmlNodeList keys = el2.GetElementsByTagName("Key");
                //    foreach (XmlElement node in keys)
                //    {
                //        KeyWords[node.Attributes["word"].InnerText] = color;
                //    }
                //}

                //nodes = el.GetElementsByTagName("MarkPrevious");
                //foreach (XmlElement el2 in nodes)
                //{
                //    PrevMarker prev = new PrevMarker(el2);
                //    PrevMarkers[prev.What] = prev;
                //}

                //nodes = el.GetElementsByTagName("MarkFollowing");
                //foreach (XmlElement el2 in nodes)
                //{
                //    NextMarker next = new NextMarker(el2);
                //    NextMarkers[next.What] = next;
                //}


                syntaxes.Add(syntax);

                syntax.Save(name + ".syntax");
            }
        }
    }

    public class PrevMarkerX
    {
        public string What { get; set; }

        public bool Bold { get; } = false;

        public bool Italic { get; } = false;

        public Color Color { get; } = Color.Black;

        public bool MarkMarker { get; set; }
    }

    public class NextMarkerX
    {
        public string What { get; set; }

        public bool Bold { get; } = false;

        public bool Italic { get; } = false;

        public Color Color { get; } = Color.Black;

        public bool MarkMarker { get; set; }
    }

    public class SpanX
    {
        internal HighlightRuleSet RuleSet { get; set; }

        public bool IgnoreCase { get; set; }

        public bool StopEOL { get; set; }

        public bool? IsBeginStartOfLine { get; set; }

        public bool IsBeginSingleWord { get; set; }

        public bool IsEndSingleWord { get; set; }

        public HighlightColor Color { get; set; }

        public HighlightColor BeginColor
        {
            get
            {
                if (BeginColor != null)
                {
                    return BeginColor;
                }
                else
                {
                    return Color;
                }
            }
        }

        public HighlightColor EndColor
        {
            get
            {
                return EndColor != null ? EndColor : Color;
            }
        }

        public char[] Begin { get; set; }

        public char[] End { get; set; }

        public string Name { get; set; }

        public string Rule { get; set; }

        /// <summary>
        /// Gets the escape character of the span. The escape character is a character that can be used in front
        /// of the span end to make it not end the span. The escape character followed by another escape character
        /// means the escape character was escaped like in @"a "" b" literals in C#.
        /// The default value '\0' means no escape character is allowed.
        /// </summary>
        public char EscapeCharacter { get; set; }
    }

    public class KeywordsX
    {
        public string Name { get; set; } = "";

        public bool Bold { get; } = false;

        public bool Italic { get; } = false;

        public Color Color { get; } = Color.Black;

        public List<string> Words { get; set; } = new List<string>();
    }

    public class RuleSetX
    {
        //internal IHighlightingStrategyUsingRuleSets Highlighter; // TODO0 messed up binding

        public List<SpanX> Spans { get; set; } = new List<SpanX>();

        public List<string> KeyWords { get; set; } = new List<string>();

        public List<PrevMarkerX> PrevMarkers { get; set; } = new List<PrevMarkerX>();

        public List<NextMarkerX> NextMarkers { get; set; } = new List<NextMarkerX>();

        public string Delimiters { get; set; } = "";

        public char EscapeCharacter { get; set; }

        public bool IgnoreCase { get; set; } = false;

        public string Name { get; set; } = null;

        public string Reference { get; set; } = null;
    }


    [Serializable]
    public class SyntaxDefinition //: IHighlightingStrategyUsingRuleSets TODO0 colors should be in a theme file.
    {
        public string Name { get; set; } = "???";

        public List<string> Extensions { get; set; } = new List<string>();

        public string Folding { get; set; } = "";

        public HighlightColor DefaultTextColor { get; set; } = new HighlightColor(SystemColors.WindowText, false, false);

        public HighlightColor DigitColor { get; set; } = new HighlightColor(SystemColors.WindowText, false, false);

        public List<RuleSetX> Rules { get; set; } = new List<RuleSetX>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save(string fn)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static SyntaxDefinition Load(string fn)
        {
            SyntaxDefinition syntax = null;
            string json = File.ReadAllText(fn);
            syntax = JsonConvert.DeserializeObject<SyntaxDefinition>(json);
            return syntax;
        }
        #endregion

        // original format:
        // <SyntaxDefinition name = "C++" folding="Code" extensions = ".c;.h;.cc;.C;.cpp;.hpp">
        //     <Properties>
        //         <Property name="LineComment" value="//"/>
        //     </Properties>
        //     
        //     <Digits name = "Digits" bold = "false" italic = "false" color = "DarkBlue"/>
        //     
        //     <RuleSets>
        //         <RuleSet ignorecase = "false">
        //             <Delimiters>~!%^*()-+=|\#/{}[]:;"'&lt;&gt; , .?</Delimiters>
        //         
        //             <Span name = "PreprocessorDirectives" bold="false" italic="false" color="Green" stopateol = "true">
        //                 <Begin>#</Begin>
        //             </Span>
        //             <Span name="ScriptTag" rule="JavaScriptSet" bold="false" italic="false" color="SpringGreen" stopateol="false">
        //                 <Begin>&lt;script&gt;</Begin>
        //                 <End>&lt;/script&gt;</End>
        //             </Span>
        //     
        //             
        //             <MarkPrevious bold = "true" italic = "false" color = "MidnightBlue">(</MarkPrevious>
        //             
        //             <KeyWords name = "Punctuation" bold = "false" italic = "false" color = "DarkGreen">
        //                 <Key word = "?" />
        //                 <Key word = "," />
        //             </KeyWords>
        //             
        //             <KeyWords name = "KEYWORD1" bold="true" italic="false" color="Blue">
        //                 <Key word = "asm" />
        //                 <Key word = "auto" />
        //                 <Key word = "compl" />
        //             </KeyWords>
        //         </RuleSet>
        //     </RuleSets>
        // </SyntaxDefinition>


        // TODO0 Avalon format:
        //<SyntaxDefinition name = "C#"
        //    xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
        //    <Color name = "Comment" foreground="Green" />
        //    <Color name = "String" foreground="Blue" />

        //    <!-- This is the main ruleset. -->
        //    <RuleSet>
        //        <Span color = "Comment" begin="//" />
        //        <Span color = "Comment" multiline="true" begin="/\*" end="\*/" />

        //        <Span color = "String" >
        //            < Begin > "</Begin>
        //            < End > "</End>
        //            < RuleSet >
        //                < !--nested span for escape sequences -->
        //                <Span begin = "\\" end="." />
        //            </RuleSet>
        //        </Span>

        //        <Keywords fontWeight = "bold" foreground="Blue">
        //            <Word>if</Word>
        //            <Word>else</Word>
        //            <!-- ... -->
        //        </Keywords>

        //        <!-- Digits -->
        //        <Rule foreground = "DarkBlue" >
        //            \b0[xX][0 - 9a - fA - F]+  # hex number
        //        |    \b
        //            (    \d+(\.[0-9]+)?   #number with optional floating point
        //            |    \.[0-9]+         #or just starting with floating point
        //            )
        //            ([eE][+-]?[0 - 9]+)? # optional exponent
        //        </Rule>
        //    </RuleSet>
        //</SyntaxDefinition>

    }
}
