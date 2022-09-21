using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Nox.Component
{
    public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            PropertyChanging?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        protected void OnPropertyChanging([CallerMemberName] string? propertyName = null) =>
            OnPropertyChanging(new PropertyChangingEventArgs(propertyName));


        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string propertyName = null)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            if (comparer.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, [CallerMemberName] string propertyName = null)
        {
            ArgumentNullException.ThrowIfNull(callback);

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, [CallerMemberName] string propertyName = null)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            ArgumentNullException.ThrowIfNull(callback);

            if (comparer.Equals(oldValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);

            return true;
        }
    }

}
