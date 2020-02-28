// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 2658$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor
{
    /// <summary>A line/column position. Text editor lines/columns are counting from zero.</summary>
    public class TextLocation : IComparable<TextLocation>, IEquatable<TextLocation>
    {
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>Represents no text location (-1, -1).</summary>
        public TextLocation()
        {
            X = -1;
            Y = -1;
        }

        public TextLocation(int column, int line)
        {
            X = column;
            Y = line;
        }

        public int Line
        {
            get { return Y; }
            set { Y = value; }
        }

        public int Column
        {
            get { return X; }
            set { X = value; }
        }

        public bool IsValid
        {
            get { return X >= 0 && Y >= 0; }
        }

        public override string ToString()
        {
            return string.Format("(Line {1}, Col {0})", X, Y);
        }

        //public override int GetHashCode()
        //{
        //    return unchecked (87 * X.GetHashCode() ^ Y.GetHashCode());
        //}

        public override bool Equals(object obj)
        {
            if (!(obj is TextLocation)) 
                return false;

            return (TextLocation)obj == this;
        }

        public bool Equals(TextLocation other)
        {
            return this == other;
        }

        public static bool operator ==(TextLocation a, TextLocation b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(TextLocation a, TextLocation b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public static bool operator <(TextLocation a, TextLocation b)
        {
            if (a.Y < b.Y)
                return true;
            else if (a.Y == b.Y)
                return a.X < b.X;
            else
                return false;
        }

        public static bool operator >(TextLocation a, TextLocation b)
        {
            if (a.Y > b.Y)
                return true;
            else if (a.Y == b.Y)
                return a.X > b.X;
            else
                return false;
        }

        public static bool operator <=(TextLocation a, TextLocation b)
        {
            return !(a > b);
        }

        public static bool operator >=(TextLocation a, TextLocation b)
        {
            return !(a < b);
        }

        public int CompareTo(TextLocation other)
        {
            if (this == other)
                return 0;
            if (this < other)
                return -1;
            else
                return 1;
        }
    }
}
