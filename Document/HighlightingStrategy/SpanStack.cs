// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// A stack of Span instances. Works like Stack<Span>, but can be cloned quickly
    /// because it is implemented as linked list.
    /// </summary>
    public sealed class SpanStack : ICloneable, IEnumerable<Span>
    {
        #region Fields
        StackNode _top = null;
        #endregion

        #region Properties
        #endregion

        internal sealed class StackNode
        {
            public readonly StackNode Previous;
            public readonly Span Data;

            public StackNode(StackNode previous, Span data)
            {
                Previous = previous;
                Data = data;
            }
        }

        #region Lifecycle

        #endregion

        #region Public functions
        public Span Pop()
        {
            Span s = _top.Data;
            _top = _top.Previous;
            return s;
        }

        public Span Peek()
        {
            return _top.Data;
        }

        public void Push(Span s)
        {
            _top = new StackNode(_top, s);
        }

        public bool IsEmpty
        {
            get
            {
                return _top == null;
            }
        }

        public SpanStack Clone()
        {
            SpanStack n = new SpanStack();
            n._top = _top;
            return n;
        }
        #endregion

        #region Interfaces implementation
        object ICloneable.Clone()
        {
            return Clone();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(new StackNode(_top, null));
        }

        IEnumerator<Span> IEnumerable<Span>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<Span>
        {
            StackNode c;

            internal Enumerator(StackNode node)
            {
                c = node;
            }

            public Span Current
            {
                get
                {
                    return c.Data;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return c.Data;
                }
            }

            public void Dispose()
            {
                c = null;
            }

            public bool MoveNext()
            {
                c = c.Previous;
                return c != null;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
        #endregion
    }
}
