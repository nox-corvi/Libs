/*
 * Copyright (c) 2014-2020 Anrá aka Nox
 * 
 * This code is licensed under the MIT license (MIT) 
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights 
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included 
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 * 
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using Nox;
using Nox.Data;
using Nox.Data.SqlServer;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Nox.Libs.Data.Babaj
{
    public class DataRowColl<T> : IList<T>, IEnumerable<T>, INotifyCollectionChanged where T : DataRow
    {
        private DataTable _dataTable;
        private List<T> _Data = new List<T>();

        #region Properties
        public DataTable dataTable { get => _dataTable; set => _dataTable = value; }

        public T this[int index] { get => ((IList<T>)_Data)[index]; set => ((IList<T>)_Data)[index] = value; }

        public int Count => ((IList<T>)_Data).Count;

        public bool IsReadOnly => ((IList<T>)_Data).IsReadOnly;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region Collection Methods
        public void Add(T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>() { item }));

            item.dataTable = dataTable;
            _Data.Add(item);
        }

        public void Clear()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, this));

            _Data.Clear();
        }

        public bool Contains(T item) =>
            _Data.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) =>
            _Data.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() =>
            _Data.GetEnumerator();

        public int IndexOf(T item) =>
            _Data.IndexOf(item);

        public void Insert(int index, T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>() { item }, index));

            _Data.Insert(index, item);
        }

        public bool Remove(T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>() { item }));

            return _Data.Remove(item);
        }

        public void RemoveAt(int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>() { this[index] }, index));

            _Data.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            _Data.GetEnumerator();
        #endregion

        public DataRowColl(DataTable dataTable)
            : base()
        {
            this._dataTable = dataTable;
        }
    }
}
