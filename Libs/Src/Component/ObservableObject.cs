using System;
using System.Runtime;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nox.Component
{
    public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (e == null) throw new ArgumentNullException($"{nameof(e)} is null");
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            if (e == null) throw new ArgumentNullException($"{nameof(e)} is null");
            PropertyChanging?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected void OnPropertyChanging([CallerMemberName] string propertyName = null) =>
            OnPropertyChanging(new PropertyChangingEventArgs(propertyName));


        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>(ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string propertyName = null)
        {
            if (comparer == null) throw new ArgumentNullException($"{nameof(comparer)} is null");
            if (comparer.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, [CallerMemberName] string propertyName = null)
        {
            if (callback == null) throw new ArgumentNullException($"{nameof(callback)} is null");

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, [CallerMemberName] string propertyName = null)
        {
            if (comparer == null) throw new ArgumentNullException($"{nameof(comparer)} is null");
            if (comparer.Equals(oldValue, newValue))
                return false;

            if (callback == null) throw new ArgumentNullException($"{nameof(callback)} is null");

            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);

            return true;
        }
    }

}
