// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using SWF = System.Windows.Forms;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// Description of Bookmark.
    /// </summary>
    public class Bookmark
    {
        Document _document;
        TextLocation _location;
        bool _isEnabled = true;

        public Document Document
        {
            get
            {
                return _document;
            }
            set
            {
                if (_document != value)
                {
                    if (Anchor != null)
                    {
                        _location = Anchor.Location;
                        Anchor = null;
                    }
                    _document = value;
                    CreateAnchor();
                    OnDocumentChanged(EventArgs.Empty);
                }
            }
        }

        void CreateAnchor()
        {
            if (_document != null)
            {
                LineSegment line = _document.GetLineSegment(Math.Max(0, Math.Min(_location.Line, _document.TotalNumberOfLines-1)));
                Anchor = line.CreateAnchor(Math.Max(0, Math.Min(_location.Column, line.Length)));
                // after insertion: keep bookmarks after the initial whitespace (see DefaultFormattingStrategy.SmartReplaceLine)
                Anchor.MovementType = AnchorMovementType.AfterInsertion;
                Anchor.Deleted += AnchorDeleted;
            }
        }

        void AnchorDeleted(object sender, EventArgs e)
        {
            _document.BookmarkManager.RemoveMark(this);
        }

        /// <summary>
        /// Gets the TextAnchor used for this bookmark.
        /// Is null if the bookmark is not connected to a document.
        /// </summary>
        public TextAnchor Anchor { get; private set; }

        public TextLocation Location
        {
            get
            {
                return Anchor != null ? Anchor.Location : _location;
            }
            set
            {
                _location = value;
                CreateAnchor();
            }
        }

        public event EventHandler DocumentChanged;

        protected virtual void OnDocumentChanged(EventArgs e)
        {
            DocumentChanged?.Invoke(this, e);
        }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_document != null)
                    {
                        _document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, LineNumber));
                        _document.CommitUpdate();
                    }
                    OnIsEnabledChanged(EventArgs.Empty);
                }
            }
        }

        public event EventHandler IsEnabledChanged;

        protected virtual void OnIsEnabledChanged(EventArgs e)
        {
            IsEnabledChanged?.Invoke(this, e);
        }

        public int LineNumber
        {
            get
            {
                return Anchor != null ? Anchor.LineNumber : _location.Line;
            }
        }

        public int ColumnNumber
        {
            get
            {
                return Anchor != null ? Anchor.ColumnNumber : _location.Column;
            }
        }

        /// <summary>
        /// Gets if the bookmark can be toggled off using the 'set/unset bookmark' command.
        /// </summary>
        public virtual bool CanToggle
        {
            get
            {
                return true;
            }
        }

        public Bookmark(Document document, TextLocation location) : this(document, location, true)
        {
        }

        public Bookmark(Document document, TextLocation location, bool isEnabled)
        {
            _document = document;
            _isEnabled = isEnabled;
            Location = location;
        }

        public virtual bool Click(SWF.Control parent, SWF.MouseEventArgs e)
        {
            if (e.Button == SWF.MouseButtons.Left && CanToggle)
            {
                _document.BookmarkManager.RemoveMark(this);
                return true;
            }
            return false;
        }

        public virtual void Draw(IconBarMargin margin, Graphics g, Point p)
        {
            margin.DrawBookmark(g, p.Y, _isEnabled);
        }
    }
}
