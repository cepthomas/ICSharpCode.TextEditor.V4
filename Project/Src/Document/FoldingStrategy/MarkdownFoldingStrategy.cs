
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>Description of MarkdownFoldingStrategy.</summary>
    public class MarkdownFoldingStrategy : IFoldingStrategy
    {
        #region Methods

        // TODO1 Subset of markdown heading folding. Also Markdown.xshd.

        //This is an H1
        //=============
        //This is an H2
        //-------------

        //# This is an H1
        //## This is an H2
        //###### This is an H6

        public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
        {
            // This is a simple folding strategy.

            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            foreach (LineSegment lseg in document.LineSegmentCollection)
            {
                // Check first word for marker.
                if (lseg.Words.Count > 0 && lseg.Words[0].Word.Length > 0)
                {
                    char first = lseg.Words[0].Word[0];

                    if (first == '=' || first == '-' || first == '#')
                    {
                        //int offsetOfClosingBracket = document.FormattingStrategy.SearchBracketForward(document, offset + 1, '{', '}');
                        //int length = offsetOfClosingBracket - offset + 1;
                        //foldMarkers.Add(new FoldMarker(document, offset, length, "{...}", false));
                    }
                }
            }

            return foldMarkers;
        }

        #endregion Methods
    }
}