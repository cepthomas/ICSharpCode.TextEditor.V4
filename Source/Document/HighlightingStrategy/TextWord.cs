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
    public enum TextWordType
    {
        Word,
        Space,
        Tab
    }

    /// <summary>
    /// This class represents single words with color information, two special versions of a word are
    /// spaces and tabs.
    /// </summary>
    public class TextWord
    {
        readonly LineSegment _line;
        readonly Document _document;

        public static TextWord Space { get; } = new SpaceTextWord();

        public static TextWord Tab { get; } = new TabTextWord();

        public int Offset { get; protected set; }

        public int Length { get; protected set; }

        public bool HasDefaultColor { get; }

        public virtual TextWordType Type { get { return TextWordType.Word; } }

        public string Word { get { return _document == null ? string.Empty : _document.GetText(_line.Offset + Offset, Length); } }

        public Color Color { get { return SyntaxColor == null ? Color.Black : SyntaxColor.Color; } }

        public bool Bold { get { return SyntaxColor == null ? false : SyntaxColor.Bold; } }

        public bool Italic { get { return SyntaxColor == null ? false : SyntaxColor.Italic; } }

        public HighlightColor SyntaxColor { get; set; }

        public virtual bool IsWhiteSpace { get { return false; } }


        protected TextWord()
        {
        }

        public TextWord(Document document, LineSegment line, int offset, int length, HighlightColor color, bool hasDefaultColor)
        {
            _document = document;
            _line = line;

            SyntaxColor = color;
            Offset = offset;
            Length = length;
            HasDefaultColor = hasDefaultColor;
        }

        /// <summary>
        /// Splits the <paramref name="word"/> into two parts: the part before <paramref name="pos"/> is assigned to
        /// the reference parameter <paramref name="word"/>, the part after <paramref name="pos"/> is returned.
        /// </summary>
        public static TextWord Split(ref TextWord word, int pos)
        {
#if DEBUG_EX
            if (word.Type != TextWordType.Word)
                throw new ArgumentException("word.Type must be Word");
            if (pos <= 0)
                throw new ArgumentOutOfRangeException("pos", pos, "pos must be > 0");
            if (pos >= word.Length)
                throw new ArgumentOutOfRangeException("pos", pos, "pos must be < word.Length");
#endif
            TextWord after = new TextWord(word._document, word._line, word.Offset + pos, word.Length - pos, word.SyntaxColor, word.HasDefaultColor);
            word = new TextWord(word._document, word._line, word.Offset, pos, word.SyntaxColor, word.HasDefaultColor);
            return after;
        }


        public virtual Font GetFont(FontContainer fontContainer)
        {
            return SyntaxColor.GetFont(fontContainer);
        }
    }

    public sealed class SpaceTextWord : TextWord
    {
        public SpaceTextWord()
        {
            Length = 1;
        }

        public SpaceTextWord(HighlightColor color)
        {
            Length = 1;
            SyntaxColor = color;
        }

        public override Font GetFont(FontContainer fontContainer)
        {
            return null;
        }

        public override TextWordType Type
        {
            get
            {
                return TextWordType.Space;
            }
        }

        public override bool IsWhiteSpace
        {
            get
            {
                return true;
            }
        }
    }

    public sealed class TabTextWord : TextWord
    {
        public TabTextWord()
        {
            Length = 1;
        }

        public TabTextWord(HighlightColor color)
        {
            Length = 1;
            SyntaxColor = color;
        }

        public override Font GetFont(FontContainer fontContainer)
        {
            return null;
        }

        public override TextWordType Type { get { return TextWordType.Tab; } }

        public override bool IsWhiteSpace { get { return true; } }
    }
}
