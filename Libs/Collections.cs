using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nox
{
    public class Cache<T> : IDisposable
    {
        public enum CacheExpirationEnum { NoExpiration, SlidingExpiration, AbsoluteExpiration }

        private Log4 _Log;

        private class CacheItem
        {
            private CacheExpirationEnum _CacheExpiration = CacheExpirationEnum.SlidingExpiration;

            private DateTime _Created = DateTime.Now;
            private DateTime _LastAccess = DateTime.Now;

            private T _Value;

            #region Properties
            public readonly string Key;

            public T Value
            {
                get
                {
                    if (Expiration == CacheExpirationEnum.SlidingExpiration)
                        _LastAccess = DateTime.Now;

                    return _Value;
                }
                set => _Value = value;
            }

            public CacheExpirationEnum Expiration
            {
                set => _CacheExpiration = value;
                get => _CacheExpiration;
            }
            #endregion

            public bool Expired(int ExpirationTime)
            {
                if (_CacheExpiration == CacheExpirationEnum.NoExpiration)
                    return false;
                else
                    return (_LastAccess.AddSeconds(ExpirationTime) < DateTime.Now);
            }

            public CacheItem(string Key) =>
                this.Key = Key;
        }

        private Dictionary<string, CacheItem> _Cache = new Dictionary<string, CacheItem>();
        private bool disposedValue;

        #region Properties
        public int CacheExpirationTime { get; set; } = 300; // in Seconds, 5min default time

        public CacheExpirationEnum Expiration { get; set; } = CacheExpirationEnum.SlidingExpiration;
        #endregion

        #region Cache Methods
        private uint HashValues(params string[] Values)
        {
            var SB = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);

            foreach (var Item in Values)
                SB.AppendLine(Item);

            return Hash.HashFNV1a32(SB.ToString());
        }

        public T CacheValue(string Key, Func<T> CacheNotHit)
        {
            try
            {
                CacheItem Value;
                if (!_Cache.TryGetValue(Key, out Value))
                {
                    _Log.LogMessage($"Add to Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);

                    var CacheValue = new CacheItem(Key) { Expiration = this.Expiration };
                    CacheValue.Value = CacheNotHit.Invoke();

                    _Cache.Add(Key, CacheValue);

                    return CacheValue.Value;
                }
                else
                {
                    _Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);

                    switch (Value.Expiration)
                    {
                        case CacheExpirationEnum.NoExpiration:
                            return Value.Value;
                        case CacheExpirationEnum.SlidingExpiration:
                        case CacheExpirationEnum.AbsoluteExpiration:
                            if (Value.Expired(CacheExpirationTime))
                            {
                                // expired
                                _Cache.Remove(Key);

                                // recall method
                                return CacheValue(Key, CacheNotHit);
                            }
                            else
                                return Value.Value;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                _Log.LogException(ex);
                throw;
            }
        }

        public T SetCacheValue(string Key, Func<T> ValueFunc)
        {
            try
            {
                CacheItem Value;
                if (!_Cache.TryGetValue(Key, out Value))
                {
                    _Log.LogMessage($"Add to Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);

                    var CacheValue = new CacheItem(Key) { Expiration = this.Expiration };
                    CacheValue.Value = ValueFunc.Invoke();

                    _Cache.Add(Key, CacheValue);

                    return CacheValue.Value;
                }
                else
                    // return with always overwrite
                    return Value.Value = ValueFunc.Invoke();
            }
            catch (Exception ex)
            {
                _Log.LogException(ex);
                throw;
            }
        }

        public bool CacheValueExists(string Key)
        {
            try
            {
                CacheItem Value;
                return (_Cache.TryGetValue(Key, out Value));
            }
            catch (Exception ex)
            {
                _Log.LogException(ex);

                return false;
            }
        }

        public T GetCacheValue(string Key, T DefaultValue = default(T))
        {
            try
            {
                CacheItem Value;
                if (_Cache.TryGetValue(Key, out Value))
                {
                    _Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);

                    return Value.Value;
                }
                else
                    return DefaultValue;
            }
            catch (Exception ex)
            {
                _Log.LogException(ex);
                return default(T);
            }
        }
        #endregion

        public Cache() =>
            _Log = new Log4("Cache");

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (typeof(T).IsAssignableFrom(typeof(IDisposable)))
                        foreach (var item in _Cache)
                            ((IDisposable)item.Value)?.Dispose();

                    _Cache.Clear();
                }

                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~Cache()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class RingBuffer<T> where T : class
    {
        private T[] _Buffer;
        private int[] _freqRead;

        private int _last;
        private int _index;

        #region Properties

        /// <summary>
        /// Gibt den aktuellen Index zurück
        /// </summary>
        public virtual int Index
        {
            get
            {
                return _index;
            }
        }

        /// <summary>
        /// Gibt die Puffergröße zurück
        /// </summary>
        public virtual int Size { get { return _Buffer.Length; } }

        /// <summary>
        /// Gibt den letzten Index zurück
        /// </summary>
        public int Last { get { return _last; } }

        /// <summary>
        /// Liefert einen Eintrag zurück
        /// </summary>
        /// <param name="Index">Der Slot der ausgelesen werden soll</param>
        /// <returns>Der Wert aus dem Speicher</returns>
        public T this[int Index]
        {
            get
            {
                return _Buffer[Index];
            }
        }
        #endregion

        /// <summary>
        /// Ermittelt ob ein Slot frei ist
        /// </summary>
        /// <param name="Index">Der zu prüfende Slot</param>
        /// <returns>Wahr wenn frei, sonst Falsch</returns>
        public bool FREE(int Index) => (_Buffer[Index] == null);

        /// <summary>
        /// Ermittelt wie oft ein Slot gelesen wurde
        /// </summary>
        /// <param name="Index">Der zu prüfende Slot</param>
        /// <returns>-1 wenn frei, sonst größer 0</returns>
        public int freqRead(int Index) => _freqRead[Index];

        /// <summary>
        /// Fügt einen Wert an das Ende der Ringpuffers ein und bewegt den Zeiger weiter
        /// </summary>
        /// <param name="value">Der einzufügende Wert</param>
        public void Append(T value)
        {
            _Buffer[_last = _index] = value;
            _freqRead[_index] = 0;

            --_index;
            while (_index < 0)
                _index += _Buffer.Length;
        }

        /// <summary>
        /// Bewegt den Zeiger
        /// </summary>
        /// <param name="Delta">Der zu bewegende Wert</param>
        /// <returns>Die neue Position</returns>
        public int Move(int delta)
        {
            _index = (_index + delta) % _Buffer.Length;
            while (_index < 0)
                _index += _Buffer.Length;

            return _index;
        }

        public T Find(Predicate<T> match)
        {
            foreach (var Item in _Buffer)
                if ((Item != null) && (match.Invoke(Item)))
                    return Item;

            return default(T);
        }

        public RingBuffer(int BufferSize)
        {
            _Buffer = new T[BufferSize];
            _freqRead = new int[BufferSize];

            for (int i = 0; i < BufferSize; i++)
                _Buffer[i] = null;

            _last = -1;
            _index = 0;
        }
    }

    public class ThreadSafeDataList<T>
        : ICloneable, IList<T>

    {
        private List<T> _List = new();
        private Log4 _Log = null!;

        #region IList
        public T this[int index]
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

                lock (_List)
                {
                    return _List[index];
                }
            }
            set
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

                lock (_List)
                {
                    _List[index] = value;
                }

            }
        }

        public int Count
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

                lock (_List)
                {
                    return _List.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

                return false;
            }
        }

        public void Add(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                _List.Add(value);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, collection);

            lock (_List)
            {
                _List.AddRange(collection);
            }
        }

        public void Insert(int index, T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index, value);

            lock (_List)
            {
                _List.Insert(index, value);
            }
        }
        public bool Remove(T item)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, item);

            lock (_List)
            {
                return _List.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

            lock (_List)
            {
                _List.RemoveAt(index);
            }
        }

        public void RemoveRange(int index, int count)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index, count);

            lock (_List)
            {
                _List.RemoveRange(index, count);
            }
        }

        public void Clear()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                _List.Clear();
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, array, arrayIndex);

            lock (_List)
            {
                _List.CopyTo(array, arrayIndex);
            }
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            // Delegate rest of error checking to Array.Copy.
            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = _List[index + i];
        }

        public bool Contains(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                return _List.Contains(value);
            }
        }
        public IEnumerator GetEnumerator()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        public int IndexOf(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                return _List.IndexOf(value);
            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        #endregion

        #region ICloneable
        public object Clone()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            var Result = new ThreadSafeDataList<T>();
            lock (_List)
            {
                for (int i = 0; i < _List.Count(); i++)
                    Result.Add(_List[i]);
            }

            return Result;
        }
        #endregion

        public ThreadSafeDataList(Log4 Log)
            : base()
            => (_Log = Log)?.LogMethod(Log4.Log4LevelEnum.Trace);

        public ThreadSafeDataList()
            : base() { }
    }
}
