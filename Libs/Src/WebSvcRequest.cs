﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Nox
{
    public enum WebSvcResult
    {
        Ok = 0,
        NoResult = 1,
        Error = 2,
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class WebSvcAutoProcess 
        : Attribute
    {
        public bool EnableFullText { get; set; } = false;

        public string StringTransform { get; set; } = "";
    }

    public interface IWebSvcResponseSeed
    {
        /// <summary>
        /// each object must have an id
        /// </summary>
        Guid ObjectId { get; }
    }

    public class WebSvcResponseSeed 
        : IWebSvcResponseSeed
    {
        public Guid ObjectId { get; } = Guid.NewGuid();

        private List<PropertyInfo> _asPropInfos;
        /// <summary>
        /// Ermittelt die ProperyInfos für alle Eigenschaften mit dem Attribut WebSvcAutoSearch
        /// </summary>
        private IEnumerable<PropertyInfo> AutoSearchPropertyInfos
        {
            get
            {
                // zwischenpuffern .. 
                if (_asPropInfos == null)
                {
                    Type t = MethodBase.GetCurrentMethod().DeclaringType;
                    _asPropInfos = new List<PropertyInfo>();
                    foreach (var Item in this.GetType().GetProperties())
                    {
                        if ((Item.CanRead) & (Attribute.IsDefined(Item, typeof(WebSvcAutoProcess))))
                            _asPropInfos.Add(Item);

                    }
                }
                return _asPropInfos;
            }
        }

        private string GetPropValue(PropertyInfo i)
        {
            if (typeof(IFormattable).IsAssignableFrom(i.PropertyType))
            {

                var f = i.GetCustomAttribute<WebSvcAutoProcess>().StringTransform;

                if (f != "")
                    return ((IFormattable)i.GetValue(this)).ToString("yyyyMMdd", null);
                else
                    return i.GetValue(this).ToString();
            }
            else
                return Helpers.NZ(i.GetValue(this));
        }

        public string Value(string PropertyName)
            => GetPropValue(AutoSearchPropertyInfos.Where(f => f.Name.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault());

        private IEnumerable<string> AutoSearchValues() =>
            from c in AutoSearchPropertyInfos
            select GetPropValue(c);

        private IEnumerable<string> PropertySearchValues(string PropertyName) =>
            from c in AutoSearchPropertyInfos.Where(f => f.Name.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase))
            select GetPropValue(c);

        public bool Match(string PropertyName, string From, string To)
        {
            IEnumerable<string> Values = PropertyName == "*" ? AutoSearchValues() : PropertySearchValues(PropertyName);

            if (From != "")
                if (To != "")
                    // between 
                    return (from c in Values
                            where c.CompareTo(From) >= 0 & c.CompareTo(To) <= 0
                            select c).Count() > 0;
                else
                    // greater or equal
                    return (from c in Values
                            where c.CompareTo(From) >= 0
                            select c).Count() > 0;
            else
                // less or equal
                return (from c in Values
                        where c.CompareTo(To) <= 0
                        select c).Count() > 0;
        }

        public bool Match(string PropertyName, string Value)
        {
            IEnumerable<string> Values = PropertyName == "*" ? AutoSearchValues() : PropertySearchValues(PropertyName);

            return (from c in Values
                    where c.IsLike(Value)
                    select c).Count() > 0;
        }
    }

    public class WebSessionCacheObj<T> 
        : IDisposable
    {
        public DateTime Timestamp { get; } = DateTime.Now;

        public uint Hash { get; set; }

        public T Data { get; set; }
        public WebSessionCacheObj()
        {

        }

        public static uint HashValues(params string[] Values) =>
            Nox.Hash.HashFNV1a32(string.Concat(AppDomain.CurrentDomain.FriendlyName, typeof(T).GetHashCode(), Values));

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }

    public class WebCacheBuffer<T> : IDisposable where T : WebSessionCacheObj<WebSvcResponseSeed>
    {
        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
        public void Dispose() =>
            Dispose(true);
        #endregion
    }

    [Obsolete()]
    public interface WebSvcResultSeedList<T> 
        : IWebSvcResponseSeed, IEnumerable<T> where T : WebSvcResponseSeed
    {

    }

    //public class KeyValuePair
    //{
    //    /// <summary>
    //    /// Der zugehörige Schlüssel
    //    /// </summary>
    //    public string Key { get; set; } = "";

    //    /// <summary>
    //    /// Der zugehörige Wert
    //    /// </summary>
    //    public string Value { get; set; } = "";

    //    public KeyValuePair(string Key, string Value)
    //    {
    //        this.Key = Key;
    //        this.Value = Value;
    //    }
    //}

    [Serializable()]
    public class WebSvcResponse
    {
        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;

        #region Properties
        [Newtonsoft.Json.JsonIgnore]
        public IConfiguration Configuration
        {
            get => _configuration;
        }

        [Newtonsoft.Json.JsonIgnore]
        public ILogger Logger
        {
            get => _logger;
        }
        /// <summary>
        /// Gibt einen Status zurück der angibt ob die Aktion erfolgreich war
        /// </summary>
        public WebSvcResult State { get; set; }

        /// <summary>
        /// Gibt eine Statusnachricht zurück.
        /// </summary>
        public string Message { get; set; } = "";
        #endregion

        public WebSvcResponse(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    }

    [Serializable()]
    public abstract class WebSvcResponseShell<T> 
        : WebSvcResponse where T : IWebSvcResponseSeed
    {
        /// <summary>
        /// Gibt das Datenobjekt vom Typ IWebSvcResultSeed zurück wenn erfolgreich, sonst null
        /// </summary>
        public abstract T Data { get; set; }

        #region Helpers
        protected static string SerializeException(Exception ex)
        {
            var Result = new StringBuilder();

            if (ex != null)
            {
                Result.Append($"<exception>");
                Result.Append($"<source>{ex.Source}</source>");
                Result.Append($"<message>{ex.Message}</message>");
                Result.Append($"<stacktrace>{ex.StackTrace}</stacktrace>");
                Result.Append($"<data>");
                foreach (var Item in ex.Data.Keys)
                    Result.Append($"<{Item}>{ex.Data[Item]}</{Item}>");

                if (ex.InnerException != null)
                    Result.Append(SerializeException(ex.InnerException));

                Result.Append($"</data>");

                Result.Append($"</exception>");
            }
            else
                Result.Append($"<exception />");

            return Result.ToString();
        }
        #endregion

        public string SerializeData()
        {
            XmlSerializer writer = new XmlSerializer(typeof(T));
            using (StringWriter file = new StringWriter())
            {
                writer.Serialize(file, Data);
                return file.ToString();
            }
        }

        public WebSvcResponseShell(ILogger logger, IConfiguration configuration)
            : base(logger, configuration)
        {

        }
    }
}
