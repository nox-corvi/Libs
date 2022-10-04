using System;

namespace Nox.IO.Buffer
{
    /// <summary>
    /// Ein einfacher RingBuffer
    /// </summary>
    /// <typeparam name="T">Ein beliebiger Datentyp</typeparam>
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
}
