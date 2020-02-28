#region Header

/*
 * Created by SharpDevelop.
 * User: Jose
 * Date: 25/06/2008
 * Time: 19.25
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

#endregion Header

using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>Description of CodeFoldingStrategy. Generic bracket folding.</summary>
    public class CodeFoldingStrategy : IFoldingStrategy
    {
        #region Methods

        /// Interface implementation.
        public List<FoldMarker> GenerateFoldMarkers(Document document, string fileName, object parseInformation)
        {
            List<FoldMarker> foldMarkers = new List<FoldMarker>();

            Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;
            char openingBrace = '{';
            char closingBrace = '}';

            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == openingBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == closingBrace && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // don't fold if opening and closing brace are on the same line
                    if (startOffset < lastNewLineOffset)
                    {
                        foldMarkers.Add(new FoldMarker(document, startOffset, i + 1 - startOffset, "{...}", false));
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }
            }

            return foldMarkers;
        }

        #endregion Methods
    }
}