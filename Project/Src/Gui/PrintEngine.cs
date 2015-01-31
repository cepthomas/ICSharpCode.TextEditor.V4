// This file has been added to the base project by me. License is WTFPL.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    // Mostly borrowed from ICSharpCode.TextEditor example but made more generally useful.
    public class PrintEngine
    {
        #region Printing support
        PageSettings _pageSettings = new PageSettings();
        PrinterSettings _printerSettings = new PrinterSettings();
        int _curLineNumber = 0;
        int _endLineNumber = 0;
        int _curPageNumber = 0;
        int _totalPages = 0;
        StringFormat _printingStringFormat;
        HighlightColor _hcDef = new HighlightColor(Color.Black, false, false);
        // fixed printing fonts until the layout control is improved
        float _xSpace = 7.5f;
        float _ySpace = 16.0f;
        #endregion

        TextEditorControl _editor;

        public PrintEngine()
        {
            _pageSettings = new PageSettings();
            _pageSettings.Landscape = true;
            _pageSettings.Margins.Top = 50;
            _pageSettings.Margins.Bottom = 50;
            _pageSettings.Margins.Left = 50;
            _pageSettings.Margins.Right = 50;
            _printerSettings = new PrinterSettings();
        }

        #region Printing support

        // Edit the print settings.
        public void EditPrintSettings()
        {
            // Show the page setup dialog
            PageSetupDialog pageSetupDialog = new PageSetupDialog();
            pageSetupDialog.PageSettings = _pageSettings;
            pageSetupDialog.PrinterSettings = _printerSettings;
            pageSetupDialog.ShowNetwork = false;

            if (pageSetupDialog.ShowDialog() == DialogResult.OK)
            {
                _pageSettings = pageSetupDialog.PageSettings;
                _printerSettings = pageSetupDialog.PrinterSettings;
            }
        }

        // Start the print.
        public void Print(TextEditorControl editor)
        {
            _editor = editor;
            PrintDocument pdoc = new PrintDocument();

            pdoc.PrinterSettings = _printerSettings;
            pdoc.DefaultPageSettings = _pageSettings;

            PrintDialog pdlg = new PrintDialog();
            pdlg.AllowSelection = true;
            pdlg.ShowHelp = true;
            pdlg.Document = pdoc;
            pdlg.AllowPrintToFile = true;

            // We don't support page selection.
            pdlg.AllowSomePages = false;

            // If the result is OK then print the document.
            if (pdlg.ShowDialog() == DialogResult.OK)
            {
                // Init from user selections.
                _curPageNumber = 0;

                if (pdlg.PrinterSettings.PrintRange == PrintRange.Selection && _editor.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected)
                {
                    _curLineNumber = _editor.ActiveTextAreaControl.TextArea.SelectionManager.SelectionCollection[0].StartPosition.Line;
                    _endLineNumber = _editor.ActiveTextAreaControl.TextArea.SelectionManager.SelectionCollection[0].EndPosition.Line;
                }
                else // No selection, print all.
                {
                    _curLineNumber = 0;
                    _endLineNumber = _editor.Document.TotalNumberOfLines;
                }

                pdoc.BeginPrint += new PrintEventHandler(BeginPrint);
                pdoc.PrintPage += new PrintPageEventHandler(PrintPage);
                pdoc.Print();
            }
        }

        /////
        void BeginPrint(object sender, PrintEventArgs ev) // TODO2 remove the printing support in TextEditorControl
        {
            _curLineNumber = 0;
            _printingStringFormat = (StringFormat)System.Drawing.StringFormat.GenericTypographic.Clone();
            _printingStringFormat.Trimming = StringTrimming.None;
            _printingStringFormat.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

            // 100 should be enough for everyone ...err ?
            float[] tabStops = new float[100];
            for (int i = 0; i < tabStops.Length; ++i)
            {
                tabStops[i] = _editor.TabIndent * _editor.ActiveTextAreaControl.TextArea.TextView.WideSpaceWidth;
            }

            _printingStringFormat.SetTabStops(0, tabStops);
        }

        /////
        void PrintPage(object sender, PrintPageEventArgs ev)
        {
            Graphics g = ev.Graphics;
            float yPos = ev.MarginBounds.Top;

            _curPageNumber++;

            // Header first.
            string hdr = string.Format("{0}   page {2}   {1} ", _editor.FileName, DateTime.Now.ToString(), _curPageNumber, _totalPages);
            g.DrawString(hdr, _editor.Font, BrushRegistry.GetBrush(Color.Black), ev.MarginBounds.X, yPos);
            yPos += 20.0f;

            while (_curLineNumber < _endLineNumber)
            {
                LineSegment curLine = _editor.Document.GetLineSegment(_curLineNumber);
                if (curLine.Words != null)
                {
                    float drawingHeight = MeasurePrintingHeight(g, curLine, ev.MarginBounds.Width);
                    if (drawingHeight + yPos > ev.MarginBounds.Bottom)
                    {
                        break; // done this page
                    }

                    DrawLine(g, curLine, yPos, ev.MarginBounds);
                    yPos += drawingHeight;
                }

                _curLineNumber++;
            }

            // If more lines exist, print another page.
            ev.HasMorePages = _curLineNumber < _endLineNumber;
        }

        /////
        void DrawLine(Graphics g, LineSegment line, float yPos, RectangleF margin)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            float xPos = 0.0f;

            FontContainer fontContainer = _editor.TextEditorProperties.FontContainer;
            foreach (TextWord word in line.Words)
            {
                switch (word.Type)
                {
                    case TextWordType.Space:
                    case TextWordType.Tab:
                        throw new NotImplementedException();

                    case TextWordType.Word:
                        Font f = word.GetFont(fontContainer);

                        // Check for run off side of page.
                        // Note that MeasureString does not count the same way as DrawString but should be close enough.
                        float reqWidth = _xSpace * word.Word.Length;
                        if (xPos + reqWidth > margin.Width)
                        {
                            Advance(ref xPos, ref yPos, margin.Width, reqWidth, _ySpace);
                        }

                        // Print word one letter at a time to control spacing.
                        for (int i = 0; i < word.Word.Length; i++)
                        {
                            // If bg is not "white" draw a colored rectangle first.
                            if (word.SyntaxColor.BackgroundColor != _hcDef.BackgroundColor)
                            {
                                g.FillRectangle(BrushRegistry.GetBrush(word.SyntaxColor.BackgroundColor),
                                        xPos + margin.X + 2.0f, yPos, _xSpace, f.Height); // have to add 2.0 to start to make bg work?!?
                            }

                            g.DrawString(new string(word.Word[i], 1), f, BrushRegistry.GetBrush(word.SyntaxColor.Color), xPos + margin.X, yPos);

                            xPos += _xSpace;
                        }
                        break;
                }
            }
        }

        /////
        void Advance(ref float x, ref float y, float maxWidth, float size, float fontHeight)
        {
            if (x + size < maxWidth)
            {
                x += size;
            }
            else
            {
                x = _xSpace * 8; // fake tab
                y += fontHeight;
            }
        }

        /////
        float MeasurePrintingHeight(Graphics g, LineSegment line, float maxWidth)
        {
            float xPos = 0;
            float yPos = 0;
            float fontHeight = _editor.Font.GetHeight(g);
            FontContainer fontContainer = _editor.TextEditorProperties.FontContainer;
            foreach (TextWord word in line.Words)
            {
                switch (word.Type)
                {
                    case TextWordType.Space:
                    case TextWordType.Tab:
                        throw new NotImplementedException();

                    case TextWordType.Word:
                        SizeF drawingSize = g.MeasureString(word.Word, word.GetFont(fontContainer), new SizeF(maxWidth, fontHeight * 100), _printingStringFormat);
                        Advance(ref xPos, ref yPos, maxWidth, drawingSize.Width, _ySpace);
                        break;
                }
            }

            return yPos + fontHeight;
        }
        #endregion
    }
}
