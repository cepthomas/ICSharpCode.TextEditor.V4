// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>


using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor.Actions
{
    public abstract class AbstractLineFormatAction : AbstractEditAction
    {
        protected TextArea _textArea;
        abstract protected void Convert(Document.Document document, int startLine, int endLine);

        public override void Execute(TextArea textArea)
        {
            if (!textArea.SelectionManager.SelectionIsReadonly)
            {
                _textArea = textArea;
                textArea.BeginUpdate();
                textArea.Document.UndoStack.StartUndoGroup();

                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    Convert(textArea.Document, textArea.SelectionManager.StartPosition.Y, textArea.SelectionManager.EndPosition.Y);
                }
                else
                {
                    Convert(textArea.Document, 0, textArea.Document.TotalNumberOfLines - 1);
                }

                textArea.Document.UndoStack.EndUndoGroup();
                textArea.Caret.ValidateCaretPos();
                textArea.EndUpdate();
                textArea.Refresh();
            }
        }
    }

    public abstract class AbstractSelectionFormatAction : AbstractEditAction
    {
        protected TextArea _textArea;
        abstract protected void Convert(Document.Document document, int offset, int length);

        public override void Execute(TextArea textArea)
        {
            if (!textArea.SelectionManager.SelectionIsReadonly)
            {
                _textArea = textArea;
                textArea.BeginUpdate();

                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    Convert(textArea.Document, textArea.SelectionManager.StartOffset, textArea.SelectionManager.Length);
                }
                else
                {
                    Convert(textArea.Document, 0, textArea.Document.TextLength);
                }

                textArea.Caret.ValidateCaretPos();
                textArea.EndUpdate();
                textArea.Refresh();
            }
        }
    }

    public class RemoveLeadingWS : AbstractLineFormatAction
    {
        protected override void Convert(Document.Document document, int y1, int y2)
        {
            for (int i = y1; i < y2; ++i)
            {
                LineSegment line = document.GetLineSegment(i);
                int removeNumber = 0;

                for (int x = line.Offset; x < line.Offset + line.Length && Char.IsWhiteSpace(document.GetCharAt(x)); ++x)
                {
                    ++removeNumber;
                }

                if (removeNumber > 0)
                {
                    document.Remove(line.Offset, removeNumber);
                }
            }
        }
    }

    public class RemoveTrailingWS : AbstractLineFormatAction
    {
        protected override void Convert(Document.Document document, int y1, int y2)
        {
            for (int i = y2 - 1; i >= y1; --i)
            {
                LineSegment line = document.GetLineSegment(i);
                int removeNumber = 0;

                for (int x = line.Offset + line.Length - 1; x >= line.Offset && Char.IsWhiteSpace(document.GetCharAt(x)); --x)
                {
                    ++removeNumber;
                }

                if (removeNumber > 0)
                {
                    document.Remove(line.Offset + line.Length - removeNumber, removeNumber);
                }
            }
        }
    }

    public class ToUpperCase : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string what = document.GetText(startOffset, length).ToUpper();
            document.Replace(startOffset, length, what);
        }
    }

    public class ToLowerCase : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string what = document.GetText(startOffset, length).ToLower();
            document.Replace(startOffset, length, what);
        }
    }

    public class InvertCaseAction : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            StringBuilder what = new StringBuilder(document.GetText(startOffset, length));

            for (int i = 0; i < what.Length; ++i)
            {
                what[i] = char.IsUpper(what[i]) ? Char.ToLower(what[i]) : Char.ToUpper(what[i]);
            }

            document.Replace(startOffset, length, what.ToString());
        }
    }

    public class CapitalizeAction : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            StringBuilder what = new StringBuilder(document.GetText(startOffset, length));

            for (int i = 0; i < what.Length; ++i)
            {
                if (!char.IsLetter(what[i]) && i < what.Length - 1)
                {
                    what[i + 1] = char.ToUpper(what[i + 1]);
                }
            }
            document.Replace(startOffset, length, what.ToString());
        }

    }

    public class ConvertTabsToSpaces : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string what = document.GetText(startOffset, length);
            string spaces = new string(' ', Shared.TEP.TabIndent);
            document.Replace(startOffset, length, what.Replace("\t", spaces));
        }
    }

    public class ConvertSpacesToTabs : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string what = document.GetText(startOffset, length);
            string spaces = new string(' ', Shared.TEP.TabIndent);
            document.Replace(startOffset, length, what.Replace(spaces, "\t"));
        }
    }

    public class ConvertLeadingTabsToSpaces : AbstractLineFormatAction
    {
        protected override void Convert(Document.Document document, int y1, int y2)
        {
            for (int i = y2; i >= y1; --i)
            {
                LineSegment line = document.GetLineSegment(i);

                if(line.Length > 0)
                {
                    // count how many whitespace characters there are at the start
                    int whiteSpace;

                    for (whiteSpace = 0; whiteSpace < line.Length && char.IsWhiteSpace(document.GetCharAt(line.Offset + whiteSpace)); whiteSpace++)
                    {
                        // deliberately empty
                    }

                    if(whiteSpace > 0)
                    {
                        string newLine = document.GetText(line.Offset,whiteSpace);
                        string newPrefix = newLine.Replace("\t",new string(' ', Shared.TEP.TabIndent));
                        document.Replace(line.Offset,whiteSpace,newPrefix);
                    }
                }
            }
        }
    }

    public class ConvertLeadingSpacesToTabs : AbstractLineFormatAction
    {
        protected override void Convert(Document.Document document, int y1, int y2)
        {
            for (int i = y2; i >= y1; --i)
            {
                LineSegment line = document.GetLineSegment(i);
                if(line.Length > 0)
                {
                    // note: some users may prefer a more radical ConvertLeadingSpacesToTabs that
                    // means there can be no spaces before the first character even if the spaces
                    // didn't add up to a whole number of tabs
                    string newLine = TextUtilities.LeadingWhiteSpaceToTabs(document.GetText(line.Offset,line.Length), Shared.TEP.TabIndent);
                    document.Replace(line.Offset,line.Length,newLine);
                }
            }
        }
    }

    /// <summary>
    /// This is a sample editaction plugin, it indents the selected area.
    /// </summary>
    public class IndentSelection : AbstractLineFormatAction
    {
        protected override void Convert(Document.Document document, int startLine, int endLine)
        {
            document.FormattingStrategy.IndentLines(_textArea, startLine, endLine);
        }
    }

    /// <summary>Pretty it up.</summary>
    public class FixXml : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string sin = document.GetText(startOffset, length);
            string sout;

            try
            {
                XDocument doc = XDocument.Parse(sin);
                sout = doc.ToString();
            }
            catch (Exception)
            {
                sout = sin;
            }

            document.Replace(startOffset, length, sout);
        }
    }

    /// <summary>General purpose sorter, with options.</summary>
    public class SortSelection : AbstractSelectionFormatAction
    {
        public enum SortDirection { Ascending, Descending }

        public SortDirection Direction { get; set; }
        public bool CaseSensitive { get; set; }
        public bool IgnoreWhitespace { get; set; }
        public bool RemoveDuplicates { get; set; }

        int Compare(string x, string y)
        {
            if (x == null || y == null)
            {
                return -1;
            }

            string str1;
            string str2;

            if (Direction == SortDirection.Ascending)
            {
                str1 = x.ToString();
                str2 = y.ToString();
            }
            else
            {
                str1 = y.ToString();
                str2 = x.ToString();
            }

            if (IgnoreWhitespace)
            {
                str1 = str1.Trim();
                str2 = str2.Trim();
            }

            if (!CaseSensitive)
            {
                str1 = str1.ToUpper();
                str2 = str2.ToUpper();
            }

            return str1.CompareTo(str2);
        }

        protected override void Convert(Document.Document document, int startLine, int endLine)
        {
            List<string> lines = new List<string>();
            for (int i = startLine; i <= endLine; ++i)
            {
                LineSegment line = document.GetLineSegment(i);
                lines.Add(document.GetText(line.Offset, line.Length));
            }

            lines.Sort(Compare);

            if (RemoveDuplicates)
            {
                for (int i = 0; i < lines.Count - 1; ++i)
                {
                    if (lines[i].Equals(lines[i + 1]))
                    {
                        lines.RemoveAt(i);
                        --i;
                    }
                }
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                LineSegment line = document.GetLineSegment(startLine + i);
                document.Replace(line.Offset, line.Length, lines[i].ToString());
            }

            // remove removed duplicate lines
            for (int i = startLine + lines.Count; i <= endLine; ++i)
            {
                LineSegment line = document.GetLineSegment(startLine + lines.Count);
                document.Remove(line.Offset, line.TotalLength);
            }
        }
    }

    /// <summary>Convert a (typically) db view or field name such as a_big_dog or A_BIG_DOG into A Big Dog.</summary>
    public class ConvertUnderscoredToReadable : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            StringBuilder sb = new StringBuilder();
            string sin = document.GetText(startOffset, length);
            char lastChar = '_';

            // Fix any errant spaces.
            sin = sin.Replace(' ', '_');

            if (sin != null)
            {
                foreach (char c in sin)
                {
                    if (c == '_')
                        sb.Append(' ');
                    else if (lastChar == '_')
                        sb.Append(char.ToUpper(c));
                    else
                        sb.Append(char.ToLower(c));

                    lastChar = c;
                }
            }

            document.Replace(startOffset, length, sb.ToString());
        }
    }

    /// <summary>Convert a readable name into its db equivalent. Opposite of ConvertUnderscoredToReadable().</summary>
    public class ConvertReadableToUnderscored : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            string sin = document.GetText(startOffset, length);
            document.Replace(startOffset, length, sin.Replace(' ', '_').ToUpper());
        }
    }

    /// <summary>Lower to upper case transitions get a space.</summary>
    public class ConvertCamelcaseToReadable : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startOffset, int length)
        {
            StringBuilder sb = new StringBuilder();
            string sin = document.GetText(startOffset, length);
            char lastChar = sin[0];

            foreach (char c in sin)
            {
                if (char.IsUpper(c) && char.IsLower(lastChar))
                {
                    sb.Append(' ');
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }

                lastChar = c;
            }

            document.Replace(startOffset, length, sb.ToString());
        }
    }

    /// <summary>No comment needed.</summary>
    public class RemoveBlankLines : AbstractSelectionFormatAction
    {
        protected override void Convert(Document.Document document, int startLine, int endLine)
        {
            List<string> lines = new List<string>();
            for (int i = startLine; i <= endLine; ++i)
            {
                LineSegment line = document.GetLineSegment(i);
                lines.Add(document.GetText(line.Offset, line.Length));
            }

            for (int i = 0; i < lines.Count - 1; ++i)
            {
                if (lines[i] == "")
                {
                    lines.RemoveAt(i);
                    --i;
                }
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                LineSegment line = document.GetLineSegment(startLine + i);
                document.Replace(line.Offset, line.Length, lines[i].ToString());
            }

            // remove removed lines
            for (int i = startLine + lines.Count; i <= endLine; ++i)
            {
                LineSegment line = document.GetLineSegment(startLine + lines.Count);
                document.Remove(line.Offset, line.TotalLength);
            }
        }
    }
}
