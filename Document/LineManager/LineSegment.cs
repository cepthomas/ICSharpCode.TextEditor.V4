// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
    public class LineSegment : ISegment
    {
        internal LineSegmentTree.Enumerator _treeEntry;

        Util.WeakCollection<TextAnchor> _anchors;

        public TextWord GetWord(int column)
        {
            int curColumn = 0;
            foreach (TextWord word in Words)
            {
                if (column < curColumn + word.Length)
                {
                    return word;
                }
                curColumn += word.Length;
            }
            return null;
        }

        public bool IsDeleted
        {
            get
            {
                return !_treeEntry.IsValid;
            }
        }

        public int LineNumber
        {
            get
            {
                return _treeEntry.CurrentIndex;
            }
        }

        public int Offset
        {
            get
            {
                return _treeEntry.CurrentOffset;
            }

            set { }
        }

        public int Length
        {
            get
            {
                return TotalLength - DelimiterLength;
            }
            set { }
        }

        public int TotalLength { get; internal set; }

        public int DelimiterLength { get; internal set; }

        // highlighting information
        public List<TextWord> Words { get; set; }

        public HighlightColor GetColorForPosition(int x)
        {
            if (Words != null)
            {
                int xPos = 0;
                foreach (TextWord word in Words)
                {
                    if (x < xPos + word.Length)
                    {
                        return word.SyntaxColor;
                    }
                    xPos += word.Length;
                }
            }
            return new HighlightColor(Color.Black, false, false);
        }

        public SpanStack HighlightSpanStack { get; set; }

        /// <summary>
        /// Converts a <see cref="LineSegment"/> instance to string (for debug purposes)
        /// </summary>
        public override string ToString()
        {
            if (IsDeleted)
                return "[LineSegment: (deleted) Length = " + Length + ", TotalLength = " + TotalLength + ", DelimiterLength = " + DelimiterLength + "]";
            else
                return "[LineSegment: LineNumber=" + LineNumber + ", Offset = "+ Offset +", Length = " + Length + ", TotalLength = " + TotalLength + ", DelimiterLength = " + DelimiterLength + "]";
        }

        #region Anchor management

        public TextAnchor CreateAnchor(int column)
        {
            if (column < 0 || column > Length)
                throw new ArgumentOutOfRangeException("column");
            TextAnchor anchor = new TextAnchor(this, column);
            AddAnchor(anchor);
            return anchor;
        }

        void AddAnchor(TextAnchor anchor)
        {
            //Debug.Assert(anchor.Line == this);

            if (_anchors == null)
                _anchors = new Util.WeakCollection<TextAnchor>();

            _anchors.Add(anchor);
        }

        /// <summary>
        /// Is called when the LineSegment is deleted.
        /// </summary>
        internal void Deleted(ref DeferredEventList deferredEventList)
        {
            //Console.WriteLine("Deleted");
            _treeEntry = LineSegmentTree.Enumerator.Invalid;
            if (_anchors != null)
            {
                foreach (TextAnchor a in _anchors)
                {
                    a.Delete(ref deferredEventList);
                }
                _anchors = null;
            }
        }

        /// <summary>
        /// Is called when a part of the line is removed.
        /// </summary>
        internal void RemovedLinePart(ref DeferredEventList deferredEventList, int startColumn, int length)
        {
            if (length == 0)
                return;
            //Debug.Assert(length > 0);

            //Console.WriteLine("RemovedLinePart " + startColumn + ", " + length);
            if (_anchors != null)
            {
                List<TextAnchor> deletedAnchors = null;
                foreach (TextAnchor a in _anchors)
                {
                    if (a.ColumnNumber > startColumn)
                    {
                        if (a.ColumnNumber >= startColumn + length)
                        {
                            a.ColumnNumber -= length;
                        }
                        else
                        {
                            if (deletedAnchors == null)
                                deletedAnchors = new List<TextAnchor>();
                            a.Delete(ref deferredEventList);
                            deletedAnchors.Add(a);
                        }
                    }
                }
                if (deletedAnchors != null)
                {
                    foreach (TextAnchor a in deletedAnchors)
                    {
                        _anchors.Remove(a);
                    }
                }
            }
        }

        /// <summary>
        /// Is called when a part of the line is inserted.
        /// </summary>
        internal void InsertedLinePart(int startColumn, int length)
        {
            if (length == 0)
                return;
            //Debug.Assert(length > 0);

            //Console.WriteLine("InsertedLinePart " + startColumn + ", " + length);
            if (_anchors != null)
            {
                foreach (TextAnchor a in _anchors)
                {
                    if (a.MovementType == AnchorMovementType.BeforeInsertion
                            ? a.ColumnNumber > startColumn
                            : a.ColumnNumber >= startColumn)
                    {
                        a.ColumnNumber += length;
                    }
                }
            }
        }

        /// <summary>
        /// Is called after another line's content is appended to this line because the newline in between
        /// was deleted.
        /// The DefaultLineManager will call Deleted() on the deletedLine after the MergedWith call.
        ///
        /// firstLineLength: the length of the line before the merge.
        /// </summary>
        internal void MergedWith(LineSegment deletedLine, int firstLineLength)
        {
            //Console.WriteLine("MergedWith");

            if (deletedLine._anchors != null)
            {
                foreach (TextAnchor a in deletedLine._anchors)
                {
                    a.Line = this;
                    AddAnchor(a);
                    a.ColumnNumber += firstLineLength;
                }
                deletedLine._anchors = null;
            }
        }

        /// <summary>
        /// Is called after a newline was inserted into this line, splitting it into this and followingLine.
        /// </summary>
        internal void SplitTo(LineSegment followingLine)
        {
            //Console.WriteLine("SplitTo");

            if (_anchors != null)
            {
                List<TextAnchor> movedAnchors = null;
                foreach (TextAnchor a in _anchors)
                {
                    if (a.MovementType == AnchorMovementType.BeforeInsertion
                            ? a.ColumnNumber > this.Length
                            : a.ColumnNumber >= this.Length)
                    {
                        a.Line = followingLine;
                        followingLine.AddAnchor(a);
                        a.ColumnNumber -= this.Length;

                        if (movedAnchors == null)
                            movedAnchors = new List<TextAnchor>();
                        movedAnchors.Add(a);
                    }
                }
                if (movedAnchors != null)
                {
                    foreach (TextAnchor a in movedAnchors)
                    {
                        _anchors.Remove(a);
                    }
                }
            }
        }
        #endregion
    }
}
