using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox
{
    public class CancelEventArgs : System.EventArgs
    {
        public bool Cancel { set; get; }

        public CancelEventArgs() { }
    }

    public class ListItemEventArgs<T> : CancelEventArgs
    {
        #region Properties
        private T _Item;
        public T Item { get { return _Item; } }
        #endregion

        public ListItemEventArgs(T Item)
        {
            _Item = Item;
        }
    }
}
