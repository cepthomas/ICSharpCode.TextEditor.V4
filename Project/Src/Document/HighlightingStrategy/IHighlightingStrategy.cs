// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// A highlighting strategy for a buffer.
    /// </summary>
    public interface IHighlightingStrategy
    {
        /// <value>
        /// The name of the highlighting strategy, must be unique
        /// </value>
        string Name { get; }

        /// <value>
        /// The name of the folding type. Not technically part of highlighting but best place to keep it. TODO?
        /// </value>
        string Folding { get; }

        /// <value>
        /// The file extenstions on which this highlighting strategy gets used
        /// </value>
        string[] Extensions { get; }

        /// <summary>
        /// ???
        /// </summary>
        Dictionary<string, string> Properties { get; }

        /// <remarks>
        /// Used internally, do not call
        /// </remarks>
        void MarkTokens(IDocument document, List<LineSegment> lines);

        /// <remarks>
        /// Used internally, do not call
        /// </remarks>
        void MarkTokens(IDocument document);
    }

    public interface IHighlightingStrategyUsingRuleSets : IHighlightingStrategy
    {
        /// <remarks>
        /// Used internally, do not call
        /// </remarks>
        HighlightRuleSet GetRuleSet(Span span);

        /// <remarks>
        /// Used internally, do not call
        /// </remarks>
        HighlightColor GetColor(IDocument document, LineSegment keyWord, int index, int length);
    }
}
