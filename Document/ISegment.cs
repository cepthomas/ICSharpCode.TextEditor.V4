// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// This interface is used to describe a span inside a text sequence.
    /// </summary>
    public interface ISegment
    {
        int Offset { get; set; }

        int Length { get; set; }

        //[CLSCompliant(false)]
        //protected int offset = -1;
        //[CLSCompliant(false)]
        //protected int length = -1;
    }
}
