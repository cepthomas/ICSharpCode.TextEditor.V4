// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    /// <summary>
    /// A class that is able to draw a line on any control (outside the text editor)
    /// </summary>
    public class DrawableLine
    {
        static readonly StringFormat _sf = (StringFormat)System.Drawing.StringFormat.GenericTypographic.Clone();
        readonly List<SimpleTextWord> _words = new List<SimpleTextWord>();
        SizeF _spaceSize;
        readonly Font _monospacedFont;
        readonly Font _boldMonospacedFont;

        private class SimpleTextWord
        {
            internal TextWordType Type;
            internal string       Word;
            internal bool         Bold;
            internal Color        Color;

            public SimpleTextWord(TextWordType Type, string Word, bool Bold, Color Color)
            {
                this.Type = Type;
                this.Word = Word;
                this.Bold = Bold;
                this.Color = Color;
            }

            internal readonly static SimpleTextWord Space = new SimpleTextWord(TextWordType.Space, " ", false, Color.Black);
            internal readonly static SimpleTextWord Tab = new SimpleTextWord(TextWordType.Tab, "\t", false, Color.Black);
        }

        public DrawableLine(Document.Document document, LineSegment line, Font monospacedFont, Font boldMonospacedFont)
        {
            _monospacedFont = monospacedFont;
            _boldMonospacedFont = boldMonospacedFont;

            if (line.Words != null)
            {
                foreach (TextWord word in line.Words)
                {
                    if (word.Type == TextWordType.Space)
                    {
                        _words.Add(SimpleTextWord.Space);
                    }
                    else if (word.Type == TextWordType.Tab)
                    {
                        _words.Add(SimpleTextWord.Tab);
                    }
                    else
                    {
                        _words.Add(new SimpleTextWord(TextWordType.Word, word.Word, word.Bold, word.Color));
                    }
                }
            }
            else
            {
                _words.Add(new SimpleTextWord(TextWordType.Word, document.GetText(line), false, Color.Black));
            }
        }

#if UNUSED
        public int LineLength
        {
            get
            {
                int length = 0;
                foreach (SimpleTextWord word in _words)
                {
                    length += word.Word.Length;
                }
                return length;
            }
        }

        public void SetBold(int startIndex, int endIndex, bool bold)
        {
            if (startIndex < 0)
                throw new ArgumentException("startIndex must be >= 0");

            if (startIndex > endIndex)
                throw new ArgumentException("startIndex must be <= endIndex");

            if (startIndex == endIndex) return;
            int pos = 0;

            for (int i = 0; i < _words.Count; i++)
            {
                SimpleTextWord word = _words[i];
                if (pos >= endIndex)
                    break;
                int wordEnd = pos + word.Word.Length;
                // 3 possibilities:
                if (startIndex <= pos && endIndex >= wordEnd)
                {
                    // word is fully in region:
                    word.Bold = bold;
                }
                else if (startIndex <= pos)
                {
                    // beginning of word is in region
                    int inRegionLength = endIndex - pos;
                    SimpleTextWord newWord = new SimpleTextWord(word.Type, word.Word.Substring(inRegionLength), word.Bold, word.Color);
                    _words.Insert(i + 1, newWord);

                    word.Bold = bold;
                    word.Word = word.Word.Substring(0, inRegionLength);
                }
                else if (startIndex < wordEnd)
                {
                    // end of word is in region (or middle of word is in region)
                    int notInRegionLength = startIndex - pos;

                    SimpleTextWord newWord = new SimpleTextWord(word.Type, word.Word.Substring(notInRegionLength), word.Bold, word.Color);
                    // newWord.Bold will be set in the next iteration
                    _words.Insert(i + 1, newWord);

                    word.Word = word.Word.Substring(0, notInRegionLength);
                }
                pos = wordEnd;
            }
        }
#endif

        public static float DrawDocumentWord(Graphics g, string word, PointF position, Font font, Color foreColor)
        {
            if (word == null || word.Length == 0)
            {
                return 0f;
            }

            SizeF wordSize = g.MeasureString(word, font, 32768, _sf);

            g.DrawString(word, font, BrushRegistry.GetBrush(foreColor), position, _sf);

            return wordSize.Width;
        }

        public SizeF GetSpaceSize(Graphics g)
        {
            if (_spaceSize.IsEmpty)
            {
                _spaceSize = g.MeasureString("-", _boldMonospacedFont,  new PointF(0, 0), _sf);
            }
            return _spaceSize;
        }

        public void DrawLine(Graphics g, ref float xPos, float xOffset, float yPos, Color c)
        {
            SizeF spaceSize = GetSpaceSize(g);
            foreach (SimpleTextWord word in _words)
            {
                switch (word.Type)
                {
                case TextWordType.Space:
                    xPos += spaceSize.Width;
                    break;

                case TextWordType.Tab:
                    float tabWidth = spaceSize.Width * 4;
                    xPos += tabWidth;
                    xPos = (int)((xPos + 2) / tabWidth) * tabWidth;
                    break;

                case TextWordType.Word:
                    xPos += DrawDocumentWord(g, word.Word, new PointF(xPos + xOffset, yPos), 
                        word.Bold ? _boldMonospacedFont : _monospacedFont,
                        c == Color.Empty ? word.Color : c);
                    break;
                }
            }
        }

#if UNUSED
        public void DrawLine(Graphics g, ref float xPos, float xOffset, float yPos)
        {
            DrawLine(g, ref xPos, xOffset, yPos, Color.Empty);
        }

        public float MeasureWidth(Graphics g, float xPos)
        {
            SizeF spaceSize = GetSpaceSize(g);
            foreach (SimpleTextWord word in _words)
            {
                switch (word.Type)
                {
                case TextWordType.Space:
                    xPos += spaceSize.Width;
                    break;
                case TextWordType.Tab:
                    float tabWidth = spaceSize.Width * 4;
                    xPos += tabWidth;
                    xPos = (int)((xPos + 2) / tabWidth) * tabWidth;
                    break;
                case TextWordType.Word:
                    if (word.Word != null && word.Word.Length > 0)
                    {
                        xPos += g.MeasureString(word.Word, word.Bold ? _boldMonospacedFont : _monospacedFont, 32768, _sf).Width;
                    }
                    break;
                }
            }
            return xPos;
        }

#endif
    }
}
