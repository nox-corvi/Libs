using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nox.WebApi;

public abstract class DataModel
    : IDisposable
{
    private readonly ILogger<DataModel> Logger;

    private Assembly _Assembly = null!;
    private string _Namespace = "";

    public readonly string ConnectionString;
    public Operate Operate { get; } = null;

    private Cache<TableDescriptor> TableDesciptors = new Cache<TableDescriptor>();
    private List<string> TableKeys = new();

    #region Cache Methods
    public void CacheAttributes()
    {   
        Logger.LogDebug(nameof(CacheAttributes));

        CacheAttributes(_Assembly, _Namespace);
    }

    private void CacheAttributes(Assembly assembly, string Namespace = "")
    {
        Logger.LogDebug($"{nameof(CacheAttributes)}({assembly}), {Namespace})");

        foreach (var dataModelType in assembly.GetTypes())
        {
            if (dataModelType.IsSubclassOf(typeof(DataModel)))
            {
                if (Namespace != "")
                {
                    // try next if namespace does not match
                    if (!dataModelType.Namespace.Equals(Namespace, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                // check if namespace of types is subordinated to datamodel
                foreach (var item in assembly.GetTypes())
                {
                    if (typeof(DataRow).IsAssignableFrom(item))
                    {
                        if (item.Namespace.Equals(dataModelType.Namespace, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string Key = $"{item.Namespace}.{item.Name}";

                            if (!TableKeys.Exists(f => f.Equals(Key, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                // add if not exists
                                TableKeys.Add(Key);
                            }

                            if (!TableDesciptors.CacheValueExists(Key))
                            {
                                var TableDescriptorItem = new TableDescriptor(item.Name);

                                TableDescriptorItem.TableSource = item.GetCustomAttribute<TableAttribute>()?.TableSource ?? "";

                                foreach (var property in item.GetProperties())
                                {
                                    var PropertyDescriptorItem = new PropertyDescriptor(property);

                                    foreach (var attribute in property.GetCustomAttributes())
                                    {
                                        // check primarykey status
                                        if (attribute.GetType().IsAssignableFrom(typeof(PrimaryKeyAttribute)))
                                            PropertyDescriptorItem.IsPrimaryKey = true;

                                        // get table source field
                                        if (typeof(ColumnAttribute).IsAssignableFrom(attribute.GetType()))
                                        {
                                            PropertyDescriptorItem.Source = (attribute as ColumnAttribute).Source;
                                            PropertyDescriptorItem.Order = (attribute as ColumnAttribute).Order;

                                            PropertyDescriptorItem.MappingDescriptor = new ColumnMappingDescriptor(PropertyDescriptorItem);

                                            TableDescriptorItem.Add(PropertyDescriptorItem);
                                        }
                                    }
                                }


                                TableDesciptors.SetCacheValue(Key, () => TableDescriptorItem);
                            }
                        }
                    }
                }
                return;
            }
        }

        // babaj needs an implementation of the datamodel class
        throw new Exception("datamodel not implemented");
    }
    #endregion

    public TableDescriptor GetTableDescriptor(string Key)
        => TableDesciptors.GetCacheValue(Key);

    public DataModel(string ConnectionString, ILogger<DataModel> Logger, string Namespace = "")
    {
        this.ConnectionString = ConnectionString;
        this.Logger = Logger ?? throw new ArgumentNullException(nameof(Logger));

        this.Operate = new Operate(ConnectionString, Logger);

        CacheAttributes(_Assembly = Assembly.GetCallingAssembly(), _Namespace = Namespace);
    }

    public virtual void Dispose()
        => TableDesciptors.Dispose();
}