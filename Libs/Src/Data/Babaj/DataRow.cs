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
    [Flags]
    public enum DataRowStateEnum
    {
        Detached = 1,
        Unchanged = 2,
        Added = 4,
        Deleted = 8,
        Modified = 16,
    }

    //public class DataRowEventArgs : EventArgs
    //{
    //    public DataRow dataRow { get; set; }

    //    public DataRowEventArgs(DataRow dataRow) :
    //        base()
    //    {
    //        this.dataRow = dataRow;
    //    }
    //}
    //public class DataRowCancelEventArgs : CancelEventArgs
    //{
    //    public DataRow dataRow { get; set; }

    //    public DataRowCancelEventArgs(DataRow dataRow) :
    //        base()
    //    {
    //        this.dataRow = dataRow;
    //    }
    //}

    //public delegate void DataRowCancelEventHandler(object sender, DataRowCancelEventArgs e);
    //public delegate void DataRowEventHandler(object sender, DataRowEventArgs e);

    public abstract class DataRow : INotifyPropertyChanged
    {
        private DataTable _dataTable;

        public event PropertyChangedEventHandler PropertyChanged;

        //public event DataRowCancelEventHandler BeforeAttachRow;
        //public event DataRowEventHandler AfterAttachRow;

        //public event DataRowCancelEventHandler BeforeDetachRow;
        //public event DataRowEventHandler AfterDetachRow;

        //public event DataRowCancelEventHandler BeforeInsertRow;
        //public event DataRowEventHandler AfterInsertRow;

        //public event DataRowCancelEventHandler BeforeUpdateRow;
        //public event DataRowEventHandler AfterUpdateRow;

        //public event DataRowCancelEventHandler BeforeDeleteRow;
        //public event DataRowEventHandler AfterDeleteRow;

        #region Properties
        public DataTable dataTable
        {
            get => _dataTable;
            set
            {
                if (_dataTable != value)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(dataTable)));
                    _dataTable = value;

                    // set detached-flag
                    if (value != null)
                        DataRowState &= (~DataRowStateEnum.Detached);
                    else
                        DataRowState |= DataRowStateEnum.Detached;
                }
            }
        }

        public DataRowStateEnum DataRowState { get; private set; } = DataRowStateEnum.Detached | DataRowStateEnum.Unchanged;

        public bool IsDetached =>
            ((DataRowState & DataRowStateEnum.Detached) == DataRowStateEnum.Detached);

        public bool IsUnchanged =>
        ((DataRowState & DataRowStateEnum.Unchanged) == DataRowStateEnum.Unchanged);

        public bool IsAdded =>
            ((DataRowState & DataRowStateEnum.Added) == DataRowStateEnum.Added);

        public bool IsDeleted =>
            ((DataRowState & DataRowStateEnum.Deleted) == DataRowStateEnum.Deleted);

        public bool IsModified =>
            ((DataRowState & DataRowStateEnum.Modified) == DataRowStateEnum.Modified);
        #endregion

        /// <summary>
        /// Wird ausgelöst, wenn eine Zeile neu der DataTable hinzugefügt wird.
        /// </summary>
        public virtual void OnBeforeInsertRow()
        {

        }

        /// <summary>
        /// Wird ausgelöst, wenn eine vorhandene Zeile in der DataTable geändert wird.
        /// </summary>
        public virtual void OnBeforeUpdateRow()
        {

        }

        /// <summary>
        /// Wird ausgelöst, wenn eine vorhandene Zeile in der DataTable gelöscht wird.
        /// </summary>
        public virtual void OnBeforeDeleteRow()
        {

        }

        public virtual void OnBeforeAttachRow()
        {

        }

        public virtual void OnDetachRow()
        {

        }

        #region Helpers
        public void PropertyChange([CallerMemberName]string PropertyName = "") =>
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        public void SetPropertyValue<T>(ref T TargetVar, T value, [CallerMemberName]string PropertyName = "") where T : IComparable
        {
            if (TargetVar?.CompareTo(value) != 0)
            {
                // Assign 
                TargetVar = value;

                // Notify
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
            }
        }

        public object GetPropertyValue(PropertyInfo info)
        {
            if (info != null)
                return info.GetValue(this, null);
            else
                return null;
        }

        public object GetPropertyValue(string PropertyName) =>
            GetPropertyValue(this.GetType().GetProperty(PropertyName));

        public T GetPropertyValue<T>(PropertyInfo info)
        {
            var value = GetPropertyValue(info);
            if (value != null)
                if (!Convert.IsDBNull(value))
                {
                    /* error if try to convert double to float, use invariant cast from String!!!
                     * http://stackoverflow.com/questions/1667169/why-do-i-get-invalidcastexception-when-casting-a-double-to-decimal
                     */
                    if (typeof(T) == typeof(double))
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value.ToString());
                    else
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(value.ToString());
                }
                else
                    return default(T);
            else
                return default(T);
        }

        public T GetPropertyValue<T>(string PropertyName) =>
            GetPropertyValue<T>(this.GetType().GetProperty(PropertyName));
        #endregion

        /// <summary>
        /// Akzeptiert die Änderungen und setzt den RowState auf Unchanged zurück. 
        /// </summary>
        public void AcceptChanges()
        {
            if ((DataRowState & DataRowStateEnum.Detached) == DataRowStateEnum.Detached)
                throw new RowNotInTableException();

            DataRowState &= (~DataRowStateEnum.Modified);
            DataRowState |= DataRowStateEnum.Unchanged;
        }

        public static T Get<T>(string ConnectionString, Guid Id) where T : DataRow
        {
            var Result = (T)Activator.CreateInstance(typeof(T), ConnectionString);

            return Result;
        }

        public static T New<T>(string ConnectionString) where T : DataRow =>
            (T)Activator.CreateInstance(typeof(T), ConnectionString);

        public DataRow() =>
            PropertyChanged += (object sender, PropertyChangedEventArgs e) => DataRowState |= DataRowStateEnum.Modified;
    }
}
