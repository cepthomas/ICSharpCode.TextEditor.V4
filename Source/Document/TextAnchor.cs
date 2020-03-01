// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
    public enum AnchorMovementType
    {
        /// <summary>
        /// Behaves like a start marker - when text is inserted at the anchor position, the anchor will stay
        /// before the inserted text.
        /// </summary>
        BeforeInsertion,
        /// <summary>
        /// Behave like an end marker - when text is insered at the anchor position, the anchor will move
        /// after the inserted text.
        /// </summary>
        AfterInsertion
    }

    /// <summary>
    /// An anchor that can be put into a document and moves around when the document is changed.
    /// </summary>
    public sealed class TextAnchor
    {
        #region Fields
        #endregion

        #region Properties
        public LineSegment Line { get; internal set; }

        public bool IsDeleted { get { return Line == null; } }

        public int LineNumber { get { return Line.LineNumber; } }

        public int ColumnNumber { get; internal set; }

        public TextLocation Location { get { return new TextLocation(ColumnNumber, LineNumber); } }

        public int Offset { get { return Line.Offset + ColumnNumber; } }

        public AnchorMovementType MovementType { get; set; }
        #endregion

        #region Events
        public event EventHandler Deleted;
        #endregion

        #region Lifecycle
        #endregion

        #region Public functions
        #endregion

        #region Private functions
        internal void Delete(ref DeferredEventList deferredEventList)
        {
            // we cannot fire an event here because this method is called while the LineManager adjusts the
            // lineCollection, so an event handler could see inconsistent state
            Line = null;
            deferredEventList.AddDeletedAnchor(this);
        }

        internal void RaiseDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        internal TextAnchor(LineSegment lineSegment, int columnNumber)
        {
            Line = lineSegment;
            ColumnNumber = columnNumber;
        }
        #endregion

        // old:
        //static Exception AnchorDeletedError()
        //{
        //    return new InvalidOperationException("The text containing the anchor was deleted");
        //}
    }
}
