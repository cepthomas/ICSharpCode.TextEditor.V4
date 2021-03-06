﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions
{
    public class ToggleBookmark : EditAction
    {
        public override void Execute(TextArea textArea)
        {
            textArea.Document.BookmarkManager.ToggleMarkAt(textArea.Caret.Position);
            textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, textArea.Caret.Line));
            textArea.Document.CommitUpdate();
        }
    }

    public class GoToPreviousBookmark : EditAction
    {
        Predicate<Bookmark> _predicate = null;

        public GoToPreviousBookmark() { }

        public GoToPreviousBookmark(Predicate<Bookmark> predicate)
        {
            _predicate = predicate;
        }

        public override void Execute(TextArea textArea)
        {
            Bookmark mark = textArea.Document.BookmarkManager.GetPrevMark(textArea.Caret.Line, _predicate);
            if (mark != null)
            {
                textArea.Caret.Position = mark.Location;
                textArea.SelectionManager.ClearSelection();
                textArea.SetDesiredColumn();
            }
        }
    }

    public class GoToNextBookmark : EditAction
    {
        Predicate<Bookmark> _predicate = null;

        public GoToNextBookmark(Predicate<Bookmark> predicate)
        {
            _predicate = predicate;
        }

        public override void Execute(TextArea textArea)
        {
            Bookmark mark = textArea.Document.BookmarkManager.GetNextMark(textArea.Caret.Line, _predicate);
            if (mark != null)
            {
                textArea.Caret.Position = mark.Location;
                textArea.SelectionManager.ClearSelection();
                textArea.SetDesiredColumn();
            }
        }
    }

    public class ClearAllBookmarks : EditAction
    {
        Predicate<Bookmark> _predicate = null;

        public ClearAllBookmarks(Predicate<Bookmark> predicate)
        {
            _predicate = predicate;
        }

        public override void Execute(TextArea textArea)
        {
            textArea.Document.BookmarkManager.RemoveMarks(_predicate);
            textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
            textArea.Document.CommitUpdate();
        }
    }
}
