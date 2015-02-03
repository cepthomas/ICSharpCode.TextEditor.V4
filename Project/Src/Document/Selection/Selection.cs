// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
    public class Selection
    {
        IDocument _document;
        TextLocation _startPosition;
        TextLocation _endPosition;

        public TextLocation StartPosition
        {
            get { return _startPosition; }
            set { DefaultDocument.ValidatePosition(_document, value); _startPosition = value; }
        }

        public TextLocation EndPosition
        {
            get { return _endPosition; }
            set { DefaultDocument.ValidatePosition(_document, value); _endPosition = value; }
        }

        public int Offset
        {
            get { return _document.PositionToOffset(_startPosition); }
        }

        public int EndOffset
        {
            get { return _document.PositionToOffset(_endPosition); }
        }

        public int Length
        {
            get { return EndOffset - Offset; }
        }

        public bool IsEmpty
        {
            get { return _startPosition == _endPosition; }
        }

        public bool IsValid
        {
            get { return _startPosition.IsValid && _endPosition.IsValid; }
        }

        public bool IsRect { get; set; }

        public string SelectedText
        {
            get
            {
                if (_document != null)
                {
                    if (Length < 0)
                    {
                        return null;
                    }
                    return _document.GetText(Offset, Length);
                }
                return null;
            }
        }

        /// <summary>Creates a new instance of <see cref="Selection"/></summary>
        public Selection(IDocument document)
        {
            _document = document;
            _startPosition = new TextLocation();
            _endPosition = new TextLocation();
            IsRect = false;
        }

        /// <summary>Creates a new instance of <see cref="Selection"/></summary>
        public Selection(IDocument document, TextLocation startPosition, TextLocation endPosition, bool isRect)
        {
            DefaultDocument.ValidatePosition(document, startPosition);
            DefaultDocument.ValidatePosition(document, endPosition);
            Debug.Assert(startPosition <= endPosition);
            _document = document;
            _startPosition = startPosition;
            _endPosition = endPosition;
            IsRect = isRect;
        }

        /// <summary>Converts a <see cref="Selection"/> instance to string (for debug purposes)</summary>
        public override string ToString()
        {
            return String.Format("[DefaultSelection : StartPosition={0}, EndPosition={1}, IsRect={2}]", _startPosition, _endPosition, IsRect);
        }

        public bool ContainsPosition(TextLocation position)
        {
            if (this.IsEmpty)
                return false;

            return _startPosition.Y < position.Y && position.Y  < _endPosition.Y ||
                   _startPosition.Y == position.Y && _startPosition.X <= position.X && (_startPosition.Y != _endPosition.Y || position.X <= _endPosition.X) ||
                   _endPosition.Y == position.Y && _startPosition.Y != _endPosition.Y && position.X <= _endPosition.X;
        }

        public bool ContainsOffset(int offset)
        {
            return _startPosition.IsValid && _endPosition.IsValid && Offset <= offset && offset <= EndOffset;
        }
    }
}
