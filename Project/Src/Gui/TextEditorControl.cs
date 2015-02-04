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

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
    /// <summary>This class is used for a basic text area control</summary>
    [ToolboxBitmap("ICSharpCode.TextEditor.Resources.TextEditorControl.bmp")]
    [ToolboxItem(true)]
    public class TextEditorControl : TextEditorControlBase // TODO3 combine these?
    {
        protected Panel textAreaPanel     = new Panel();
        TextAreaControl primaryTextArea  = null;
        Splitter textAreaSplitter  = null;
        TextAreaControl secondaryTextArea = null;
        TextAreaControl activeTextAreaControl = null;

        bool _dirty = false;
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

        public override TextAreaControl ActiveTextAreaControl
        {
            get { return activeTextAreaControl; }
        }

        protected void SetActiveTextAreaControl(TextAreaControl value)
        {
            if (activeTextAreaControl != value)
            {
                activeTextAreaControl = value;

                if (ActiveTextAreaControlChanged != null)
                {
                    ActiveTextAreaControlChanged(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ActiveTextAreaControlChanged;

        public TextEditorControl()
        {
            SetStyle(ControlStyles.ContainerControl, true);

            textAreaPanel.Dock = DockStyle.Fill;

            Document = (new DocumentFactory()).CreateDocument();
            Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy();

            primaryTextArea  = new TextAreaControl(this);
            activeTextAreaControl = primaryTextArea;

            primaryTextArea.TextArea.GotFocus += delegate
            {
                SetActiveTextAreaControl(primaryTextArea);
            };

            primaryTextArea.Dock = DockStyle.Fill;
            textAreaPanel.Controls.Add(primaryTextArea);
            InitializeTextAreaControl(primaryTextArea);
            Controls.Add(textAreaPanel);
            ResizeRedraw = true;
            Document.UpdateCommited += new EventHandler(CommitUpdateRequested);
            OptionsChanged();
        }

        protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
        {
        }

        public override void OptionsChanged()
        {
            primaryTextArea.OptionsChanged();
            if (secondaryTextArea != null)
            {
                secondaryTextArea.OptionsChanged();
            }
        }

        public void Split()
        {
            if (secondaryTextArea == null)
            {
                secondaryTextArea = new TextAreaControl(this);
                secondaryTextArea.Dock = DockStyle.Bottom;
                secondaryTextArea.Height = Height / 2;

                secondaryTextArea.TextArea.GotFocus += delegate
                {
                    SetActiveTextAreaControl(secondaryTextArea);
                };

                textAreaSplitter =  new Splitter();
                textAreaSplitter.BorderStyle = BorderStyle.FixedSingle ;
                textAreaSplitter.Height = 8;
                textAreaSplitter.Dock = DockStyle.Bottom;
                textAreaPanel.Controls.Add(textAreaSplitter);
                textAreaPanel.Controls.Add(secondaryTextArea);
                InitializeTextAreaControl(secondaryTextArea);
                secondaryTextArea.OptionsChanged();
            }
            else
            {
                SetActiveTextAreaControl(primaryTextArea);

                textAreaPanel.Controls.Remove(secondaryTextArea);
                textAreaPanel.Controls.Remove(textAreaSplitter);

                secondaryTextArea.Dispose();
                textAreaSplitter.Dispose();
                secondaryTextArea = null;
                textAreaSplitter  = null;
            }
        }

        [Browsable(false)]
        public bool EnableUndo
        {
            get { return Document.UndoStack.CanUndo; }
        }

        [Browsable(false)]
        public bool EnableRedo
        {
            get { return Document.UndoStack.CanRedo; }
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
                this.primaryTextArea.TextArea.UpdateMatchingBracket();

                if (secondaryTextArea != null)
                {
                    this.secondaryTextArea.TextArea.UpdateMatchingBracket();
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
                this.primaryTextArea.TextArea.UpdateMatchingBracket();
                if (secondaryTextArea != null)
                {
                    this.secondaryTextArea.TextArea.UpdateMatchingBracket();
                }

                EndUpdate();
            }
        }

        public virtual void SetHighlighting(string name)
        {
            Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(name);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Document.UndoStack.ClearAll();
                Document.UpdateCommited -= new EventHandler(CommitUpdateRequested);

                if (textAreaPanel != null)
                {
                    if (secondaryTextArea != null)
                    {
                        secondaryTextArea.Dispose();
                        textAreaSplitter.Dispose();
                        secondaryTextArea = null;
                        textAreaSplitter  = null;
                    }

                    if (primaryTextArea != null)
                    {
                        primaryTextArea.Dispose();
                    }

                    textAreaPanel.Dispose();
                    textAreaPanel = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Update Methods
        public override void EndUpdate()
        {
            base.EndUpdate();
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
                        this.primaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
                        if (this.secondaryTextArea != null)
                        {
                            this.secondaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.PositionToLineEnd:
                    case TextAreaUpdateType.SingleLine:
                        this.primaryTextArea.TextArea.UpdateLine(update.Position.Y);
                        if (this.secondaryTextArea != null)
                        {
                            this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.SinglePosition:
                        this.primaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
                        if (this.secondaryTextArea != null)
                        {
                            this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
                        }
                        break;

                    case TextAreaUpdateType.LinesBetween:
                        this.primaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
                        if (this.secondaryTextArea != null)
                        {
                            this.secondaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
                        }
                        break;

                    case TextAreaUpdateType.WholeTextArea:
                        this.primaryTextArea.TextArea.Invalidate();
                        if (this.secondaryTextArea != null)
                        {
                            this.secondaryTextArea.TextArea.Invalidate();
                        }
                        break;
                }
            }

            Document.UpdateQueue.Clear();
        }
        #endregion
    }
}
