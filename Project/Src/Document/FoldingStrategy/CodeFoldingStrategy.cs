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

// From https://code.google.com/p/codingeditor

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>Description of CodeFoldingStrategy. Generic bracket folding.</summary>
    public class CodeFoldingStrategy : IFoldingStrategy
    {
        #region Methods

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

        #endregion Methods
    }
}