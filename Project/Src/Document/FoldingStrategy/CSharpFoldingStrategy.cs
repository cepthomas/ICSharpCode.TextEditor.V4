
namespace ICSharpCode.TextEditor.Document
{
    using System.Collections.Generic;

    //public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    //{
    //    List<NewFolding> newFoldings = new List<NewFolding>();

    //    Stack<int> startOffsets = new Stack<int>();
    //    int lastNewLineOffset = 0;
    //    char openingBrace = this.OpeningBrace;
    //    char closingBrace = this.ClosingBrace;
    //    for (int i = 0; i < document.TextLength; i++)
    //    {
    //        char c = document.GetCharAt(i);
    //        if (c == openingBrace)
    //        {
    //            startOffsets.Push(i);
    //        }
    //        else if (c == closingBrace && startOffsets.Count > 0)
    //        {
    //            int startOffset = startOffsets.Pop();
    //            // don't fold if opening and closing brace are on the same line
    //            if (startOffset < lastNewLineOffset) 
    //            {
    //                newFoldings.Add(new NewFolding(startOffset, i + 1));
    //            }
    //        }
    //        else if (c == '\n' || c == '\r')
    //        {
    //            lastNewLineOffset = i + 1;
    //        }
    //    }
    //    newFoldings.Sort((a,b) => a.StartOffset.CompareTo(b.StartOffset));
    //    return newFoldings;
    //}


    /// <summary>CSharpFoldingStrategy TODO1 combine with region stuff below. Then fix up CodeFoldingStrategy. Maybe a cpp/h strategy too with base class.</summary>
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


        //public FoldMarker(IDocument document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType, string foldText)


        //    public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        //{
        //    var newFoldings = new List<NewFolding>();

        //    var startOffsets = new Stack<int>();
        //    int lastNewLineOffset = 0;
        //    char openingBrace = this.OpeningBrace;
        //    char closingBrace = this.ClosingBrace;
        //    for (int i = 0; i < document.TextLength; i++)
        //    {
        //        char c = document.GetCharAt(i);
        //        if (c == openingBrace)
        //        {
        //            startOffsets.Push(i);
        //        }
        //        else if (c == closingBrace && startOffsets.Count > 0)
        //        {
        //            int startOffset = startOffsets.Pop();
        //            // don't fold if opening and closing brace are on the same line
        //            if (startOffset < lastNewLineOffset)
        //            {
        //                newFoldings.Add(new NewFolding(startOffset, i + 1));
        //            }
        //        }
        //        else if (c == '\n' || c == '\r')
        //        {
        //            lastNewLineOffset = i + 1;
        //        }
        //    }
        //    newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        //    return newFoldings;
        //}


    }
}