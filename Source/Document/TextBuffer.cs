// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
    public class TextBuffer
    {
#if DEBUG_EX
        int creatorThread = System.Threading.Thread.CurrentThread.ManagedThreadId;

        void CheckThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != creatorThread)
                throw new InvalidOperationException("GapTextBufferStategy is not thread-safe!");
        }
#endif


        #region Fields
        char[] _buffer = new char[0];
        string _cachedContent;

        int _gapBeginOffset = 0;
        int _gapEndOffset = 0;
        int _gapLength = 0; // gapLength == gapEndOffset - gapBeginOffset

        const int MIN_GAP_LEN = 128;
        const int MAX_GAP_LEN = 2048;
        #endregion

        #region Properties
        public int Length { get { return _buffer.Length - _gapLength; } }
        #endregion

        #region Events
        #endregion

        #region Lifecycle
        #endregion

        #region Public functions
        public void SetContent(string text)
        {
            if (text == null)
            {
                text = String.Empty;
            }
            _cachedContent = text;
            _buffer = text.ToCharArray();
            _gapBeginOffset = _gapEndOffset = _gapLength = 0;
        }

        public char GetCharAt(int offset)
        {
#if DEBUG_EX
            CheckThread();
#endif

            if (offset < 0 || offset >= Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset < " + Length.ToString());
            }

            return offset < _gapBeginOffset ? _buffer[offset] : _buffer[offset + _gapLength];
        }

        public string GetText(int offset, int length)
        {
#if DEBUG_EX
            CheckThread();
#endif

            if (offset < 0 || offset > Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length.ToString());
            }
            if (length < 0 || offset + length > Length)
            {
                throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset(" + offset + ")+length <= " + Length.ToString());
            }
            if (offset == 0 && length == Length)
            {
                if (_cachedContent != null)
                    return _cachedContent;
                else
                    return _cachedContent = GetTextInternal(offset, length);
            }
            else
            {
                return GetTextInternal(offset, length);
            }
        }

        public void Insert(int offset, string text)
        {
            Replace(offset, 0, text);
        }

        public void Remove(int offset, int length)
        {
            Replace(offset, length, String.Empty);
        }

        public void Replace(int offset, int length, string text)
        {
            if (text == null)
            {
                text = String.Empty;
            }

#if DEBUG_EX
            CheckThread();
#endif

            if (offset < 0 || offset > Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + Length.ToString());
            }
            if (length < 0 || offset + length > Length)
            {
                throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset+length <= " + Length.ToString());
            }

            _cachedContent = null;

            // Math.Max is used so that if we need to resize the array
            // the new array has enough space for all old chars
            PlaceGap(offset, text.Length - length);
            _gapEndOffset += length; // delete removed text
            text.CopyTo(0, _buffer, _gapBeginOffset, text.Length);
            _gapBeginOffset += text.Length;
            _gapLength = _gapEndOffset - _gapBeginOffset;
            if (_gapLength > MAX_GAP_LEN)
            {
                MakeNewBuffer(_gapBeginOffset, MIN_GAP_LEN);
            }
        }

        #endregion

        #region Private functions
        string GetTextInternal(int offset, int length)
        {
            int end = offset + length;

            if (end < _gapBeginOffset)
            {
                return new string(_buffer, offset, length);
            }

            if (offset > _gapBeginOffset)
            {
                return new string(_buffer, offset + _gapLength, length);
            }

            int block1Size = _gapBeginOffset - offset;
            int block2Size = end - _gapBeginOffset;

            StringBuilder buf = new StringBuilder(block1Size + block2Size);
            buf.Append(_buffer, offset, block1Size);
            buf.Append(_buffer, _gapEndOffset, block2Size);
            return buf.ToString();
        }

        void PlaceGap(int newGapOffset, int minRequiredGapLength)
        {
            if (_gapLength < minRequiredGapLength)
            {
                // enlarge gap
                MakeNewBuffer(newGapOffset, minRequiredGapLength);
            }
            else
            {
                while (newGapOffset < _gapBeginOffset)
                {
                    _buffer[--_gapEndOffset] = _buffer[--_gapBeginOffset];
                }
                while (newGapOffset > _gapBeginOffset)
                {
                    _buffer[_gapBeginOffset++] = _buffer[_gapEndOffset++];
                }
            }
        }

        void MakeNewBuffer(int newGapOffset, int newGapLength)
        {
            if (newGapLength < MIN_GAP_LEN) newGapLength = MIN_GAP_LEN;

            char[] newBuffer = new char[Length + newGapLength];
            if (newGapOffset < _gapBeginOffset)
            {
                // gap is moving backwards

                // first part:
                Array.Copy(_buffer, 0, newBuffer, 0, newGapOffset);
                // moving middle part:
                Array.Copy(_buffer, newGapOffset, newBuffer, newGapOffset + newGapLength, _gapBeginOffset - newGapOffset);
                // last part:
                Array.Copy(_buffer, _gapEndOffset, newBuffer, newBuffer.Length - (_buffer.Length - _gapEndOffset), _buffer.Length - _gapEndOffset);
            }
            else
            {
                // gap is moving forwards
                // first part:
                Array.Copy(_buffer, 0, newBuffer, 0, _gapBeginOffset);
                // moving middle part:
                Array.Copy(_buffer, _gapEndOffset, newBuffer, _gapBeginOffset, newGapOffset - _gapBeginOffset);
                // last part:
                int lastPartLength = newBuffer.Length - (newGapOffset + newGapLength);
                Array.Copy(_buffer, _buffer.Length - lastPartLength, newBuffer, newGapOffset + newGapLength, lastPartLength);
            }

            _gapBeginOffset = newGapOffset;
            _gapEndOffset = newGapOffset + newGapLength;
            _gapLength = newGapLength;
            _buffer = newBuffer;
        }
        #endregion
    }
}
