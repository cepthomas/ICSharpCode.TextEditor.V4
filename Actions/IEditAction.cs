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
    /// To define a new key for the textarea, you must write a class which implements this interface.
    /// </summary>
    public interface IEditAction //TODO0 should this be a base class instead?
    {
        /// <remarks>
        /// When the key which is defined per XML is pressed, this method will be launched.
        /// </remarks>
        void Execute(TextArea textArea);

        bool UserAction { get; set; }
    }
}
