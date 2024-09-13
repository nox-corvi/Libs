using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nox
{
    public class Cache<T>
        : IDisposable
    {
        public enum CacheExpirationEnum { NoExpiration, SlidingExpiration, AbsoluteExpiration }

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

        private Dictionary<string, CacheItem> _Cache = new();
        private bool disposedValue;

        private readonly ILogger<Cache<T>> _logger = null!;

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
                if (!_Cache.TryGetValue(Key, out CacheItem Value))
                {
                    _logger.LogDebug($"Add to Cache {Key}:{Helpers.NZ(Value)}");

                    var CacheValue = new CacheItem(Key)
                    {
                        Expiration = this.Expiration,
                        Value = CacheNotHit.Invoke()
                    };

                    _Cache.Add(Key, CacheValue);

                    return CacheValue.Value;
                }
                else
                {
                    _logger.LogDebug($"Get from Cache {Key}:{Helpers.NZ(Value)}");

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
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        public T SetCacheValue(string Key, Func<T> ValueFunc)
        {
            try
            {
                if (!_Cache.TryGetValue(Key, out CacheItem Value))
                {
                    _logger.LogDebug($"Add to Cache {Key}:{Helpers.NZ(Value)}");

                    var CacheValue = new CacheItem(Key)
                    {
                        Expiration = this.Expiration,
                        Value = ValueFunc.Invoke()
                    };

                    _Cache.Add(Key, CacheValue);

                    return CacheValue.Value;
                }
                else
                    // return with always overwrite
                    return Value.Value = ValueFunc.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        public bool CacheValueExists(string Key)
        {
            try
            {
                return (_Cache.TryGetValue(Key, out CacheItem Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                return false;
            }
        }

        public T GetCacheValue(string Key, T DefaultValue = default)
        {
            try
            {
                if (_Cache.TryGetValue(Key, out CacheItem Value))
                {
                    _logger.LogDebug($"Get from Cache {Key}:{Helpers.NZ(Value)}");

                    return Value.Value;
                }
                else
                    return DefaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return default;
            }
        }
        #endregion

        public Cache() =>
             _logger = Global.CreateLogger<Cache<T>>();

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

            return default;
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

        private readonly ILogger<ThreadSafeDataList<T>> _logger = null!;

        #region IList
        public T this[int index]
        {
            get
            {
                lock (_List)
                {
                    return _List[index];
                }
            }
            set
            {
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
                lock (_List)
                {
                    return _List.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public void Add(T value)
        {
            lock (_List)
            {
                _List.Add(value);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            lock (_List)
            {
                _List.AddRange(collection);
            }
        }

        public void Insert(int index, T value)
        {
            lock (_List)
            {
                _List.Insert(index, value);
            }
        }
        public bool Remove(T item)
        {
            lock (_List)
            {
                return _List.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_List)
            {
                _List.RemoveAt(index);
            }
        }

        public void RemoveRange(int index, int count)
        {
            lock (_List)
            {
                _List.RemoveRange(index, count);
            }
        }

        public void Clear()
        {
            lock (_List)
            {
                _List.Clear();
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
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
            lock (_List)
            {
                return _List.Contains(value);
            }
        }
        public IEnumerator GetEnumerator()
        {
            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        public int IndexOf(T value)
        {
            lock (_List)
            {
                return _List.IndexOf(value);
            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        #endregion

        void blub()
            => _logger.LogInformation("");

        #region ICloneable
        public object Clone()
        {
            var Result = new ThreadSafeDataList<T>();
            lock (_List)
            {
                for (int i = 0; i < _List.Count(); i++)
                    Result.Add(_List[i]);
            }

            return Result;
        }
        #endregion

        public ThreadSafeDataList()
            : base()
             => _logger = Global.CreateLogger<ThreadSafeDataList<T>>();
    }
}
