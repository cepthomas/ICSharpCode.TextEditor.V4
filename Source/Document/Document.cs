// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
//using ICSharpCode.TextEditor.Src.Util;
using ICSharpCode.TextEditor.Undo;

namespace ICSharpCode.TextEditor.Document
{
    #region Enums
    /// <summary>
    /// Describes the caret marker
    /// </summary>
    public enum LineViewerStyle
    {
        /// <summary>
        /// No line viewer will be displayed
        /// </summary>
        None,

        /// <summary>
        /// The row in which the caret is will be marked
        /// </summary>
        FullRow
    }

    /// <summary>
    /// Describes the indent style
    /// </summary>
    public enum IndentStyle
    {
        /// <summary>
        /// No indentation occurs
        /// </summary>
        None,

        /// <summary>
        /// The indentation from the line above will be
        /// taken to indent the curent line
        /// </summary>
        Auto,

        /// <summary>
        /// Inteligent, context sensitive indentation will occur
        /// </summary>
        Smart
    }

    /// <summary>
    /// Describes the bracket highlighting style
    /// </summary>
    public enum BracketHighlightingStyle
    {
        /// <summary>
        /// Brackets won't be highlighted
        /// </summary>
        None,

        /// <summary>
        /// Brackets will be highlighted if the caret is on the bracket
        /// </summary>
        OnBracket,

        /// <summary>
        /// Brackets will be highlighted if the caret is after the bracket
        /// </summary>
        AfterBracket
    }

    /// <summary>
    /// Describes the selection mode of the text area
    /// </summary>
    public enum DocumentSelectionMode
    {
        /// <summary>
        /// The 'normal' selection mode.
        /// </summary>
        Normal,

        /// <summary>
        /// Selections will be added to the current selection or new
        /// ones will be created (multi-select mode)
        /// </summary>
        Additive
    }
    #endregion

    /// <summary>
    /// This delegate is used for document events.
    /// </summary>
    public delegate void DocumentEventHandler(object sender, DocumentEventArgs e);

    // TODO2 update events
    //public event EventHandler<DocumentEventArgs> DocumentEventHandler;

    public class DocumentEventArgs : EventArgs
    {
        /// <returns>always a valid Document which is related to the Event.</returns>
        public Document Document { get; set; } = null;

        /// <returns>-1 if no offset was specified for this event</returns>
        public int Offset { get; set; } = 0;

        /// <returns>null if no text was specified for this event</returns>
        public string Text { get; set; } = null;

        /// <returns>-1 if no length was specified for this event</returns>
        public int Length { get; set; } = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    public class Document
    {
        #region Fields
        #endregion

        #region Properties
        public LineManager LineManager { get; set; }

        public MarkerStrategy MarkerStrategy { get; set; }

        //public TextEditorProperties TextEditorProperties { get; set; } = new TextEditorProperties();

        public UndoStack UndoStack { get; } = new UndoStack();

        public IList<LineSegment> LineSegmentCollection
        {
            get { return LineManager.LineSegmentCollection; }
        }

        public bool ReadOnly { get; set; } = false;

        public TextBuffer TextBuffer { get; set; }

        public IFormattingStrategy FormattingStrategy { get; set; }

        public FoldingManager FoldingManager { get; set; }

        public HighlightingStrategy HighlightingStrategy
        {
            get { return LineManager.HighlightingStrategy; }
            set { LineManager.HighlightingStrategy = value; }
        }

        public int TextLength
        {
            get { return TextBuffer.Length; }
        }

        public BookmarkManager BookmarkManager { get; set; }

        public int TotalNumberOfLines
        {
            get { return LineManager.TotalNumberOfLines; }
        }

        public List<TextAreaUpdate> UpdateQueue { get; } = new List<TextAreaUpdate>();

        public string TextContent
        {
            get
            {
                return GetText(0, TextBuffer.Length);
            }
            set
            {
                OnDocumentAboutToBeChanged(new DocumentEventArgs() { Document = this, Text = value } );
                TextBuffer.SetContent(value);
                LineManager.SetContent(value); // TODO1*** 6 seconds
                UndoStack.ClearAll();
                OnDocumentChanged(new DocumentEventArgs() { Document = this, Text = value } );
                OnTextContentChanged(EventArgs.Empty);

                FoldingManager.UpdateFoldings(); // TODO1 every edit needs to update foldings...
            }
        }
        #endregion

        #region Events
        public event EventHandler<LineLengthChangeEventArgs> LineLengthChanged
        {
            add { LineManager.LineLengthChanged += value; }
            remove { LineManager.LineLengthChanged -= value; }
        }

        public event EventHandler<LineCountChangeEventArgs> LineCountChanged
        {
            add { LineManager.LineCountChanged += value; }
            remove { LineManager.LineCountChanged -= value; }
        }

        public event EventHandler<LineEventArgs> LineDeleted
        {
            add { LineManager.LineDeleted += value; }
            remove { LineManager.LineDeleted -= value; }
        }

        public event EventHandler UpdateCommited;
        public event EventHandler TextContentChanged;

        public event DocumentEventHandler DocumentAboutToBeChanged;
        public event DocumentEventHandler DocumentChanged;

        #endregion

        #region Lifecycle
        public Document()
        {
            TextBuffer = new TextBuffer();
            FormattingStrategy = new DefaultFormattingStrategy();
            LineManager = new LineManager(this, null);
            FoldingManager = new FoldingManager(this, LineManager);
            MarkerStrategy = new MarkerStrategy(this);
            BookmarkManager = new BookmarkManager(this, LineManager);
        }
        #endregion

        #region Public functions
        public void Insert(int offset, string text)
        {
            if (!ReadOnly)
            {
                OnDocumentAboutToBeChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = -1, Text = text });

                TextBuffer.Insert(offset, text);
                LineManager.Insert(offset, text);

                UndoStack.Push(new UndoableInsert(this, offset, text));

                OnDocumentChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = -1, Text = text });
            }
        }

        public void Remove(int offset, int length)
        {
            if (!ReadOnly)
            {
                OnDocumentAboutToBeChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = length });
                UndoStack.Push(new UndoableDelete(this, offset, GetText(offset, length)));

                TextBuffer.Remove(offset, length);
                LineManager.Remove(offset, length);

                OnDocumentChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = length });
            }
        }

        public void Replace(int offset, int length, string text)
        {
            if (!ReadOnly)
            {
                OnDocumentAboutToBeChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = length, Text = text });
                UndoStack.Push(new UndoableReplace(this, offset, GetText(offset, length), text));

                TextBuffer.Replace(offset, length, text);
                LineManager.Replace(offset, length, text);

                OnDocumentChanged(new DocumentEventArgs() { Document = this, Offset = offset, Length = length, Text = text });
            }
        }

        public char GetCharAt(int offset)
        {
            return TextBuffer.GetCharAt(offset);
        }

        public string GetText(int offset, int length)
        {
#if DEBUG_EX
            if (length < 0) throw new ArgumentOutOfRangeException("length", length, "length < 0");
#endif
            return TextBuffer.GetText(offset, length);
        }

        public string GetText(LineSegment segment)
        {
            return GetText(segment.Offset, segment.Length);
        }

        public int GetLineNumberForOffset(int offset)
        {
            return LineManager.GetLineNumberForOffset(offset);
        }

        public LineSegment GetLineSegmentForOffset(int offset)
        {
            return LineManager.GetLineSegmentForOffset(offset);
        }

        public LineSegment GetLineSegment(int line)
        {
            return LineManager.GetLineSegment(line);
        }

        public int GetFirstLogicalLine(int lineNumber)
        {
            return LineManager.GetFirstLogicalLine(lineNumber);
        }

        public int GetLastLogicalLine(int lineNumber)
        {
            return LineManager.GetLastLogicalLine(lineNumber);
        }

        public int GetVisibleLine(int lineNumber)
        {
            return LineManager.GetVisibleLine(lineNumber);
        }

//		public int GetVisibleColumn(int logicalLine, int logicalColumn)
//		{
//			return lineTrackingStrategy.GetVisibleColumn(logicalLine, logicalColumn);
//		}
//
        public int GetNextVisibleLineAbove(int lineNumber, int lineCount)
        {
            return LineManager.GetNextVisibleLineAbove(lineNumber, lineCount);
        }

        public int GetNextVisibleLineBelow(int lineNumber, int lineCount)
        {
            return LineManager.GetNextVisibleLineBelow(lineNumber, lineCount);
        }

        public TextLocation OffsetToPosition(int offset)
        {
            int lineNr = GetLineNumberForOffset(offset);
            LineSegment line = GetLineSegment(lineNr);
            return new TextLocation(offset - line.Offset, lineNr);
        }

        public int PositionToOffset(TextLocation p)
        {
            if (p.Y >= TotalNumberOfLines)
            {
                return 0;
            }
            LineSegment line = GetLineSegment(p.Y);
            return Math.Min(TextLength, line.Offset + Math.Min(line.Length, p.X));
        }

        public void UpdateSegmentListOnDocumentChange<T>(List<T> list, DocumentEventArgs e) where T : Segment
        {
            int removedCharacters = e.Length > 0 ? e.Length : 0;
            int insertedCharacters = e.Text != null ? e.Text.Length : 0;
            for (int i = 0; i < list.Count; ++i)
            {
                Segment s = list[i];
                int segmentStart = s.Offset;
                int segmentEnd = s.Offset + s.Length;

                if (e.Offset <= segmentStart)
                {
                    segmentStart -= removedCharacters;
                    if (segmentStart < e.Offset)
                        segmentStart = e.Offset;
                }
                if (e.Offset < segmentEnd)
                {
                    segmentEnd -= removedCharacters;
                    if (segmentEnd < e.Offset)
                        segmentEnd = e.Offset;
                }

                //Debug.Assert(segmentStart <= segmentEnd);

                if (segmentStart == segmentEnd)
                {
                    list.RemoveAt(i);
                    --i;
                    continue;
                }

                if (e.Offset <= segmentStart)
                    segmentStart += insertedCharacters;
                if (e.Offset < segmentEnd)
                    segmentEnd += insertedCharacters;

                //Debug.Assert(segmentStart < segmentEnd);

                s.Offset = segmentStart;
                s.Length = segmentEnd - segmentStart;
            }
        }

        public void RequestUpdate(TextAreaUpdate update)
        {
            if (UpdateQueue.Count == 1 && UpdateQueue[0].TextAreaUpdateType == TextAreaUpdateType.WholeTextArea)
            {
                // if we're going to update the whole text area, we don't need to store detail updates
                return;
            }

            if (update.TextAreaUpdateType == TextAreaUpdateType.WholeTextArea)
            {
                // if we're going to update the whole text area, we don't need to store detail updates
                UpdateQueue.Clear();
            }

            UpdateQueue.Add(update);
        }

        public void CommitUpdate()
        {
            UpdateCommited?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Private functions
        void OnTextContentChanged(EventArgs e)
        {
            TextContentChanged?.Invoke(this, e);
        }

        void OnDocumentAboutToBeChanged(DocumentEventArgs e)
        {
            DocumentAboutToBeChanged?.Invoke(this, e);
        }

        void OnDocumentChanged(DocumentEventArgs e)
        {
            DocumentChanged?.Invoke(this, e);
        }
        #endregion
    }
}
