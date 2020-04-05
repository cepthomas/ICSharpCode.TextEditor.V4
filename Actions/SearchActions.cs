using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.TextEditor.Document;


namespace ICSharpCode.TextEditor.Actions
{
    public class Find : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {

        }
    }

    public class FindAgain : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {

        }
    }

    public class FindAgainReverse : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {

        }
    }

    public class FindAndReplace : IEditAction
    {
        public bool UserAction { get; set; } = false;

        public void Execute(TextArea textArea)
        {

        }
    }
}
