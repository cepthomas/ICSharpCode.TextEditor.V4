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
        #region Fields
        protected Panel _textAreaPanel = new Panel();

        Splitter _textAreaSplitter = null;

        TextAreaControl _primaryTextAreaControl = null;

        TextAreaControl _secondaryTextAreaControl = null;

        string _currentFileName = null;

        int _updateLevel = 0;

        Document.Document _document;

        /// <summary>This hashtable contains all editor keys, where the key is the key combination and the value the action. </summary>
        protected Dictionary<Keys, IEditAction> _editActions = new Dictionary<Keys, IEditAction>();

        bool _dirty = false;

        Encoding _encoding;
        #endregion

        #region Properties - internal
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableUndo { get { return Document.UndoStack.CanUndo; } }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableRedo { get { return Document.UndoStack.CanRedo; } }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get { return Document.TextContent; }
            set { Document.TextContent = value; }
        }

        /// <value>
        /// If set to true the contents can't be altered.
        /// </value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsInUpdate
        {
            get { return _updateLevel > 0; } }

        /// <value>
        /// supposedly this is the way to do it according to .NET docs,
        /// as opposed to setting the size in the constructor
        /// </value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override Size DefaultSize
        {
            get { return new Size(100, 100); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextAreaControl ActiveTextAreaControl { get; private set; } = null;
        #endregion

        #region Document Properties TODOsettings initialize from TEP when creating control - needs UI for changes
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowSpaces { get; set; } = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowTabs { get; set; } = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowEOLMarker { get; set; } = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ConvertTabsToSpaces { get; set; } = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TabIndent { get; set; } = 4;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int IndentationSize { get; set; } = 4;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IndentStyle IndentStyle { get; set; } = IndentStyle.Smart;
        #endregion

        #region Events
        public event EventHandler FileNameChanged;

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

        public event EventHandler ActiveTextAreaControlChanged;
        #endregion

        #region Lifecycle
        public TextEditorControl()
        {
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
            Document.UpdateCommited += CommitUpdateRequested;
            OptionsChanged();
        }
        #endregion

        #region Public functions
        /// <summary>Set the dirty flag.</summary>
        /// <param name="value">New value.</param>
        /// <returns>True if changed.</returns>
        public bool SetDirty(bool value)
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

                    // TODOsyntax this doesn't belong here. I did it because it needed a file/home.
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
                Document.TextContent = Util.FileReader.ReadFileContent(stream, encoding);
                Encoding = encoding;
            }
            else
            {
                using (StreamReader reader = new StreamReader(fileName, Encoding))
                {
                    string s = reader.ReadToEnd();
                    Document.TextContent = s; // TODO2*** 50Mb takes 6 seconds...
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
        #endregion

        #region Private functions
        protected void SetActiveTextAreaControl(TextAreaControl value)
        {
            if (ActiveTextAreaControl != value)
            {
                ActiveTextAreaControl = value;

                ActiveTextAreaControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
        {
        }

        void OnDocumentChanged(object sender, EventArgs e)
        {
            OnTextChanged(e);
        }

        //static Font ParseFont(string font)
        //{
        //    string[] descr = font.Split(new char[] { ',', '=' });
        //    return new Font(descr[1], Single.Parse(descr[3]));
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document.UndoStack.ClearAll();
                Document.UpdateCommited -= CommitUpdateRequested;

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

        #endregion
    }
}
