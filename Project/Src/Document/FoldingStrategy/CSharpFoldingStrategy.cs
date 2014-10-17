
namespace ICSharpCode.TextEditor.Document
{
    using System.Collections.Generic;

    /// <summary>CSharpFoldingStrategy TODO2 combine with region stuff below.</summary>
    public class CSharpFoldingStrategy : IFoldingStrategy
    {
        public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
        {
            // This is a simple folding strategy.
            // It searches for matching brackets ('{', '}') and creates folds for each region.
            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            for (int offset = 0; offset < document.TextLength; ++offset)
            {
                char c = document.GetCharAt(offset);

                if (c == '{')
                {
                    int offsetOfClosingBracket = document.FormattingStrategy.SearchBracketForward(document, offset + 1, '{', '}');

                    if (offsetOfClosingBracket > 0)
                    {
                        int length = offsetOfClosingBracket - offset + 1;
                        foldMarkers.Add(new FoldMarker(document, offset, length, "{...}", false));
                    }
                }
            }

            return foldMarkers;
        }

        /// <summary>Generates the foldings for our document.</summary>
        /// <param name="document">The current document.</param>
        /// <param name="fileName">The filename of the document.</param>
        /// <param name="parseInformation">Extra parse information, not used in this sample.</param>
        /// <returns>A list of FoldMarkers.</returns>
        public List<FoldMarker> GenerateFoldMarkersRegion(IDocument document, string fileName, object parseInformation)
        {
            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            Stack<int> startLines = new Stack<int>();

            // Create foldmarkers for the whole document, enumerate through every line.
            for (int i = 0; i < document.TotalNumberOfLines; i++)
            {
                var seg = document.GetLineSegment(i);
                int offs = 0;
                int end = document.TextLength;
                char c;

                for (offs = seg.Offset; offs < end && ((c = document.GetCharAt(offs)) == ' ' || c == '\t'); offs++)
                {
                }

                if (offs == end)
                {
                    break;
                }

                int spaceCount = offs - seg.Offset;

                // now offs points to the first non-whitespace char on the line
                if (document.GetCharAt(offs) == '#')
                {
                    string text = document.GetText(offs, seg.Length - spaceCount);
                    if (text.StartsWith("#region"))
                    {
                        startLines.Push(i);
                    }

                    if (text.StartsWith("#endregion") && startLines.Count > 0)
                    {
                        // Add a new FoldMarker to the list.
                        int start = startLines.Pop();
                        foldMarkers.Add(new FoldMarker(document, start, document.GetLineSegment(start).Length, i, spaceCount + "#endregion".Length));
                    }
                }
            }

            return foldMarkers;
        }
    }
}