// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;


namespace ICSharpCode.TextEditor.Actions
{
    /// <summary>
    /// To define a new key for the textarea, you must write a class which derives from this.
    /// </summary>
    public abstract class EditAction
    {
        public virtual void Execute(TextArea textArea)
        {
            throw new NotImplementedException();
        }

        public bool UserAction { get; set; }
    }
}
