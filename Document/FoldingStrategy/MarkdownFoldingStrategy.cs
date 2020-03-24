// This file has been added to the base project by me.

using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>Description of MarkdownFoldingStrategy. Supports atx (#) headers only.</summary>
    public class MarkdownFoldingStrategy : IFoldingStrategy
    {
        #region Methods
        public List<FoldMarker> GenerateFoldMarkers(Document document/*, string fileName, object parseInformation*/)
        {
            // This is a simple folding strategy - generates folds at each heading.

            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            int lastHeadingLine = -1;
            int lastHeadingOffset = -1;

            for (int i = 0;i < document.LineSegmentCollection.Count;i++)
            {
                LineSegment lseg = document.LineSegmentCollection[i];
                //# This is an H1
                // ....
                //## This is an H2
                // ....
                //###### This is an H6
                // ....

                if (lseg.Words.Count > 0 && lseg.Words[0].Word.Length > 0)
                {
                    if (lseg.Words[0].Word.StartsWith("#"))
                    {
                        if (lastHeadingLine != -1)
                        {
                            // Finish old one. 
                            lastHeadingOffset = document.LineSegmentCollection[lastHeadingLine + 1].Offset;
                            foldMarkers.Add(new FoldMarker(document, lastHeadingOffset, lseg.Offset - lastHeadingOffset - 1, "...", false));
                        }

                        lastHeadingLine = i;
                    }
                }
            }

            if (lastHeadingLine != -1)
            {
                // Finish last one.
                lastHeadingOffset = document.LineSegmentCollection[lastHeadingLine + 1].Offset;
                foldMarkers.Add(new FoldMarker(document, lastHeadingOffset, document.TextLength - lastHeadingOffset - 1, "...", false));
            }

            return foldMarkers;
        }

        public List<FoldMarker> GenerateFoldMarkersRegion(Document document, string fileName, object parseInformation)
        {
            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            Stack<int> startLines = new Stack<int>();

            // Create foldmarkers for the whole document, enumerate through every line.
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


        #endregion Methods
    }
}