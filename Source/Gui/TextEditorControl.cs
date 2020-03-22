// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Text;
using System.IO;
using System.Diagnostics;

using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Actions;
//using ICSharpCode.TextEditor.Src.Util;
using ICSharpCode.TextEditor.Common;


namespace ICSharpCode.TextEditor
{
    /// <summary>This class is used for a basic text area control</summary>
    public class TextEditorControl : UserControl
    {
        public event EventHandler FileNameChanged;

        protected Panel _textAreaPanel = new Panel();

        Splitter _textAreaSplitter = null;

        TextAreaControl _primaryTextAreaControl = null;

        TextAreaControl _secondaryTextAreaControl = null;

        string _currentFileName = null;

        int _updateLevel = 0;

        Document.Document _document;

        /// <summary>
        /// This hashtable contains all editor keys, where
        /// the key is the key combination and the value the
        /// action.
        /// </summary>
        protected Dictionary<Keys, IEditAction> _editActions = new Dictionary<Keys, IEditAction>();

        bool _dirty = false;

        Encoding _encoding;


        /// <value>
        /// Current file's character encoding
        /// </value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Encoding Encoding { get { return _encoding ?? Shared.TEP.Encoding; } set { _encoding = value; } }

        /// <value>
        /// The current file name
        /// </value>
        [Browsable(false)]
        [ReadOnly(true)]
        public string FileName
        {
            get
            {
                return _currentFileName;
            }
            set
            {
                if (_currentFileName != value)
                {
                    _currentFileName = value;
                    OnFileNameChanged(EventArgs.Empty);
                }
            }
        }

        /// <value>
        /// The current document
        /// </value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Document.Document Document
        {
            get
            {
                return _document;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (_document != null)
                {
                    _document.DocumentChanged -= OnDocumentChanged;
                }
                _document = value;
                _document.UndoStack.TextEditorControl = this;
                _document.DocumentChanged += OnDocumentChanged;
            }
        }

        [Browsable(false)]
        public bool EnableUndo { get { return Document.UndoStack.CanUndo; } }

        [Browsable(false)]
        public bool EnableRedo { get { return Document.UndoStack.CanRedo; } }

        [EditorBrowsable(EditorBrowsableState.Always), Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(System.Drawing.Design.UITypeEditor))]
        public override string Text
        {
            get
            {
                return Document.TextContent;
            }
            set
            {
                Document.TextContent = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Always), Browsable(true)]
        public new event EventHandler TextChanged
        {
            add
            {
                base.TextChanged += value;
            }
            remove
            {
                base.TextChanged -= value;
            }
        }

        /// <value>
        /// If set to true the contents can't be altered.
        /// </value>
        [Browsable(false)]
        public bool IsReadOnly
        {
            get
            {
                return Document.ReadOnly;
            }
            set
            {
                Document.ReadOnly = value;
            }
        }

        /// <value>
        /// true, if the textarea is updating it's status, while
        /// it updates it status no redraw operation occurs.
        /// </value>
        [Browsable(false)]
        public bool IsInUpdate
        {
            get
            {
                return _updateLevel > 0;
            }
        }

        /// <value>
        /// supposedly this is the way to do it according to .NET docs,
        /// as opposed to setting the size in the constructor
        /// </value>
        protected override Size DefaultSize
        {
            get
            {
                return new Size(100, 100);
            }
        }

        #region Document Properties TODO1 these
        /// <value>
        /// If true spaces are shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true spaces are shown in the textarea")]
        public bool ShowSpaces
        {
            get
            {
                return Shared.TEP.ShowSpaces;
            }
            set
            {
                Shared.TEP.ShowSpaces = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// Specifies the quality of text rendering (whether to use hinting and/or anti-aliasing).
        /// </value>
        [Category("Appearance")]
        [DefaultValue(TextRenderingHint.SystemDefault)]
        [Description("Specifies the quality of text rendering (whether to use hinting and/or anti-aliasing).")]
        public TextRenderingHint TextRenderingHint
        {
            get
            {
                return Shared.TEP.TextRenderingHint;
            }
            set
            {
                Shared.TEP.TextRenderingHint = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true tabs are shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true tabs are shown in the textarea")]
        public bool ShowTabs
        {
            get
            {
                return Shared.TEP.ShowTabs;
            }
            set
            {
                Shared.TEP.ShowTabs = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true EOL markers are shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true EOL markers are shown in the textarea")]
        public bool ShowEOLMarkers
        {
            get
            {
                return Shared.TEP.ShowEOLMarker;
            }
            set
            {
                Shared.TEP.ShowEOLMarker = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true the horizontal ruler is shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true the horizontal ruler is shown in the textarea")]
        public bool ShowHRuler
        {
            get
            {
                return Shared.TEP.ShowHorizontalRuler;
            }
            set
            {
                Shared.TEP.ShowHorizontalRuler = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true the vertical ruler is shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("If true the vertical ruler is shown in the textarea")]
        public bool ShowVRuler
        {
            get
            {
                return Shared.TEP.ShowVerticalRuler;
            }
            set
            {
                Shared.TEP.ShowVerticalRuler = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// The row in which the vertical ruler is displayed
        /// </value>
        [Category("Appearance")]
        [DefaultValue(80)]
        [Description("The row in which the vertical ruler is displayed")]
        public int VRulerRow
        {
            get
            {
                return Shared.TEP.VerticalRulerRow;
            }
            set
            {
                Shared.TEP.VerticalRulerRow = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true line numbers are shown in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("If true line numbers are shown in the textarea")]
        public bool ShowLineNumbers
        {
            get
            {
                return Shared.TEP.ShowLineNumbers;
            }
            set
            {
                Shared.TEP.ShowLineNumbers = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true invalid lines are marked in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true invalid lines are marked in the textarea")]
        public bool ShowInvalidLines
        {
            get
            {
                return Shared.TEP.ShowInvalidLines;
            }
            set
            {
                Shared.TEP.ShowInvalidLines = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// If true folding is enabled in the textarea
        /// </value>
        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("If true folding is enabled in the textarea")]
        public bool EnableFolding
        {
            get
            {
                return Shared.TEP.EnableFolding;
            }
            set
            {
                Shared.TEP.EnableFolding = value;
                OptionsChanged();
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("If true matching brackets are highlighted")]
        public bool ShowMatchingBracket
        {
            get
            {
                return Shared.TEP.ShowMatchingBracket;
            }
            set
            {
                Shared.TEP.ShowMatchingBracket = value;
                OptionsChanged();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [Description("If true the icon bar is displayed")]
        public bool IsIconBarVisible
        {
            get
            {
                return Shared.TEP.IsIconBarVisible;
            }
            set
            {
                Shared.TEP.IsIconBarVisible = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// The width in spaces of a tab character
        /// </value>
        [Category("Appearance")]
        [DefaultValue(4)]
        [Description("The width in spaces of a tab character")]
        public int TabIndent
        {
            get
            {
                return Shared.TEP.TabIndent;
            }
            set
            {
                Shared.TEP.TabIndent = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// The line viewer style
        /// </value>
        [Category("Appearance")]
        [DefaultValue(LineViewerStyle.None)]
        [Description("The line viewer style")]
        public LineViewerStyle LineViewerStyle
        {
            get
            {
                return Shared.TEP.LineViewerStyle;
            }
            set
            {
                Shared.TEP.LineViewerStyle = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// The indent style
        /// </value>
        [Category("Behavior")]
        [DefaultValue(IndentStyle.Smart)]
        [Description("The indent style")]
        public IndentStyle IndentStyle
        {
            get
            {
                return Shared.TEP.IndentStyle;
            }
            set
            {
                Shared.TEP.IndentStyle = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// if true spaces are converted to tabs
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Converts tabs to spaces while typing")]
        public bool ConvertTabsToSpaces
        {
            get
            {
                return Shared.TEP.ConvertTabsToSpaces;
            }
            set
            {
                Shared.TEP.ConvertTabsToSpaces = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// if true spaces are converted to tabs
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Hide the mouse cursor while typing")]
        public bool HideMouseCursor
        {
            get
            {
                return Shared.TEP.HideMouseCursor;
            }
            set
            {
                Shared.TEP.HideMouseCursor = value;
                OptionsChanged();
            }
        }

        /// <value>
        /// if true spaces are converted to tabs
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Allows the caret to be placed beyond the end of line")]
        public bool AllowCaretBeyondEOL
        {
            get
            {
                return Shared.TEP.AllowCaretBeyondEOL;
            }
            set
            {
                Shared.TEP.AllowCaretBeyondEOL = value;
                OptionsChanged();
            }
        }
        /// <value>
        /// if true spaces are converted to tabs
        /// </value>
        [Category("Behavior")]
        [DefaultValue(BracketMatchingStyle.After)]
        [Description("Specifies if the bracket matching should match the bracket before or after the caret.")]
        public BracketMatchingStyle BracketMatchingStyle
        {
            get
            {
                return Shared.TEP.BracketMatchingStyle;
            }
            set
            {
                Shared.TEP.BracketMatchingStyle = value;
                OptionsChanged();
            }
        }

        #endregion

        public TextAreaControl ActiveTextAreaControl { get; private set; } = null;

        protected void SetActiveTextAreaControl(TextAreaControl value)
        {
            if (ActiveTextAreaControl != value)
            {
                ActiveTextAreaControl = value;

                ActiveTextAreaControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ActiveTextAreaControlChanged;

        public TextEditorControl()
        {
            //base:
            //Font = new Font("Consolas", 10);
            GenerateDefaultActions();

            SetStyle(ControlStyles.ContainerControl, true);

            _textAreaPanel.Dock = DockStyle.Fill;

            //Document = (new DocumentFactory()).CreateDocument();
            //Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy();
            Document = new Document.Document();

            _primaryTextAreaControl = new TextAreaControl(this);
            ActiveTextAreaControl = _primaryTextAreaControl;

            _primaryTextAreaControl.TextArea.GotFocus += delegate
            {
                SetActiveTextAreaControl(_primaryTextAreaControl);
            };

            _primaryTextAreaControl.Dock = DockStyle.Fill;
            _textAreaPanel.Controls.Add(_primaryTextAreaControl);
            InitializeTextAreaControl(_primaryTextAreaControl);
            Controls.Add(_textAreaPanel);
            ResizeRedraw = true;
            Document.UpdateCommited += new EventHandler(CommitUpdateRequested);
            OptionsChanged();
        }

        protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
        {
        }

        void OnDocumentChanged(object sender, EventArgs e)
        {
            OnTextChanged(e);
        }

        /// <summary>Set the dirty flag.</summary>
        /// <param name="value">New value.</param>
        /// <returns>True if changed.</returns>
        public bool SetDirty(bool value) //TODO1 make this a property.
        {
            bool changed = value != _dirty;
            _dirty = value;
            return changed;
        }

        /// <summary>Get the dirty flag.</summary>
        /// <returns></returns>
        public bool GetDirty()
        {
            return _dirty;
        }

        //static Font ParseFont(string font)
        //{
        //    string[] descr = font.Split(new char[] { ',', '=' });
        //    return new Font(descr[1], Single.Parse(descr[3]));
        //}


        public bool IsEditAction(Keys keyData)
        {
            return _editActions.ContainsKey(keyData);
        }

        internal IEditAction GetEditAction(Keys keyData)
        {
            if (!IsEditAction(keyData))
            {
                return null;
            }
            return (IEditAction)_editActions[keyData];
        }

        void GenerateDefaultActions() //TODO1 get from file/config
        {
            _editActions[Keys.Left] = new CaretLeft();
            _editActions[Keys.Left | Keys.Shift] = new ShiftCaretLeft();
            _editActions[Keys.Left | Keys.Control] = new WordLeft();
            _editActions[Keys.Left | Keys.Control | Keys.Shift] = new ShiftWordLeft();
            _editActions[Keys.Right] = new CaretRight();
            _editActions[Keys.Right | Keys.Shift] = new ShiftCaretRight();
            _editActions[Keys.Right | Keys.Control] = new WordRight();
            _editActions[Keys.Right | Keys.Control | Keys.Shift] = new ShiftWordRight();
            _editActions[Keys.Up] = new CaretUp();
            _editActions[Keys.Up | Keys.Shift] = new ShiftCaretUp();
            _editActions[Keys.Up | Keys.Control] = new ScrollLineUp();
            _editActions[Keys.Down] = new CaretDown();
            _editActions[Keys.Down | Keys.Shift] = new ShiftCaretDown();
            _editActions[Keys.Down | Keys.Control] = new ScrollLineDown();

            _editActions[Keys.Insert] = new ToggleEditMode();
            _editActions[Keys.Insert | Keys.Control] = new Copy();
            _editActions[Keys.Insert | Keys.Shift] = new Paste();
            _editActions[Keys.Delete] = new Delete();
            _editActions[Keys.Delete | Keys.Shift] = new Cut();
            _editActions[Keys.Home] = new Home();
            _editActions[Keys.Home | Keys.Shift] = new ShiftHome();
            _editActions[Keys.Home | Keys.Control] = new MoveToStart();
            _editActions[Keys.Home | Keys.Control | Keys.Shift] = new ShiftMoveToStart();
            _editActions[Keys.End] = new End();
            _editActions[Keys.End | Keys.Shift] = new ShiftEnd();
            _editActions[Keys.End | Keys.Control] = new MoveToEnd();
            _editActions[Keys.End | Keys.Control | Keys.Shift] = new ShiftMoveToEnd();
            _editActions[Keys.PageUp] = new MovePageUp();
            _editActions[Keys.PageUp | Keys.Shift] = new ShiftMovePageUp();
            _editActions[Keys.PageDown] = new MovePageDown();
            _editActions[Keys.PageDown | Keys.Shift] = new ShiftMovePageDown();

            _editActions[Keys.Return] = new Return();
            _editActions[Keys.Tab] = new Tab();
            _editActions[Keys.Tab | Keys.Shift] = new ShiftTab();
            _editActions[Keys.Back] = new Backspace();
            _editActions[Keys.Back | Keys.Shift] = new Backspace();

            _editActions[Keys.X | Keys.Control] = new Cut();
            _editActions[Keys.C | Keys.Control] = new Copy();
            _editActions[Keys.V | Keys.Control] = new Paste();

            _editActions[Keys.A | Keys.Control] = new SelectWholeDocument();
            _editActions[Keys.Escape] = new ClearSelection();

            _editActions[Keys.Divide | Keys.Control] = new ToggleComment();
            _editActions[Keys.OemQuestion | Keys.Control] = new ToggleComment();

            _editActions[Keys.Back | Keys.Alt] = new Actions.Undo();
            _editActions[Keys.Z | Keys.Control] = new Actions.Undo();
            _editActions[Keys.Y | Keys.Control] = new Redo();

            _editActions[Keys.Delete | Keys.Control] = new DeleteWord();
            _editActions[Keys.Back | Keys.Control] = new WordBackspace();
            _editActions[Keys.D | Keys.Control] = new DeleteLine();
            _editActions[Keys.D | Keys.Shift | Keys.Control] = new DeleteToLineEnd();

            _editActions[Keys.B | Keys.Control] = new GotoMatchingBrace();
        }

        /// <remarks>
        /// Call this method before a long update operation this
        /// 'locks' the text area so that no screen update occurs.
        /// </remarks>
        public void BeginUpdate()
        {
            ++_updateLevel;
        }

        /// <remarks>
        /// Loads a file given by fileName
        /// </remarks>
        /// <param name="fileName">The name of the file to open</param>
        /// <param name="autoLoadHighlighting">Automatically load the highlighting for the file</param>
        /// <param name="autodetectEncoding">Automatically detect file encoding and set Encoding property to the detected encoding.</param>
        public void LoadFile(string fileName, bool autoLoadHighlighting, bool autodetectEncoding)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                LoadFile(fileName, fs, autoLoadHighlighting, autodetectEncoding);
            }
        }

        /// <remarks>
        /// Loads a file from the specified stream.
        /// </remarks>
        /// <param name="fileName">The name of the file to open. Used to find the correct highlighting strategy
        /// if autoLoadHighlighting is active, and sets the filename property to this value.</param>
        /// <param name="stream">The stream to actually load the file content from.</param>
        /// <param name="autoLoadHighlighting">Automatically load the highlighting for the file</param>
        /// <param name="autodetectEncoding">Automatically detect file encoding and set Encoding property to the detected encoding.</param>
        public void LoadFile(string fileName, Stream stream, bool autoLoadHighlighting, bool autodetectEncoding)
        {
            BeginUpdate();
            _document.TextContent = string.Empty;
            _document.UndoStack.ClearAll();
            _document.BookmarkManager.Clear();

            if (autoLoadHighlighting)
            {
                try
                {
                    _document.HighlightingStrategy = HighlightingManager.Instance.FindHighlighterForFile(fileName);

                    // TODO1 this doesn't belong here. I did it.
                    IFoldingStrategy fs = null;

                    if (_document.HighlightingStrategy != null)
                    {
                        switch (_document.HighlightingStrategy.Folding)
                        {
                            case "Code":
                                fs = new CodeFoldingStrategy();
                                break;
                            case "CSharp":
                                fs = new CSharpFoldingStrategy();
                                break;
                            case "Markdown":
                                fs = new MarkdownFoldingStrategy();
                                break;
                            case "Xml":
                                fs = new XmlFoldingStrategy();
                                break;
                        }
                    }
                    _document.FoldingManager.FoldingStrategy = fs;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (autodetectEncoding)
            {
                Encoding encoding = Encoding;
                Document.TextContent = Util.FileReader.ReadFileContent(stream, ref encoding);
                Encoding = encoding;
            }
            else
            {
                using (StreamReader reader = new StreamReader(fileName, Encoding))
                {
                    string s = reader.ReadToEnd();
                    Document.TextContent = s; // takes 6 seconds...
                }
            }

            FileName = fileName;
            Document.UpdateQueue.Clear();
            EndUpdate();

            OptionsChanged();
            Refresh();
        }

        /// <summary>
        /// Gets if the document can be saved with the current encoding without losing data.
        /// </summary>
        public bool CanSaveWithCurrentEncoding()
        {
            if (_encoding == null || Util.FileReader.IsUnicode(_encoding))
                return true;

            // not a unicode codepage
            string text = _document.TextContent;
            return _encoding.GetString(_encoding.GetBytes(text)) == text;
        }

        /// <remarks>
        /// Saves the text editor content into the file.
        /// </remarks>
        public void SaveFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                SaveFile(fs);
            }
            FileName = fileName;
        }

        /// <remarks>
        /// Saves the text editor content into the specified stream.
        /// Does not close the stream.
        /// </remarks>
        public void SaveFile(Stream stream)
        {
            StreamWriter streamWriter = new StreamWriter(stream, this.Encoding ?? Encoding.UTF8);

            // save line per line to apply the LineTerminator to all lines
            // (otherwise we might save files with mixed-up line endings)
            foreach (LineSegment line in Document.LineSegmentCollection)
            {
                streamWriter.Write(Document.GetText(line.Offset, line.Length));
                if (line.DelimiterLength > 0)
                {
                    char charAfterLine = Document.GetCharAt(line.Offset + line.Length);
                    if (charAfterLine != '\n' && charAfterLine != '\r')
                        throw new InvalidOperationException("The document cannot be saved because it is corrupted.");

                    // only save line terminator if the line has one
                    streamWriter.Write(Shared.TEP.LineTerminator);
                }
            }
            streamWriter.Flush();
        }

        //// used in insight window
        //public virtual string GetRangeDescription(int selectedItem, int itemCount)
        //{
        //    StringBuilder sb = new StringBuilder(selectedItem.ToString());
        //    sb.Append(" from ");
        //    sb.Append(itemCount.ToString());
        //    return sb.ToString();
        //}

        /// <remarks>
        /// Overwritten refresh method that does nothing if the control is in
        /// an update cycle.
        /// </remarks>
        public override void Refresh()
        {
            if (IsInUpdate)
            {
                return;
            }
            base.Refresh();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document.UndoStack.ClearAll();
                Document.UpdateCommited -= new EventHandler(CommitUpdateRequested);

                if (_textAreaPanel != null)
                {
                    if (_secondaryTextAreaControl != null)
                    {
                        _secondaryTextAreaControl.Dispose();
                        _textAreaSplitter.Dispose();
                        _secondaryTextAreaControl = null;
                        _textAreaSplitter = null;
                    }

                    if (_primaryTextAreaControl != null)
                    {
                        _primaryTextAreaControl.Dispose();
                    }

                    _textAreaPanel.Dispose();
                    _textAreaPanel = null;
                }

                _document.HighlightingStrategy = null;
                _document.UndoStack.TextEditorControl = null;
            }

            base.Dispose(disposing);
        }

        protected virtual void OnFileNameChanged(EventArgs e)
        {
            FileNameChanged?.Invoke(this, e);
        }

        public void OptionsChanged()
        {
            _primaryTextAreaControl.OptionsChanged();
            if (_secondaryTextAreaControl != null)
            {
                _secondaryTextAreaControl.OptionsChanged();
            }
        }

        public void Split()
        {
            if (_secondaryTextAreaControl == null)
            {
                _secondaryTextAreaControl = new TextAreaControl(this);
                _secondaryTextAreaControl.Dock = DockStyle.Bottom;
                _secondaryTextAreaControl.Height = Height / 2;

                _secondaryTextAreaControl.TextArea.GotFocus += delegate
                {
                    SetActiveTextAreaControl(_secondaryTextAreaControl);
                };

                _textAreaSplitter = new Splitter();
                _textAreaSplitter.BorderStyle = BorderStyle.FixedSingle;
                _textAreaSplitter.Height = 8;
                _textAreaSplitter.Dock = DockStyle.Bottom;
                _textAreaPanel.Controls.Add(_textAreaSplitter);
                _textAreaPanel.Controls.Add(_secondaryTextAreaControl);
                InitializeTextAreaControl(_secondaryTextAreaControl);
                _secondaryTextAreaControl.OptionsChanged();
            }
            else
            {
                SetActiveTextAreaControl(_primaryTextAreaControl);

                _textAreaPanel.Controls.Remove(_secondaryTextAreaControl);
                _textAreaPanel.Controls.Remove(_textAreaSplitter);

                _secondaryTextAreaControl.Dispose();
                _textAreaSplitter.Dispose();
                _secondaryTextAreaControl = null;
                _textAreaSplitter = null;
            }
        }

        public void Undo()
        {
            if (Document.ReadOnly)
            {
                return;
            }

            if (Document.UndoStack.CanUndo)
            {
                BeginUpdate();
                Document.UndoStack.Undo();

                Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
                this._primaryTextAreaControl.TextArea.UpdateMatchingBracket();

                if (_secondaryTextAreaControl != null)
                {
                    this._secondaryTextAreaControl.TextArea.UpdateMatchingBracket();
                }
                EndUpdate();
            }
        }

        public void Redo()
        {
            if (Document.ReadOnly)
            {
                return;
            }

            if (Document.UndoStack.CanRedo)
            {
                BeginUpdate();
                Document.UndoStack.Redo();

                Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
                _primaryTextAreaControl.TextArea.UpdateMatchingBracket();
                if (_secondaryTextAreaControl != null)
                {
                    _secondaryTextAreaControl.TextArea.UpdateMatchingBracket();
                }

                EndUpdate();
            }
        }

        /// <remarks>
        /// Call this method to 'unlock' the text area. After this call
        /// screen update can occur. But no automatical refresh occurs you
        /// have to commit the updates in the queue.
        /// </remarks>
        public void EndUpdate()//override
        {
            //Debug.Assert(updateLevel > 0);
            _updateLevel = Math.Max(0, _updateLevel - 1);

            Document.CommitUpdate();
            if (!IsInUpdate)
            {
                ActiveTextAreaControl.Caret.OnEndUpdate();
            }
        }

        void CommitUpdateRequested(object sender, EventArgs e)
        {
            if (IsInUpdate)
            {
                return;
            }

            foreach (TextAreaUpdate update in Document.UpdateQueue)
            {
                switch (update.TextAreaUpdateType)
                {
                    case TextAreaUpdateType.PositionToEnd:
                        this._primaryTextAreaControl.TextArea.UpdateToEnd(update.Position.Y);
                        if (this._secondaryTextAreaControl != null)
                        {
                            this._secondaryTextAreaControl.TextArea.UpdateToEnd(update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.PositionToLineEnd:
                    case TextAreaUpdateType.SingleLine:
                        this._primaryTextAreaControl.TextArea.UpdateLine(update.Position.Y);
                        if (this._secondaryTextAreaControl != null)
                        {
                            this._secondaryTextAreaControl.TextArea.UpdateLine(update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.SinglePosition:
                        this._primaryTextAreaControl.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
                        if (this._secondaryTextAreaControl != null)
                        {
                            this._secondaryTextAreaControl.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
                        }
                        break;

                    case TextAreaUpdateType.LinesBetween:
                        this._primaryTextAreaControl.TextArea.UpdateLines(update.Position.X, update.Position.Y);
                        if (this._secondaryTextAreaControl != null)
                        {
                            this._secondaryTextAreaControl.TextArea.UpdateLines(update.Position.X, update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.WholeTextArea:
                        this._primaryTextAreaControl.TextArea.Invalidate();
                        if (this._secondaryTextAreaControl != null)
                        {
                            this._secondaryTextAreaControl.TextArea.Invalidate();
                        }
                        break;
                }
            }

            Document.UpdateQueue.Clear();
        }
    }
}
