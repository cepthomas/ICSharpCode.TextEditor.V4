// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
    public enum TextMarkerType
    {
        Invisible,
        SolidBlock,
        Underlined,
        WaveLine
    }

    /// <summary>Marks a part of a document.</summary>
    public class TextMarker : ISegment
    {
        //public virtual int Offset { get; set; } = -1;

        //public virtual int Length { get; set; } = -1;
        public int Offset { get; set; } = -1;

        public int Length { get; set; } = -1;

        public TextMarkerType TextMarkerType { get; private set; }

        public Color Color { get; private set; }

        public Color ForeColor { get; private set; }

        public bool OverrideForeColor { get; private set; }

        /// <summary>Marks the text segment as read-only.</summary>
        public bool IsReadOnly { get; set; }

        public string ToolTip { get; set; }

        /// <summary>Gets the last offset that is inside the marker region.</summary>
        public int EndOffset { get { return Offset + Length - 1; } }

        public TextMarker(int offset, int length, TextMarkerType textMarkerType, Color color)
        {
            if (length < 1) length = 1;
            Offset = offset;
            Length = length;
            TextMarkerType = textMarkerType;
            Color = color;
        }

        //public TextMarker(int offset, int length, TextMarkerType textMarkerType, Color color, Color foreColor)
        //{
        //    if (length < 1) length = 1;
        //    Offset = offset;
        //    Length = length;
        //    TextMarkerType = textMarkerType;
        //    Color = color;
        //    ForeColor = foreColor;
        //    OverrideForeColor = true;
        //}
    }

    /// <summary>
    /// Manages the list of markers and provides ways to retrieve markers for specific positions.
    /// </summary>
    public sealed class MarkerStrategy
    {
        readonly List<TextMarker> _textMarker = new List<TextMarker>();
        readonly Dictionary<int, List<TextMarker>> _markersTable = new Dictionary<int, List<TextMarker>>();

        public Document Document { get; }

        //public IEnumerable<TextMarker> TextMarker { get { return _textMarker.AsReadOnly(); } }

        public void AddMarker(TextMarker item)
        {
            _markersTable.Clear();
            _textMarker.Add(item);
        }

        //public void InsertMarker(int index, TextMarker item)
        //{
        //    _markersTable.Clear();
        //    _textMarker.Insert(index, item);
        //}

        //public void RemoveMarker(TextMarker item)
        //{
        //    _markersTable.Clear();
        //    _textMarker.Remove(item);
        //}

        //public void RemoveAll(Predicate<TextMarker> match)
        //{
        //    _markersTable.Clear();
        //    _textMarker.RemoveAll(match);
        //}

        public MarkerStrategy(Document document)
        {
            Document = document;
            document.DocumentChanged += DocumentChanged;
        }

        public List<TextMarker> GetMarkers(int offset)
        {
            if (!_markersTable.ContainsKey(offset))
            {
                List<TextMarker> markers = new List<TextMarker>();
                for (int i = 0; i < _textMarker.Count; ++i)
                {
                    TextMarker marker = _textMarker[i];
                    if (marker.Offset <= offset && offset <= marker.EndOffset)
                    {
                        markers.Add(marker);
                    }
                }
                _markersTable[offset] = markers;
            }
            return _markersTable[offset];
        }

        public List<TextMarker> GetMarkers(int offset, int length)
        {
            int endOffset = offset + length - 1;
            List<TextMarker> markers = new List<TextMarker>();
            for (int i = 0; i < _textMarker.Count; ++i)
            {
                TextMarker marker = _textMarker[i];
                if
                (
                    // start in marker region
                    marker.Offset <= offset && offset <= marker.EndOffset ||
                    // end in marker region
                    marker.Offset <= endOffset && endOffset <= marker.EndOffset ||
                    // marker start in region
                    offset <= marker.Offset && marker.Offset <= endOffset ||
                    // marker end in region
                    offset <= marker.EndOffset && marker.EndOffset <= endOffset
                )
                {
                    markers.Add(marker);
                }
            }
            return markers;
        }

        public List<TextMarker> GetMarkers(TextLocation position)
        {
            if (position.Y >= Document.TotalNumberOfLines || position.Y < 0)
            {
                return new List<TextMarker>();
            }
            LineSegment segment = Document.GetLineSegment(position.Y);
            return GetMarkers(segment.Offset + position.X);
        }

        void DocumentChanged(object sender, DocumentEventArgs e)
        {
            // reset markers table
            _markersTable.Clear();
            Document.UpdateSegmentListOnDocumentChange(_textMarker, e);
        }
    }
}
