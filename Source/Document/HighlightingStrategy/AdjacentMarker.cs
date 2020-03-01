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
    /// <summary>
    /// Used for mark next token
    /// </summary>
    public class AdjacentMarker
    {
        #region Properties
        /// <value>
        /// String value to indicate to mark next token
        /// </value>
        public string What { get; }

        /// <value>
        /// Color for marking next token
        /// </value>
        public HighlightColor Color { get; }

        /// <value>
        /// If true the indication text will be marked with the same color too
        /// </value>
        public bool MarkMarker { get; } = false;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public AdjacentMarker(XmlElement mark)
        {
            Color = new HighlightColor(mark);
            What  = mark.InnerText;
            if (mark.Attributes["markmarker"] != null)
            {
                MarkMarker = bool.Parse(mark.Attributes["markmarker"].InnerText);
            }
        }
    }

}
