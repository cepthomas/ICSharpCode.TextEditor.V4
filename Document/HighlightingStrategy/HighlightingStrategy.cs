// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Document
{
    public class HighlightingStrategy
    {
        #region Fields
        HighlightRuleSet _defaultRuleSet = null;

        // Line state variable
        protected LineSegment _currentLine;
        protected int _currentLineNumber;

        // Span stack state variable
        protected SpanStack _currentSpanStack;

        // Span state variables
        protected bool _inSpan;
        protected Span _activeSpan;
        protected HighlightRuleSet _activeRuleSet;

        // Line scanning state variables
        protected int _currentOffset;
        protected int _currentLength;
        #endregion

        #region Properties
        public string Folding { get; set; } = "";

        public HighlightColor DigitColor { get; set; } = new HighlightColor();

        public Dictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

        public string Name { get; private set; } = "";

        public string[] Extensions { get; set; } = new string[] { };

        public List<HighlightRuleSet> Rules { get; private set; } = new List<HighlightRuleSet>();

        public HighlightColor DefaultTextColor { get; set; } = new HighlightColor();
        #endregion

        public HighlightingStrategy(string name)
        {
            Name = name;
            DigitColor = new HighlightColor(SystemColors.WindowText, false, false);
            DefaultTextColor = new HighlightColor(SystemColors.WindowText, false, false);
        }

        public HighlightRuleSet FindHighlightRuleSet(string name)
        {
            foreach (HighlightRuleSet ruleSet in Rules)
            {
                if (ruleSet.Name == name)
                {
                    return ruleSet;
                }
            }
            return null;
        }

        public void AddRuleSet(HighlightRuleSet aRuleSet)
        {
            HighlightRuleSet existing = FindHighlightRuleSet(aRuleSet.Name);
            if (existing != null)
            {
                existing.MergeFrom(aRuleSet);
            }
            else
            {
                Rules.Add(aRuleSet);
            }
        }

        //public void ResolveReferences()
        //{
        //    // Resolve references from Span definitions to RuleSets
        //    ResolveRuleSetReferences();

        //    // Resolve references from RuleSet defintitions to Highlighters defined in an external mode file
        //    ResolveExternalReferences();
        //}

        public void ResolveRuleSetReferences()
        {
            foreach (HighlightRuleSet ruleSet in Rules)
            {
                if (ruleSet.Name == null)
                {
                    _defaultRuleSet = ruleSet;
                }

                foreach (Span aSpan in ruleSet.Spans)
                {
                    if (aSpan.Rule != null)
                    {
                        bool found = false;
                        foreach (HighlightRuleSet refSet in Rules)
                        {
                            if (refSet.Name == aSpan.Rule)
                            {
                                found = true;
                                aSpan.RuleSet = refSet;
                                break;
                            }
                        }
                        if (!found)
                        {
                            aSpan.RuleSet = null;
                            throw new Exception("The RuleSet " + aSpan.Rule + " could not be found in mode definition " + Name);
                        }
                    }
                    else
                    {
                        aSpan.RuleSet = null;
                    }
                }
            }

            if (_defaultRuleSet == null)
            {
                throw new Exception("No default RuleSet is defined for mode definition " + Name);
            }
        }

        //void ResolveExternalReferences()
        //{
        //    foreach (HighlightRuleSet ruleSet in Rules)
        //    {
        //        ruleSet.Highlighter = this;
        //        if (ruleSet.Reference != null)
        //        {
        //            HighlightingStrategy highlighter = HighlightingManager.Instance.FindHighlighter(ruleSet.Reference);

        //            if (highlighter == null)
        //            {
        //                throw new Exception("The mode defintion " + ruleSet.Reference + " which is refered from the " + Name + " mode definition could not be found");
        //            }
        //            else
        //            {
        //                ruleSet.Highlighter = highlighter;
        //            }
        //        }
        //    }
        //}

        //protected void ImportSettingsFrom(HighlightingStrategy source)
        //{
        //    Properties = source.Properties;
        //    Extensions = source.Extensions;
        //    DigitColor = source.DigitColor;
        //    defaultRuleSet = source.defaultRuleSet;
        //    Name = source.Name;
        //    Folding = source.Folding;
        //    Rules = source.Rules;
        //    DefaultTextColor = source.DefaultTextColor;
        //}

        public HighlightColor GetColor(Document document, LineSegment currentSegment, int currentOffset, int currentLength)
        {
            return GetColor(_defaultRuleSet, document, currentSegment, currentOffset, currentLength);
        }

        protected HighlightColor GetColor(HighlightRuleSet ruleSet, Document document, LineSegment currentSegment, int currentOffset, int currentLength)
        {
            if (ruleSet != null)
            {
                if (ruleSet.Reference != null)
                {
                    return ruleSet.Highlighter.GetColor(document, currentSegment, currentOffset, currentLength);
                }
                else
                {
                    return (HighlightColor)ruleSet.KeyWords[document, currentSegment, currentOffset, currentLength];
                }
            }
            return null;
        }

        public HighlightRuleSet GetRuleSet(Span aSpan)
        {
            if (aSpan == null)
            {
                return _defaultRuleSet;
            }
            else
            {
                if (aSpan.RuleSet != null)
                {
                    if (aSpan.RuleSet.Reference != null)
                    {
                        return aSpan.RuleSet.Highlighter.GetRuleSet(null);
                    }
                    else
                    {
                        return aSpan.RuleSet;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public void MarkTokens(Document document)
        {
            if (Rules.Count == 0)
            {
                return;
            }

            int lineNumber = 0;

            while (lineNumber < document.TotalNumberOfLines)
            {
                LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);
                if (lineNumber >= document.LineSegmentCollection.Count)   // may be, if the last line ends with a delimiter
                {
                    break;                                                // then the last line is not in the collection :)
                }

                _currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? previousLine.HighlightSpanStack.Clone() : null);

                if (_currentSpanStack != null)
                {
                    while (!_currentSpanStack.IsEmpty && _currentSpanStack.Peek().StopEOL)
                    {
                        _currentSpanStack.Pop();
                    }
                    if (_currentSpanStack.IsEmpty) _currentSpanStack = null;
                }

                _currentLine = (LineSegment)document.LineSegmentCollection[lineNumber];

                if (_currentLine.Length == -1)   // happens when buffer is empty !
                {
                    return;
                }

                _currentLineNumber = lineNumber;
                List<TextWord> words = ParseLine(document);
                // Alex: clear old words
                if (_currentLine.Words != null)
                {
                    _currentLine.Words.Clear();
                }
                _currentLine.Words = words;
                _currentLine.HighlightSpanStack = (_currentSpanStack == null || _currentSpanStack.IsEmpty) ? null : _currentSpanStack;

                ++lineNumber;
            }

            document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
            document.CommitUpdate();
            _currentLine = null;
        }

        bool MarkTokensInLine(Document document, int lineNumber, ref bool spanChanged)
        {
            _currentLineNumber = lineNumber;
            bool processNextLine = false;
            LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);

            _currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? previousLine.HighlightSpanStack.Clone() : null);
            if (_currentSpanStack != null)
            {
                while (!_currentSpanStack.IsEmpty && _currentSpanStack.Peek().StopEOL)
                {
                    _currentSpanStack.Pop();
                }
                if (_currentSpanStack.IsEmpty)
                {
                    _currentSpanStack = null;
                }
            }

            _currentLine = document.LineSegmentCollection[lineNumber];

            if (_currentLine.Length == -1)   // happens when buffer is empty !
            {
                return false;
            }

            List<TextWord> words = ParseLine(document);

            if (_currentSpanStack != null && _currentSpanStack.IsEmpty)
            {
                _currentSpanStack = null;
            }

            // Check if the span state has changed, if so we must re-render the next line
            // This check may seem utterly complicated but I didn't want to introduce any function calls
            // or allocations here for perf reasons.
            if (_currentLine.HighlightSpanStack != _currentSpanStack)
            {
                if (_currentLine.HighlightSpanStack == null)
                {
                    processNextLine = false;
                    foreach (Span sp in _currentSpanStack)
                    {
                        if (!sp.StopEOL)
                        {
                            spanChanged = true;
                            processNextLine = true;
                            break;
                        }
                    }
                }
                else if (_currentSpanStack == null)
                {
                    processNextLine = false;
                    foreach (Span sp in _currentLine.HighlightSpanStack)
                    {
                        if (!sp.StopEOL)
                        {
                            spanChanged = true;
                            processNextLine = true;
                            break;
                        }
                    }
                }
                else
                {
                    SpanStack.Enumerator e1 = _currentSpanStack.GetEnumerator();
                    SpanStack.Enumerator e2 = _currentLine.HighlightSpanStack.GetEnumerator();
                    bool done = false;
                    while (!done)
                    {
                        bool blockSpanIn1 = false;
                        while (e1.MoveNext())
                        {
                            if (!((Span)e1.Current).StopEOL)
                            {
                                blockSpanIn1 = true;
                                break;
                            }
                        }
                        bool blockSpanIn2 = false;
                        while (e2.MoveNext())
                        {
                            if (!((Span)e2.Current).StopEOL)
                            {
                                blockSpanIn2 = true;
                                break;
                            }
                        }
                        if (blockSpanIn1 || blockSpanIn2)
                        {
                            if (blockSpanIn1 && blockSpanIn2)
                            {
                                if (e1.Current != e2.Current)
                                {
                                    done = true;
                                    processNextLine = true;
                                    spanChanged = true;
                                }
                            }
                            else
                            {
                                spanChanged = true;
                                done = true;
                                processNextLine = true;
                            }
                        }
                        else
                        {
                            done = true;
                            processNextLine = false;
                        }
                    }
                }
            }
            else
            {
                processNextLine = false;
            }

            //// Alex: remove old words
            if (_currentLine.Words != null)
                _currentLine.Words.Clear();
            _currentLine.Words = words;
            _currentLine.HighlightSpanStack = (_currentSpanStack != null && !_currentSpanStack.IsEmpty) ? _currentSpanStack : null;

            return processNextLine;
        }

        public void MarkTokens(Document document, List<LineSegment> inputLines)
        {
            if (Rules.Count == 0)
            {
                return;
            }

            Dictionary<LineSegment, bool> processedLines = new Dictionary<LineSegment, bool>();

            bool spanChanged = false;
            int documentLineSegmentCount = document.LineSegmentCollection.Count;

            foreach (LineSegment lineToProcess in inputLines)
            {
                if (!processedLines.ContainsKey(lineToProcess))
                {
                    int lineNumber = lineToProcess.LineNumber;
                    bool processNextLine = true;

                    if (lineNumber != -1)
                    {
                        while (processNextLine && lineNumber < documentLineSegmentCount)
                        {
                            processNextLine = MarkTokensInLine(document, lineNumber, ref spanChanged);
                            processedLines[_currentLine] = true;
                            ++lineNumber;
                        }
                    }
                }
            }

            if (spanChanged || inputLines.Count > 20)
            {
                // if the span was changed (more than inputLines lines had to be reevaluated)
                // or if there are many lines in inputLines, it's faster to update the whole
                // text area instead of many small segments
                document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
            }
            else
            {
                //				document.Caret.ValidateCaretPos();
                //				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
                //
                foreach (LineSegment lineToProcess in inputLines)
                {
                    document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, lineToProcess.LineNumber));
                }

            }
            document.CommitUpdate();
            _currentLine = null;
        }

        void UpdateSpanStateVariables()
        {
            _inSpan = (_currentSpanStack != null && !_currentSpanStack.IsEmpty);
            _activeSpan = _inSpan ? _currentSpanStack.Peek() : null;
            _activeRuleSet = GetRuleSet(_activeSpan);
        }

        List<TextWord> ParseLine(Document document)
        {
            List<TextWord> words = new List<TextWord>();
            HighlightColor markNext = null;

            _currentOffset = 0;
            _currentLength = 0;
            UpdateSpanStateVariables();

            int currentLineLength = _currentLine.Length;
            int currentLineOffset = _currentLine.Offset;

            for (int i = 0; i < currentLineLength; ++i)
            {
                char ch = document.GetCharAt(currentLineOffset + i);
                switch (ch)
                {
                    case '\n':
                    case '\r':
                        PushCurWord(document, ref markNext, words);
                        ++_currentOffset;
                        break;

                    case ' ':
                        PushCurWord(document, ref markNext, words);
                        if (_activeSpan != null && _activeSpan.Color.HasBackground)
                        {
                            words.Add(new SpaceTextWord(_activeSpan.Color));
                        }
                        else
                        {
                            words.Add(TextWord.Space);
                        }
                        ++_currentOffset;
                        break;

                    case '\t':
                        PushCurWord(document, ref markNext, words);
                        if (_activeSpan != null && _activeSpan.Color.HasBackground)
                        {
                            words.Add(new TabTextWord(_activeSpan.Color));
                        }
                        else
                        {
                            words.Add(TextWord.Tab);
                        }
                        ++_currentOffset;
                        break;

                    default:
                        {
                            // handle escape characters
                            char escapeCharacter = '\0';
                            if (_activeSpan != null && _activeSpan.EscapeCharacter != '\0')
                            {
                                escapeCharacter = _activeSpan.EscapeCharacter;
                            }
                            else if (_activeRuleSet != null)
                            {
                                escapeCharacter = _activeRuleSet.EscapeCharacter;
                            }
                            if (escapeCharacter != '\0' && escapeCharacter == ch)
                            {
                                // we found the escape character
                                if (_activeSpan != null && _activeSpan.End != null && _activeSpan.End.Length == 1
                                        && escapeCharacter == _activeSpan.End[0])
                                {
                                    // the escape character is a end-doubling escape character
                                    // it may count as escape only when the next character is the escape, too
                                    if (i + 1 < currentLineLength)
                                    {
                                        if (document.GetCharAt(currentLineOffset + i + 1) == escapeCharacter)
                                        {
                                            _currentLength += 2;
                                            PushCurWord(document, ref markNext, words);
                                            ++i;
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    // this is a normal \-style escape
                                    ++_currentLength;
                                    if (i + 1 < currentLineLength)
                                    {
                                        ++_currentLength;
                                    }
                                    PushCurWord(document, ref markNext, words);
                                    ++i;
                                    continue;
                                }
                            }

                            // highlight digits
                            if (!_inSpan && (Char.IsDigit(ch) || (ch == '.' && i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1)))) && _currentLength == 0)
                            {
                                bool ishex = false;
                                bool isfloatingpoint = false;

                                if (ch == '0' && i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'X')   // hex digits
                                {
                                    const string hex = "0123456789ABCDEF";
                                    ++_currentLength;
                                    ++i; // skip 'x'
                                    ++_currentLength;
                                    ishex = true;
                                    while (i + 1 < currentLineLength && hex.IndexOf(Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1))) != -1)
                                    {
                                        ++i;
                                        ++_currentLength;
                                    }
                                }
                                else
                                {
                                    ++_currentLength;
                                    while (i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1)))
                                    {
                                        ++i;
                                        ++_currentLength;
                                    }
                                }
                                if (!ishex && i + 1 < currentLineLength && document.GetCharAt(currentLineOffset + i + 1) == '.')
                                {
                                    isfloatingpoint = true;
                                    ++i;
                                    ++_currentLength;
                                    while (i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1)))
                                    {
                                        ++i;
                                        ++_currentLength;
                                    }
                                }

                                if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'E')
                                {
                                    isfloatingpoint = true;
                                    ++i;
                                    ++_currentLength;
                                    if (i + 1 < currentLineLength && (document.GetCharAt(currentLineOffset + i + 1) == '+' || document.GetCharAt(_currentLine.Offset + i + 1) == '-'))
                                    {
                                        ++i;
                                        ++_currentLength;
                                    }
                                    while (i + 1 < _currentLine.Length && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1)))
                                    {
                                        ++i;
                                        ++_currentLength;
                                    }
                                }

                                if (i + 1 < _currentLine.Length)
                                {
                                    char nextch = Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1));
                                    if (nextch == 'F' || nextch == 'M' || nextch == 'D')
                                    {
                                        isfloatingpoint = true;
                                        ++i;
                                        ++_currentLength;
                                    }
                                }

                                if (!isfloatingpoint)
                                {
                                    bool isunsigned = false;
                                    if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'U')
                                    {
                                        ++i;
                                        ++_currentLength;
                                        isunsigned = true;
                                    }
                                    if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'L')
                                    {
                                        ++i;
                                        ++_currentLength;
                                        if (!isunsigned && i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'U')
                                        {
                                            ++i;
                                            ++_currentLength;
                                        }
                                    }
                                }

                                words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, DigitColor, false));
                                _currentOffset += _currentLength;
                                _currentLength = 0;
                                continue;
                            }

                            // Check for SPAN ENDs
                            if (_inSpan)
                            {
                                if (_activeSpan.End != null && _activeSpan.End.Length > 0)
                                {
                                    if (MatchExpr(_currentLine, _activeSpan.End, i, document, _activeSpan.IgnoreCase))
                                    {
                                        PushCurWord(document, ref markNext, words);
                                        string regex = GetRegString(_currentLine, _activeSpan.End, i, document);
                                        _currentLength += regex.Length;
                                        words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, _activeSpan.EndColor, false));
                                        _currentOffset += _currentLength;
                                        _currentLength = 0;
                                        i += regex.Length - 1;
                                        _currentSpanStack.Pop();
                                        UpdateSpanStateVariables();
                                        continue;
                                    }
                                }
                            }

                            // check for SPAN BEGIN
                            if (_activeRuleSet != null)
                            {
                                foreach (Span span in _activeRuleSet.Spans)
                                {
                                    if ((!span.IsBeginSingleWord || _currentLength == 0)
                                            && (!span.IsBeginStartOfLine.HasValue || span.IsBeginStartOfLine.Value == (_currentLength == 0 && words.TrueForAll(delegate (TextWord textWord)
                                {
                                    return textWord.Type != TextWordType.Word;
                                })))
                                && MatchExpr(_currentLine, span.Begin, i, document, _activeRuleSet.IgnoreCase))
                                    {
                                        PushCurWord(document, ref markNext, words);
                                        string regex = GetRegString(_currentLine, span.Begin, i, document);

                                        if (!OverrideSpan(regex, document, words, span, ref i))
                                        {
                                            _currentLength += regex.Length;
                                            words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, span.BeginColor, false));
                                            _currentOffset += _currentLength;
                                            _currentLength = 0;

                                            i += regex.Length - 1;
                                            if (_currentSpanStack == null)
                                            {
                                                _currentSpanStack = new SpanStack();
                                            }
                                            _currentSpanStack.Push(span);
                                            span.IgnoreCase = _activeRuleSet.IgnoreCase;

                                            UpdateSpanStateVariables();
                                        }

                                        goto skip;
                                    }
                                }
                            }

                            // check if the char is a delimiter
                            if (_activeRuleSet != null && ch < 256 && _activeRuleSet.Delimiters[ch])
                            {
                                PushCurWord(document, ref markNext, words);
                                if (_currentOffset + _currentLength + 1 < _currentLine.Length)
                                {
                                    ++_currentLength;
                                    PushCurWord(document, ref markNext, words);
                                    goto skip;
                                }
                            }

                            ++_currentLength;
                        skip: continue;
                        }
                }
            }

            PushCurWord(document, ref markNext, words);

            OnParsedLine(document, _currentLine, words);

            return words;
        }

        protected void OnParsedLine(Document document, LineSegment currentLine, List<TextWord> words)
        {
        }

        protected bool OverrideSpan(string spanBegin, Document document, List<TextWord> words, Span span, ref int lineOffset)
        {
            return false;
        }

        /// <summary>
        /// pushes the curWord string on the word list, with the correct color.
        /// </summary>
        void PushCurWord(Document document, ref HighlightColor markNext, List<TextWord> words)
        {
            // Svante Lidman : Need to look through the next prev logic.
            if (_currentLength > 0)
            {
                if (words.Count > 0 && _activeRuleSet != null)
                {
                    TextWord prevWord = null;
                    int pInd = words.Count - 1;
                    while (pInd >= 0)
                    {
                        if (!words[pInd].IsWhiteSpace)
                        {
                            prevWord = (TextWord)words[pInd];
                            if (prevWord.HasDefaultColor)
                            {
                                AdjacentMarker marker = (AdjacentMarker)_activeRuleSet.PrevMarkers[document, _currentLine, _currentOffset, _currentLength];
                                if (marker != null)
                                {
                                    prevWord.SyntaxColor = marker.Color;
                                    //									document.Caret.ValidateCaretPos();
                                    //									document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
                                }
                            }
                            break;
                        }
                        pInd--;
                    }
                }

                if (_inSpan)
                {
                    HighlightColor c = null;
                    bool hasDefaultColor = true;
                    if (_activeSpan.Rule == null)
                    {
                        c = _activeSpan.Color;
                    }
                    else
                    {
                        c = GetColor(_activeRuleSet, document, _currentLine, _currentOffset, _currentLength);
                        hasDefaultColor = false;
                    }

                    if (c == null)
                    {
                        c = _activeSpan.Color;
                        if (c.Color == Color.Transparent)
                        {
                            c = DefaultTextColor;
                        }
                        hasDefaultColor = true;
                    }
                    words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, markNext != null ? markNext : c, hasDefaultColor));
                }
                else
                {
                    HighlightColor c = markNext != null ? markNext : GetColor(_activeRuleSet, document, _currentLine, _currentOffset, _currentLength);
                    if (c == null)
                    {
                        words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, DefaultTextColor, true));
                    }
                    else
                    {
                        words.Add(new TextWord(document, _currentLine, _currentOffset, _currentLength, c, false));
                    }
                }

                if (_activeRuleSet != null)
                {
                    AdjacentMarker nextMarker = (AdjacentMarker)_activeRuleSet.NextMarkers[document, _currentLine, _currentOffset, _currentLength];
                    if (nextMarker != null)
                    {
                        if (nextMarker.MarkMarker && words.Count > 0)
                        {
                            TextWord prevword = ((TextWord)words[words.Count - 1]);
                            prevword.SyntaxColor = nextMarker.Color;
                        }
                        markNext = nextMarker.Color;
                    }
                    else
                    {
                        markNext = null;
                    }
                }
                _currentOffset += _currentLength;
                _currentLength = 0;
            }
        }

        #region Matching
        /// <summary>
        /// get the string, which matches the regular expression expr,
        /// in string s2 at index
        /// </summary>
        static string GetRegString(LineSegment lineSegment, char[] expr, int index, Document document)
        {
            int j = 0;
            StringBuilder regexpr = new StringBuilder();

            for (int i = 0; i < expr.Length; ++i, ++j)
            {
                if (index + j >= lineSegment.Length)
                    break;

                switch (expr[i])
                {
                    case '@': // "special" meaning
                        ++i;
                        if (i == expr.Length)
                            throw new Exception("Unexpected end of @ sequence, use @@ to look for a single @.");

                        switch (expr[i])
                        {
                            case '!': // don't match the following expression
                                StringBuilder whatmatch = new StringBuilder();
                                ++i;
                                while (i < expr.Length && expr[i] != '@')
                                {
                                    whatmatch.Append(expr[i++]);
                                }
                                break;
                            case '@': // matches @
                                regexpr.Append(document.GetCharAt(lineSegment.Offset + index + j));
                                break;
                        }
                        break;
                    default:
                        if (expr[i] != document.GetCharAt(lineSegment.Offset + index + j))
                        {
                            return regexpr.ToString();
                        }
                        regexpr.Append(document.GetCharAt(lineSegment.Offset + index + j));
                        break;
                }
            }
            return regexpr.ToString();
        }

        /// <summary>
        /// returns true, if the get the string s2 at index matches the expression expr
        /// </summary>
        static bool MatchExpr(LineSegment lineSegment, char[] expr, int index, Document document, bool ignoreCase)
        {
            for (int i = 0, j = 0; i < expr.Length; ++i, ++j)
            {
                switch (expr[i])
                {
                    case '@': // "special" meaning
                        ++i;
                        if (i == expr.Length)
                            throw new Exception("Unexpected end of @ sequence, use @@ to look for a single @.");
                        switch (expr[i])
                        {
                            case 'C': // match whitespace or punctuation
                                if (index + j == lineSegment.Offset || index + j >= lineSegment.Offset + lineSegment.Length)
                                {
                                    // nothing (EOL or SOL)
                                }
                                else
                                {
                                    char ch = document.GetCharAt(lineSegment.Offset + index + j);
                                    if (!Char.IsWhiteSpace(ch) && !Char.IsPunctuation(ch))
                                    {
                                        return false;
                                    }
                                }
                                break;
                            case '!': // don't match the following expression
                                {
                                    StringBuilder whatmatch = new StringBuilder();
                                    ++i;
                                    while (i < expr.Length && expr[i] != '@')
                                    {
                                        whatmatch.Append(expr[i++]);
                                    }
                                    if (lineSegment.Offset + index + j + whatmatch.Length < document.TextLength)
                                    {
                                        int k = 0;
                                        for (; k < whatmatch.Length; ++k)
                                        {
                                            char docChar = ignoreCase ? Char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index + j + k)) : document.GetCharAt(lineSegment.Offset + index + j + k);
                                            char spanChar = ignoreCase ? Char.ToUpperInvariant(whatmatch[k]) : whatmatch[k];
                                            if (docChar != spanChar)
                                            {
                                                break;
                                            }
                                        }
                                        if (k >= whatmatch.Length)
                                        {
                                            return false;
                                        }
                                    }
                                    //									--j;
                                    break;
                                }
                            case '-': // don't match the  expression before
                                {
                                    StringBuilder whatmatch = new StringBuilder();
                                    ++i;
                                    while (i < expr.Length && expr[i] != '@')
                                    {
                                        whatmatch.Append(expr[i++]);
                                    }
                                    if (index - whatmatch.Length >= 0)
                                    {
                                        int k = 0;
                                        for (; k < whatmatch.Length; ++k)
                                        {
                                            char docChar = ignoreCase ? Char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index - whatmatch.Length + k)) : document.GetCharAt(lineSegment.Offset + index - whatmatch.Length + k);
                                            char spanChar = ignoreCase ? Char.ToUpperInvariant(whatmatch[k]) : whatmatch[k];
                                            if (docChar != spanChar)
                                                break;
                                        }
                                        if (k >= whatmatch.Length)
                                        {
                                            return false;
                                        }
                                    }
                                    //									--j;
                                    break;
                                }
                            case '@': // matches @
                                if (index + j >= lineSegment.Length || '@' != document.GetCharAt(lineSegment.Offset + index + j))
                                {
                                    return false;
                                }
                                break;
                        }
                        break;
                    default:
                        {
                            if (index + j >= lineSegment.Length)
                            {
                                return false;
                            }
                            char docChar = ignoreCase ? char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index + j)) : document.GetCharAt(lineSegment.Offset + index + j);
                            char spanChar = ignoreCase ? char.ToUpperInvariant(expr[i]) : expr[i];
                            if (docChar != spanChar)
                            {
                                return false;
                            }
                            break;
                        }
                }
            }
            return true;
        }
        #endregion
    }
}
