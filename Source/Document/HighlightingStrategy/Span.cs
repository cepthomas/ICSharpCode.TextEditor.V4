// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
    public sealed class Span
    {
        #region Fields
        HighlightColor _beginColor;
        HighlightColor _endColor;
        #endregion

        #region Properties
        internal HighlightRuleSet RuleSet { get; set; }

        public bool IgnoreCase { get; set; }

        public bool StopEOL { get; }

        public bool? IsBeginStartOfLine { get; }

        public bool IsBeginSingleWord { get; }

        public bool IsEndSingleWord { get; }

        public HighlightColor Color { get; }

        public HighlightColor BeginColor { get { return _beginColor != null ? _beginColor : Color; } }

        public HighlightColor EndColor { get { return _endColor != null ? _endColor : Color; } }

        public char[] Begin { get; }

        public char[] End { get; }

        public string Name { get; }

        public string Rule { get; }

        /// <summary>
        /// Gets the escape character of the span. The escape character is a character that can be used in front
        /// of the span end to make it not end the span. The escape character followed by another escape character
        /// means the escape character was escaped like in @"a "" b" literals in C#.
        /// The default value '\0' means no escape character is allowed.
        /// </summary>
        public char EscapeCharacter { get; }
        #endregion

        #region Lifecycle
        public Span(XmlElement span)
        {
            Color = new HighlightColor(span);

            if (span.HasAttribute("rule"))
            {
                Rule = span.GetAttribute("rule");
            }

            if (span.HasAttribute("escapecharacter"))
            {
                EscapeCharacter = span.GetAttribute("escapecharacter")[0];
            }

            Name = span.GetAttribute("name");
            if (span.HasAttribute("stopateol"))
            {
                StopEOL = Boolean.Parse(span.GetAttribute("stopateol"));
            }

            Begin = span["Begin"].InnerText.ToCharArray();
            _beginColor = new HighlightColor(span["Begin"], Color);

            if (span["Begin"].HasAttribute("singleword"))
            {
                IsBeginSingleWord = Boolean.Parse(span["Begin"].GetAttribute("singleword"));
            }
            if (span["Begin"].HasAttribute("startofline"))
            {
                IsBeginStartOfLine = Boolean.Parse(span["Begin"].GetAttribute("startofline"));
            }

            if (span["End"] != null)
            {
                End = span["End"].InnerText.ToCharArray();
                _endColor = new HighlightColor(span["End"], Color);
                if (span["End"].HasAttribute("singleword"))
                {
                    IsEndSingleWord = Boolean.Parse(span["End"].GetAttribute("singleword"));
                }

            }
        }
        #endregion
    }
}
