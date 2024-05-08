using System;
using System.ComponentModel;
using System.Reflection;


namespace Nox.WebApi;

public interface IDataRow
{
    Guid Id { get; set; }

    object GetPropertyValue(PropertyInfo info);
    object GetPropertyValue(string PropertyName);

    T GetPropertyValue<T>(PropertyInfo info);
    T GetPropertyValue<T>(string PropertyName);
}

public abstract class DataRow
{
    public abstract Guid Id { get; set; }

    #region Helpers
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
                return default;
        else
            return default;
    }

    public T GetPropertyValue<T>(string PropertyName) =>
        GetPropertyValue<T>(this.GetType().GetProperty(PropertyName));
    #endregion
}
