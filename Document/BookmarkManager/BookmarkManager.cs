// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// This class handles the bookmarks for a buffer
    /// </summary>
    public class BookmarkManager //TODO1 another disaster.
    {
#if DEBUG_EX
        IList<Bookmark> bookmark = new CheckedList<Bookmark>();
#else
        List<Bookmark> _bookmark = new List<Bookmark>();
#endif

        public event BookmarkEventHandler Removed;
        public event BookmarkEventHandler Added;

        /// <value>
        /// Contains all bookmarks
        /// </value>
        public ReadOnlyCollection<Bookmark> Marks { get { return new ReadOnlyCollection<Bookmark>(_bookmark); } }

        public Document Document { get; }

        /// <summary>
        /// Creates a new instance of <see cref="BookmarkManager"/>
        /// </summary>
        internal BookmarkManager(Document document, LineManager lineTracker)
        {
            Document = document;
        }

        /// <summary>
        /// Sets the mark at the line <code>location.Line</code> if it is not set, if the
        /// line is already marked the mark is cleared.
        /// </summary>
        public void ToggleMarkAt(TextLocation location)
        {
            Bookmark newMark = new Bookmark(Document, location);

            Type newMarkType = newMark.GetType();

            for (int i = 0; i < _bookmark.Count; ++i)
            {
                Bookmark mark = _bookmark[i];

                if (mark.LineNumber == location.Line && mark.CanToggle && mark.GetType() == newMarkType)
                {
                    _bookmark.RemoveAt(i);
                    OnRemoved(new BookmarkEventArgs(mark));
                    return;
                }
            }

            _bookmark.Add(newMark);
            OnAdded(new BookmarkEventArgs(newMark));
        }

        public void AddMark(Bookmark mark)
        {
            _bookmark.Add(mark);
            OnAdded(new BookmarkEventArgs(mark));
        }

        public void RemoveMark(Bookmark mark)
        {
            _bookmark.Remove(mark);
            OnRemoved(new BookmarkEventArgs(mark));
        }

        public void RemoveMarks(Predicate<Bookmark> predicate)
        {
            for (int i = 0; i < _bookmark.Count; ++i)
            {
                Bookmark bm = _bookmark[i];
                if (predicate(bm))
                {
                    _bookmark.RemoveAt(i--);
                    OnRemoved(new BookmarkEventArgs(bm));
                }
            }
        }

        /// <returns>
        /// true, if a mark at mark exists, otherwise false
        /// </returns>
        public bool IsMarked(int lineNr)
        {
            for (int i = 0; i < _bookmark.Count; ++i)
            {
                if (_bookmark[i].LineNumber == lineNr)
                {
                    return true;
                }
            }
            return false;
        }

        /// <remarks>
        /// Clears all bookmark
        /// </remarks>
        public void Clear()
        {
            foreach (Bookmark mark in _bookmark)
            {
                OnRemoved(new BookmarkEventArgs(mark));
            }
            _bookmark.Clear();
        }

        /// <value>
        /// The lowest mark, if no marks exists it returns -1
        /// </value>
        public Bookmark GetFirstMark(Predicate<Bookmark> predicate)
        {
            if (_bookmark.Count < 1)
            {
                return null;
            }
            Bookmark first = null;
            for (int i = 0; i < _bookmark.Count; ++i)
            {
                if (predicate(_bookmark[i]) && _bookmark[i].IsEnabled && (first == null || _bookmark[i].LineNumber < first.LineNumber))
                {
                    first = _bookmark[i];
                }
            }
            return first;
        }

        /// <value>
        /// The highest mark, if no marks exists it returns -1
        /// </value>
        public Bookmark GetLastMark(Predicate<Bookmark> predicate)
        {
            if (_bookmark.Count < 1)
            {
                return null;
            }
            Bookmark last = null;
            for (int i = 0; i < _bookmark.Count; ++i)
            {
                if (predicate(_bookmark[i]) && _bookmark[i].IsEnabled && (last == null || _bookmark[i].LineNumber > last.LineNumber))
                {
                    last = _bookmark[i];
                }
            }
            return last;
        }
        bool AcceptAnyMarkPredicate(Bookmark mark)
        {
            return true;
        }
        public Bookmark GetNextMark(int curLineNr)
        {
            return GetNextMark(curLineNr, AcceptAnyMarkPredicate);
        }

        /// <remarks>
        /// returns first mark higher than <code>lineNr</code>
        /// </remarks>
        /// <returns>
        /// returns the next mark > cur, if it not exists it returns FirstMark()
        /// </returns>
        public Bookmark GetNextMark(int curLineNr, Predicate<Bookmark> predicate)
        {
            if (_bookmark.Count == 0)
            {
                return null;
            }

            Bookmark next = GetFirstMark(predicate);
            foreach (Bookmark mark in _bookmark)
            {
                if (predicate(mark) && mark.IsEnabled && mark.LineNumber > curLineNr)
                {
                    if (mark.LineNumber < next.LineNumber || next.LineNumber <= curLineNr)
                    {
                        next = mark;
                    }
                }
            }
            return next;
        }

        public Bookmark GetPrevMark(int curLineNr)
        {
            return GetPrevMark(curLineNr, AcceptAnyMarkPredicate);
        }
        /// <remarks>
        /// returns first mark lower than <code>lineNr</code>
        /// </remarks>
        /// <returns>
        /// returns the next mark lower than cur, if it not exists it returns LastMark()
        /// </returns>
        public Bookmark GetPrevMark(int curLineNr, Predicate<Bookmark> predicate)
        {
            if (_bookmark.Count == 0)
            {
                return null;
            }

            Bookmark prev = GetLastMark(predicate);

            foreach (Bookmark mark in _bookmark)
            {
                if (predicate(mark) && mark.IsEnabled && mark.LineNumber < curLineNr)
                {
                    if (mark.LineNumber > prev.LineNumber || prev.LineNumber >= curLineNr)
                    {
                        prev = mark;
                    }
                }
            }
            return prev;
        }

        protected virtual void OnRemoved(BookmarkEventArgs e)
        {
            Removed?.Invoke(this, e);
        }

        protected virtual void OnAdded(BookmarkEventArgs e)
        {
            Added?.Invoke(this, e);
        }
    }
}
