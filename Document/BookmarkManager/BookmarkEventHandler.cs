// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
    public delegate void BookmarkEventHandler(object sender, BookmarkEventArgs e);

    /// <summary>
    /// Description of BookmarkEventHandler.
    /// </summary>
    public class BookmarkEventArgs : EventArgs
    {
        public Bookmark Bookmark { get; }

        public BookmarkEventArgs(Bookmark bookmark)
        {
            Bookmark = bookmark;
        }
    }
}
