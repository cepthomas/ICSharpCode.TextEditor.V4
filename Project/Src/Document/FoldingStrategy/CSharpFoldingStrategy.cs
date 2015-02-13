// This file has been added to the base project by me.

using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>CSharp folding.</summary>
    public class CSharpFoldingStrategy : IFoldingStrategy
    {
        /// Interface implementation.
        public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
        {
            // The main part is the standard braces parsing.
            CodeFoldingStrategy braces = new CodeFoldingStrategy();
            List<FoldMarker> foldMarkers = braces.GenerateFoldMarkers(document, fileName, parseInformation);

            // Add regions.
            foldMarkers.AddRange(GenerateFoldMarkersRegion(document, fileName, parseInformation));

            foldMarkers.Sort((a, b) => a.Offset.CompareTo(b.Offset));

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

            // Create foldmarkers for the whole document. TODO3 could use some cleanup and improvements.
            for (int i = 0; i < document.TotalNumberOfLines; i++)
            {
                LineSegment seg = document.GetLineSegment(i);
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

                // Now offs points to the first non-whitespace char on the line.
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