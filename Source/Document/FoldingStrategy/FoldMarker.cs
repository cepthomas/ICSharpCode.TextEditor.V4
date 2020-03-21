// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
    public enum FoldType
    {
        Unspecified,
        MemberBody,
        Region,
        TypeBody
    }

    public class FoldMarker : Segment, IComparable //, AbstractSegment
    {
        readonly Document document = null;

        int startLine = -1;
        int startColumn;
        int endLine = -1;
        int endColumn;

        //public virtual int offset { get; set; } = -1;
        int offset = -1;

        //public virtual int length { get; set; } = -1;
        int length = -1;

        static void GetPointForOffset(Document document, int offset, out int line, out int column)
        {
            if (offset > document.TextLength)
            {
                line = document.TotalNumberOfLines + 1;
                column = 1;
            }
            else if (offset < 0)
            {
                line = -1;
                column = -1;
            }
            else
            {
                line = document.GetLineNumberForOffset(offset);
                column = offset - document.GetLineSegment(line).Offset;
            }
        }

        public FoldType FoldType { get; set; } = FoldType.Unspecified;

        public int StartLine
        {
            get
            {
                if (startLine < 0)
                {
                    GetPointForOffset(document, offset, out startLine, out startColumn);
                }
                return startLine;
            }
        }

        public int StartColumn
        {
            get
            {
                if (startLine < 0)
                {
                    GetPointForOffset(document, offset, out startLine, out startColumn);
                }
                return startColumn;
            }
        }

        public int EndLine
        {
            get
            {
                if (endLine < 0)
                {
                    GetPointForOffset(document, offset + length, out endLine, out endColumn);
                }
                return endLine;
            }
        }

        public int EndColumn
        {
            get
            {
                if (endLine < 0)
                {
                    GetPointForOffset(document, offset + length, out endLine, out endColumn);
                }
                return endColumn;
            }
        }

        public int Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
                startLine = -1;
                endLine = -1;
            }
        }
        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
                endLine = -1;
            }
        }

        public bool IsFolded { get; set; } = false;

        public string FoldText { get; } = "...";

        public string InnerText
        {
            get
            {
                return document.GetText(offset, length);
            }
        }

        public FoldMarker(Document document, int offset, int length, string foldText, bool isFolded)
        {
            this.document = document;
            this.offset   = offset;
            this.length   = length;
            this.FoldText = foldText;
            this.IsFolded = isFolded;
        }

        public FoldMarker(Document document, int startLine, int startColumn, int endLine, int endColumn) : this(document, startLine, startColumn, endLine, endColumn, FoldType.Unspecified)
        {
        }

        public FoldMarker(Document document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType)  : this(document, startLine, startColumn, endLine, endColumn, foldType, "...")
        {
        }

        public FoldMarker(Document document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType, string foldText) : this(document, startLine, startColumn, endLine, endColumn, foldType, foldText, false)
        {
        }

        public FoldMarker(Document document, int startLine, int startColumn, int endLine, int endColumn, FoldType foldType, string foldText, bool isFolded)
        {
            this.document = document;

            startLine = Math.Min(document.TotalNumberOfLines - 1, Math.Max(startLine, 0));
            LineSegment startLineSegment = document.GetLineSegment(startLine);

            endLine = Math.Min(document.TotalNumberOfLines - 1, Math.Max(endLine, 0));
            LineSegment endLineSegment   = document.GetLineSegment(endLine);

            // Prevent the region from completely disappearing
            if (string.IsNullOrEmpty(foldText))
            {
                foldText = "...";
            }

            this.FoldType = foldType;
            this.FoldText = foldText;
            this.offset = startLineSegment.Offset + Math.Min(startColumn, startLineSegment.Length);
            this.length = (endLineSegment.Offset + Math.Min(endColumn, endLineSegment.Length)) - this.offset;
            this.IsFolded = isFolded;
        }

        public int CompareTo(object o)
        {
            if (!(o is FoldMarker))
            {
                throw new ArgumentException();
            }
            FoldMarker f = (FoldMarker)o;
            if (offset != f.offset)
            {
                return offset.CompareTo(f.offset);
            }

            return length.CompareTo(f.length);
        }
    }
}
