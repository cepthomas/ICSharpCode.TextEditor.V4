// This file has been added to the base project by me. License is WTFPL.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

// TODO3 Replace in files? Part of refactoring?

namespace ICSharpCode.TextEditor.Document
{
    public class Finder // TODO0 debug all - need unit tests
    {
        #region Enums
        /// <summary>Find modes.</summary>
        public enum FindMode { Normal, Extended, Wildcard, Regex }
        #endregion

        #region Helper classes
        /// <summary>One instance of a find.</summary>
        public class FindInfo
        {
            public int Index { get; set; } // in text buffer
            public int LineNumber { get; set; } // as seen
            public string LineText { get; set; } // the line
            public string MatchedText { get; set; } // to highlight

            public FindInfo(int index, int linenum, string ltext, string mtext)
            {
                MatchedText = mtext;
                LineText = ltext;
                LineNumber = linenum;
                Index = index;
            }
        };

        /// <summary>All the found stuff in one file.</summary>
        public class FileFindInfo
        {
            public string FilePath { get; set; }
            public List<FindInfo> Infos { get; set; }

            public FileFindInfo(string filepath)
            {
                Infos = new List<FindInfo>();
                FilePath = filepath;
            }
        };
        #endregion

        #region Properties
        /// <summary>How to search.</summary>
        public FindMode Mode { get; set; }

        /// <summary>How to search.</summary>
        public bool MatchCase { get; set; }

        /// <summary>How to search.</summary>
        public bool MatchWholeWord { get; set; }

        /// <summary>The editor control of interest.</summary>
        public TextEditorControl Editor { get; set; }
        #endregion

        #region Fields
        /// <summary>The reg ex object. It is null if the search is not started.</summary>
        Regex _regex = null;

        /// <summary>Last match index.</summary>
        int _matchIndex;
        #endregion

        #region Lifecycle
        /// <summary>Constructor</summary>
        public Finder()
        {
            _regex = null;
        }
        #endregion

        #region Public worker functions
        /// <summary>Escape magic chars so we can use regex for plain old filters.
        /// Static so the whole world can use it.</summary>
        /// <returns></returns>
        public static Regex CreateRegex(string regExString, FindMode mode, bool matchCase, bool matchWholeWord, bool next)
        {
            Regex r = null;

            switch (mode)
            {
                case FindMode.Normal:
                    // replace escape characters
                    regExString = Regex.Escape(regExString);
                    //regExString = regExString.Replace(@"\*", ".*").Replace(@"\?", ".");
                    break;

                case FindMode.Wildcard:
                    regExString = regExString.Replace("*", @"\w*");     // multiple characters wildcard (*)
                    regExString = regExString.Replace("?", @"\w");      // single character wildcard (?)
                    break;

                case FindMode.Extended:
                    //\n  new line (LF)
                    //\r   carriage return (CR)
                    //\t   tab character
                    //\0  null character
                    //\xddd   special character with code ddd
                    regExString = regExString.Replace(@"\\", @"\");
                    break;

                case FindMode.Regex:
                    // No changes needed.
                    break;
            }

            // Figure out options.
            if (matchWholeWord || mode == FindMode.Wildcard) // if wild cards selected, find whole words only
            {
                regExString = string.Format("{0}{1}{0}", @"\b", regExString);
            }

            RegexOptions options = RegexOptions.None;
            if (!next)
            {
                options |= RegexOptions.RightToLeft;
            }

            if (!matchCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return new Regex(regExString, options);
        }

        /// <summary>Find next (or previous) in current editor. It does the UI selection.</summary>
        /// <param name="ftext"></param>
        /// <param name="next">False means previous.</param>
        /// <returns>Text found</returns>
        public bool Find(string ftext, bool next = true)
        {
            bool found = false;

            Match match;
            
            // Is this the first time called?
            if (_regex == null)
            {
                _regex = CreateRegex(ftext, Mode, MatchCase, MatchWholeWord, next);

                match = _regex.Match(Editor.Document.TextContent, next ? 0 : Editor.Document.TextLength - 1);
            }
            else
            {
                match = _regex.Match(Editor.Document.TextContent, _matchIndex);
            }

            // save last
            _matchIndex = next ? match.Index + 1 : match.Index - 1;

            // Found a match?
            if (match.Success)
            {
                // Then select it.
                TextLocation p1 = Editor.Document.OffsetToPosition(match.Index);
                TextLocation p2 = Editor.Document.OffsetToPosition(match.Index + match.Length);
                Editor.ActiveTextAreaControl.SelectionManager.SetSelection(p1, p2);
                Editor.ActiveTextAreaControl.ScrollTo(p1.Line, p1.Column);
                Editor.ActiveTextAreaControl.Caret.Position = Editor.Document.OffsetToPosition(match.Index + match.Length);
            }

            return found;
        }

        /// <summary>Find all in current editor. It does the UI selection.</summary>
        /// <param name="ftext"></param>
        /// <returns>How many instances</returns>
        public int FindAll(string ftext)
        {
            int num = 0;

            Editor.BeginUpdate();

            try
            {
                Color sel = Editor.Document.TextEditorProperties.CaretMarkerColor.BackgroundColor;
                _regex = CreateRegex(ftext, Mode, MatchCase, MatchWholeWord, true);
                foreach (Match match in _regex.Matches(Editor.Document.TextContent))
                {
                    TextMarker mk = new TextMarker(match.Index, match.Length, TextMarkerType.SolidBlock, sel);
                    Editor.Document.MarkerStrategy.AddMarker(mk);
                    num++;
                }
            }
            finally
            {
                Editor.EndUpdate();
            }

            return num;
        }

        /// <summary>Performs one replacement in current editor.</summary>
        /// <param name="ftext"></param>
        /// <param name="rtext"></param>
        /// <returns>True if replacement made, otherwise not found.</returns>
        public bool Replace(string ftext, string rtext)
        {
            bool replaced = false;

            Editor.Document.UndoStack.StartUndoGroup();
            Editor.BeginUpdate();

            try
            {
                _regex = CreateRegex(ftext, Mode, MatchCase, MatchWholeWord, true);

                Match match = _regex.Match(Editor.Document.TextContent);
                if (match.Success)
                {
                    Editor.Document.Replace(match.Index, match.Length, rtext);
                    replaced = true;
                }
            }
            finally
            {
                Editor.Document.UndoStack.EndUndoGroup();
                Editor.EndUpdate();
            }

            return replaced;
        }

        /// <summary>Performs all replacements in current editor.</summary>
        /// <param name="ftext"></param>
        /// <param name="rtext"></param>
        /// <returns>How many instances</returns>
        public int ReplaceAll(string ftext, string rtext)
        {
            int num = 0;

            Editor.Document.UndoStack.StartUndoGroup();
            Editor.BeginUpdate();

            try
            {
                _regex = CreateRegex(ftext, Mode, MatchCase, MatchWholeWord, true);

                foreach (Match match in _regex.Matches(Editor.Document.TextContent))
                {
                    Editor.Document.Replace(match.Index, match.Length, rtext);
                    num++;
                }
            }
            finally
            {
                Editor.Document.UndoStack.EndUndoGroup();
                Editor.EndUpdate();
            }

            return num;
        }

        /// <summary>Find the entered text and pattern in all files in the provided directory.</summary>
        public List<FileFindInfo> FindInFiles(string ftext, string directory, string patterns, bool subdirs)
        {
            int num = 0;
            List<FileFindInfo> results = new List<FileFindInfo>();

            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            string[] apatterns = patterns.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pattern in apatterns)
            {
                foreach (FileInfo file in dirInfo.GetFiles(pattern.Trim(), SearchOption.AllDirectories))
                {
                    FileFindInfo ffi = FindInFile(file);
                    num += ffi.Infos.Count();
                    results.Add(ffi);
                }
            }

            return results;
        }
        #endregion

        #region Private helper functions
        /// <summary>Finds the given text and pattern in the given file.</summary>
        /// <param name="file">The file to look in</param>
        private FileFindInfo FindInFile(FileInfo file)
        {
            StreamReader sr = new StreamReader(file.FullName);
            string line;
            int lineNum = 1;
            FileFindInfo ffi = new FileFindInfo(file.FullName);

            while ((line = sr.ReadLine()) != null)
            {
                Match m = _regex.Match(line);
                if (m.Success)
                {
                    ffi.Infos.Add(new FindInfo(m.Index, lineNum, line, m.ToString()));
                }
                lineNum++;
            }

            return ffi;
        }
        #endregion
    }
}