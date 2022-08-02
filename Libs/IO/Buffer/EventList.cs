using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.IO.Buffer
{
    public class EventList<T> : List<T>
    {
        public event EventHandler<ListItemEventArgs<T>> OnItemAdd;
        public event EventHandler<ListItemEventArgs<T>> OnItemRemove;

        public new void Add(T Item)
        {
            ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
            OnItemAdd?.Invoke(this, Args);

            if (Args.Cancel) return;

            base.Add(Item);
        }

        

        public new bool Remove(T Item)
        {
            ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
            OnItemRemove?.Invoke(this, Args);

            if (Args.Cancel) return false;

            return base.Remove(Item);
        }

        public new void Clear()
        {
            if (OnItemRemove != null)
                for (int i = 0; i < base.Count; i++)
                {
                    ListItemEventArgs<T> Args = new ListItemEventArgs<T>(base[i]);
                    OnItemRemove.Invoke(this, Args);

                    if (Args.Cancel) return;
                }

            base.Clear();
        }

        public new void AddRange(IEnumerable<T> Collection)
        {
            if (OnItemAdd != null)
                foreach (T Item in Collection)
                {
                    ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
                    OnItemAdd.Invoke(this, Args);

                    if (Args.Cancel) return;
                }

            base.AddRange(Collection);
        }

        public new void Insert(int Index, T Item)
        {
            if (OnItemAdd != null)
            {
                ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
                OnItemAdd.Invoke(this, Args);

                if (Args.Cancel) return;
            }

            base.Insert(Index, Item);
        }

        public new void InsertRange(int Index, IEnumerable<T> Collection)
        {
            if (OnItemAdd != null)
                foreach (T Item in Collection)
                {
                    ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
                    OnItemAdd.Invoke(this, Args);

                    if (Args.Cancel) return;
                }

            base.InsertRange(Index, Collection);
        }

        public new int RemoveAll(Predicate<T> Match)
        {
            if (OnItemRemove != null)
                for (int i = 0; i < base.Count; i++)
                    if (Match(base[i]))
                    {
                        ListItemEventArgs<T> Args = new ListItemEventArgs<T>(base[i]);
                        OnItemAdd.Invoke(this, Args);

                        if (Args.Cancel) return 0;
                    }

            return base.RemoveAll(Match);
        }

        public new void RemoveAt(int Index)
        {
            try
            {
                T Item = base[Index];

                if (OnItemRemove != null)
                {
                    ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
                    OnItemAdd.Invoke(this, Args);

                    if (Args.Cancel) return;
                }
            }
            finally
            {
                base.RemoveAt(Index);
            }
        }

        public new void RemoveRange(int Index, int Count)
        {
            if (OnItemRemove != null)
                for (int i = 0; i < base.Count; i++)
                {
                    try
                    {
                        T Item = base[i];

                        ListItemEventArgs<T> Args = new ListItemEventArgs<T>(Item);
                        OnItemAdd.Invoke(this, Args);

                        if (Args.Cancel) return;
                    }
                    catch { }
                }
            base.RemoveRange(Index, Count);
        }
    }
}
