/*
 * Copyright (c) 2014-2018 Anrá aka Nox
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
using System.IO;
using M = System.Math;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Cryptography;
using Nox.Libs;
using Nox.Libs.Data;
using Nox.Libs.Data.SqlServer;
using Nox.Libs.Buffer;

namespace Nox.Libs.IO
{
    public enum FSFlags
    {
        None = 0,
        Hidden = 1,
        Archive = 2,
        ReadOnly = 4,
        Encrypted = 8,
        SymLink = 64,

        Transformed = 128,

        SystemUseOnly = 256,

        Directory = 8192,
    }

    /// <summary>
    /// Die Basisklasse für alle FS-Objekte
    /// </summary>
    public abstract class FSObject
    {
        /// <summary>
        /// Liefert einen Verweis auf das FSBase-Objekt zurück
        /// </summary>
        public virtual FS FSBase { get; protected set; }

        public FSObject(FS FS)
        {
            this.FSBase = FS;
        }
    }

    /// <summary>
    /// Die Basisklasse für alle FSObjekte, welche innerhalb einen FSClusters verwaltet werden
    /// </summary>
    public abstract class FSElement : FSObject
    {
        /// <summary>
        /// Liefert zurück, ob ein Feld geändert wurde oder legt es fest
        /// </summary>
        public virtual bool Dirty { get; protected set; }

        public abstract void Read(BinaryReader Reader);
        public abstract void Write(BinaryWriter Writer);

        public FSElement(FS filesystem)
            : base(filesystem)
        {
        }
    }

    /// <summary>
    /// Die Basisklasse für alle FSObjekte welche als Container für mehrere FSClusterObjekte dienen
    /// </summary>
    public abstract class FSContainer : FSObject
    {
        /// <summary>
        /// Liefert zurück, ob ein Feld geändert wurde oder legt es fest
        /// </summary>
        public virtual bool Dirty { get; protected set; }

        public abstract void Read();
        public abstract void Write();

        public FSContainer(FS filesystem)
            : base(filesystem)
        {
        }
    }

    /// <summary>
    /// Die Basisklasse für alle FSObjekte, welche in einem Cluster gespeichert werden.
    /// </summary>
    public abstract class FSCluster : FSObject
    {
        // Felder
        private int _Cluster;

        /// <summary>
        /// Liefert die Nummer des Clusters zurück
        /// </summary>
        public int Cluster { get { return _Cluster; } }

        /// <summary>
        /// Liefert zurück ob ein Feld geändert wurde oder legt es fest
        /// </summary>
        public virtual bool Dirty { get; protected set; }

        /// <summary>
        /// Liest die Daten aus der Datei
        /// </summary>
        public virtual void Read()
        {
            try
            {
                FSBase.Handle.Position = (FSBase.Header.ClusterSize * Cluster) + FSBase.Header.FirstClusterOffset;

                var CryptoStream = new CryptoStream(FSBase.Handle, FSBase.CreateDecryptor(), CryptoStreamMode.Read);
                BinaryReader Reader = new BinaryReader(CryptoStream);

                ReadUserData(Reader);
                Dirty = false;
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
            catch (Exception e)
            {
                throw new FSException(e.Message);
            }
        }

        /// <summary>
        /// Kann überladen werden, um benutzerdefinierte Daten zu lesen
        /// </summary>
        /// <param name="Reader">Der BinaryReader von dem gelesen werden soll</param>
        public abstract void ReadUserData(BinaryReader Reader);

        /// <summary>
        /// Schreibt die Daten in die Datei
        /// </summary>
        public virtual void Write()
        {
            if (Dirty)
            {
                try
                {
                    FSBase.Handle.Position = (FSBase.Header.ClusterSize * Cluster) + FSBase.Header.FirstClusterOffset;

                    var CryptoStream = new CryptoStream(FSBase.Handle, FSBase.CreateEncryptor(), CryptoStreamMode.Write);
                    BinaryWriter Writer = new BinaryWriter(CryptoStream);

                    WriteUserData(Writer);

                    // Leeren des Schreib-Puffers erzwingen
                    Writer.Flush();

                    // CryptoStream gefüllt, Puffer leeren
                    CryptoStream.Flush();
                    
                    // und abschliessen
                    CryptoStream.FlushFinalBlock();

                    Dirty = false;
                }
                catch (FSException)
                {
                    // pass through
                    throw;
                }
                catch (IOException IOe)
                {
                    throw new FSException(IOe.Message);
                }
                catch (Exception e)
                {
                    throw new FSException(e.Message);
                }
            }
        }

        /// <summary>
        /// Kann überladen werden, um benutzerdefinierte Daten zu schreiben
        /// </summary>
        /// <param name="Writer">Der BinaryWriter in den geschrieben werden soll</param>
        public abstract void WriteUserData(BinaryWriter Writer);

        public FSCluster(FS filesystem, int Cluster)
            : base(filesystem)
        {
            _Cluster = Cluster;
            Dirty = true;
        }
    }

    /// <summary>
    /// Behandelt sämtliche FS-Ausnahmen 
    /// </summary>
    public class FSException : Exception
    {
        #region Konstruktoren
        public FSException(string Message)
            : base(Message)
        {
        }
        public FSException(string Message, Exception innerException)
            : base(Message, innerException)
        {
        }
        #endregion
    }

    public interface IFSPatchTransform : IDisposable
    {
        string Extension { get; }

        FileStream Convert(string SourceFile);
        FileStream Convert(FileStream SourceFile);
    }

    public class FS3Three<T>
    {
        public FS3Three() : base()
        {


        }
    }

    public class FSCache<T> : FSObject where T : FSCluster
    {
        public const int FREE = -1;

        private T[]     Clusters;
        private int     Index;

        #region Cache-Methods
        /// <summary>
        /// Liefert die Position des gesuchten DataClusters zurück
        /// </summary>
        /// <param name="Cluster">Die Nummer des gesuchten DataClusters</param>
        /// <returns>Positiv wenn im Cache, sonst -1</returns>
        public int Exists(int Cluster)
        {
            //TODO:Optimieren der Cache-Suche ...
            int ItemsTested = Clusters.Length, i = Index;
            while (ItemsTested-- > 0)
            {
                if (Clusters[i] != null)
                    if (Clusters[i].Cluster == Cluster)
                        return i;

                i = (++i) >= Clusters.Length ? 0 : i;
            }

            return -1;
        }

        /// <summary>
        /// Entfernt einen Element am angegebenen Index aus dem Cache und gibt das Feld frei
        /// </summary>
        /// <param name="Position">Der nullbasierte Index des zu entfernenden Elements</param>
        public void RemoveAt(int Position)
        {
            try
            {
                if (Clusters[Position] != null)
                {
                    // Schreiben des Clusters auf die Festplatte
                    if (Clusters[Position].Dirty)
                        Clusters[Position].Write();

                    Clusters[Position] = null;
                }
            }
            catch (Exception)
            {
                // pass through
                throw;
            }
        }

        /// <summary>
        /// Sucht einen DataCluster im Cache und gibt ihn zurück
        /// </summary>
        /// <param name="Cluster">Die ClusterNummer die gesucht werden soll</param>
        /// <returns>FSDataCluster wenn vorhanden, sonst null</returns>
        public T Item(int Cluster)
        {
            int Position = Exists(Cluster);

            if (Position == FREE)
                return null;
            else
                return Clusters[Position];
        }

        /// <summary>
        /// Fügt einen DataCluster in den Cache ein
        /// </summary>
        /// <param name="Value">Der DataCluster der eingefügt werden soll</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Append(T Value)
        {
            try
            {
                RemoveAt(Index);

                Clusters[Index] = Value;
                Index = (++Index) >= Clusters.Length ? 0 : Index;
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }

        /// <summary>
        /// Schreibt sämtliche gespeicherten DataClusters auf den Datenträger.
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Flush()
        {
            try
            {
                int ItemsTested = Clusters.Length;
                for (int i = Index; ItemsTested > 0; ItemsTested--)
                {
                    if (Clusters[i] != null)
                        Clusters[i].Write();

                    i = (--i) < 0 ? Clusters.Length - 1 : i;
                }
            }
            catch (FSException)
            {
                // pass
                throw;
            }
        }
        #endregion

        public FSCache(FS filesystem, int CacheSize)
            : base(filesystem)
        {
            Clusters = new T[CacheSize];
            for (int i = 0; i < CacheSize; i++)
                Clusters[i] = null;

            Index = 0;
        }
    }

    public class FSClusterMap : FSCluster
    {
        private const int   DEFAULT_SIGNATURE = 0x1494BFDA;

        private int         _Signature = DEFAULT_SIGNATURE;
        private uint[]      _Map;

        // Felder
        private int _SlotCount;
        private int _SlotsFree;

        #region Properties
        /// <summary>
        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
        /// </summary>
        /// <param name="Index">Der nullbasierte Index des Clusters</param>
        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
        public bool this[int Index]
        {
            get
            {
                uint Mask = (uint)(1 << (Index & 0x1F));
                return (_Map[Index >> 5] & Mask) == Mask;
            }
            set
            {
                try
                {
                    uint Mask = (uint)(1 << (Index & 0x1F));
                    bool Used = (_Map[Index >> 5] & Mask) == Mask;

                    int Modified = (Index >> 5);
                    if (value)
                    {
                        if (!Used)
                            _SlotsFree--;

                        _Map[Modified] |= Mask;
                    }
                    else
                    {
                        if (Used)
                            _SlotsFree++;

                        _Map[Modified] &= (uint)~Mask;
                    }

                    Dirty = true;
                }
                catch (Exception e)
                {
                    throw new FSException(e.Message);
                }
            }
        }

        /// <summary>
        /// Liefert die Anzahl an Slot zurück.
        /// </summary>
        public int SlotCount
        {
            get
            {
                return _SlotCount;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an freien Slots zurück.
        /// </summary>
        public int SlotsFree
        {
            get
            {
                return _SlotsFree;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an belegten Slots zurück
        /// </summary>
        public int SlotsUsed 
        {
            get
            {
                return _SlotCount - _SlotsFree;
            }
        }
        #endregion

        #region I/O
        public override void ReadUserData(BinaryReader Reader)
        {
            try
            {
                if ((_Signature = Reader.ReadInt32()) != DEFAULT_SIGNATURE)
                    throw new FSException("signature mismatch");

                for (int i = 0; i < _Map.Length; i++)
                {
                    uint r = _Map[i] = Reader.ReadUInt32();

                    if (r == 0xFFFFFFFF)
                        _SlotsFree -= 32;
                    else
                        for (int k = 0; k < 32; k++, r >>= 1)
                            _SlotsFree -= (byte)(r & 1);
                }
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }

        public override void WriteUserData(BinaryWriter Writer)
        {
            try
            {
                Writer.Write(_Signature);
                for (int i = 0; i < _Map.Length; i++)
                    Writer.Write(_Map[i]);
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        #endregion

        /// <summary>
        /// Ermittelt einen freien Slot und liefert ihn zurück
        /// </summary>
        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
        public int GetFreeSlot()
        {
            if (_SlotsFree == 0)
                return -1;
            else
            {
                for (int i = 0; i < _Map.Length; i++)
                    if (_Map[i] != 0xFFFFFFFF)
                    {
                        int Start = i << 5;
                        for (int j = 0; j < 32; j++)
                            if (!this[Start + j])
                                return Start + j;
                    }
            }

            return -1;
        }

        public FSClusterMap(FS filesystem, int Cluster)
            : base(filesystem, Cluster)
        {
            _SlotsFree = _SlotCount = ((FSBase.Header.ClusterSize - 8) << 3);
            _Map = new uint[_SlotCount >> 5];
        }
    }

    public class FSClusterMaps : FSContainer
    {
        private FSClusterMap[]   _Map;

        #region Properties
        /// <summary>
        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
        /// </summary>
        /// <param name="Index">Der 0-basierte Index des Clusters</param>
        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
        public bool this[int Index]
        {
            get
            {
                int Map = 0, R = Index;
                while (R >= _Map[Map].SlotCount)
                    R -= _Map[Map++].SlotCount;

                return _Map[Map][R];
            }
            set
            {
                int Map = 0, R = Index;
                while (R >= _Map[Map].SlotCount)
                    R -= _Map[Map++].SlotCount;

                _Map[Map][R] = value;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an Slot zurück.
        /// </summary>
        public int SlotCount
        {
            get
            {
                int Result = 0;
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                    Result += _Map[i].SlotCount;

                return Result;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an freien Slots zurück.
        /// </summary>
        public int SlotsFree
        {
            get
            {
                int Result = 0;
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                    Result += _Map[i].SlotsFree;

                return Result;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an belegten Slots zurück
        /// </summary>
        public int SlotsUsed
        {
            get
            {
                int Result = 0;
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                    Result += _Map[i].SlotsUsed;

                return Result;
            }
        }

        /// <summary>
        /// Liefert zurück ob die Karte geändert worde ist
        /// </summary>
        public override bool Dirty
        {
            get
            {
                var Result = base.Dirty;

                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                    Result |= _Map[i].Dirty;

                return Result;
            }
            protected set
            {
                base.Dirty = value;
            }
        }
        #endregion

        /// <summary>
        /// Liest die ClusterMap vom Dateisystem
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public override void Read()
        {

            try
            {
                _Map = new FSClusterMap[FSBase.Header.ClusterMapThreshold];
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                {
                    _Map[i] = new FSClusterMap(FSBase, FSBase.Header[i]);
                    _Map[i].Read();
                }
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }

        /// <summary>
        /// Schreibt die ClusterMap in das Dateisystem
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public override void Write()
        {
            try
            {
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                    _Map[i].Write();
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }

        /// <summary>
        /// Ermittelt einen freien Slot und liefert ihn zurück
        /// </summary>
        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
        public int GetFreeSlot()
        {
            int Base = 0;
            for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
            {
                if (_Map[i].SlotsFree > 0)
                    return Base + _Map[i].GetFreeSlot();

                Base += _Map[i].SlotCount;
            }

            return -1;
        }

        /// <summary>
        /// Erstellt die ClusterMaps neu und schreibt sie auf den Datenträger
        /// </summary>
        /// <returns></returns>
        public void CreateNewClusterMaps()
        {
            try
            {
                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
                {
                    _Map[i] = new FSClusterMap(FSBase, FSBase.Header[i]);
                    _Map[i].Write();

                    FSBase.Maps[_Map[i].Cluster] = true;
                }
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }

        public FSClusterMaps(FS filesystem)
            : base(filesystem)
        {
            _Map = new FSClusterMap[FSBase.Header.ClusterMapThreshold];
        }
    }

    public class FSDataCluster : FSCluster
    {
        private const uint  DEFAULT_SIGNATURE = 0xFAD53F33;

        private uint    _Signature;

        private int     _Previous;
        private int     _Next;

        private byte[]  _Data;
        private uint    _CRC;

        #region Properties
        /// <summary>
        /// Liefert ein Byte aus dem Datenpuffer zurück oder legt es fest
        /// </summary>
        /// <param name="Index">Der Index an dem das Byte gelesen oder geschrieben werden soll</param>
        /// <returns>Das gelesene Byte</returns>
        public byte this[int Index]
        {
            get
            {
                return _Data[Index];
            }
            set
            {
                _Data[Index] = value;
                Dirty = true;
            }
        }

        public int Previous
        {
            get
            {
                return _Previous;
            }
            set
            {
                if (_Previous != value)
                {
                    _Previous = value;
                    Dirty = true;
                }
            }
        }

        public int Next
        {
            get
            {
                return _Next;
            }
            set
            {
                if (_Next != value)
                {
                    _Next = value;
                    Dirty = true;
                }
            }
        }
        #endregion

        #region I/O
        public override void ReadUserData(BinaryReader Reader)
        {
            try
            {
                if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
                    throw new FSException("signature mismatch");

                _Previous = Reader.ReadInt32();
                _Next = Reader.ReadInt32();

                int DataRead = Reader.Read(_Data, 0, _Data.Length);

                _CRC = Reader.ReadUInt32();
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }

        public override void WriteUserData(BinaryWriter Writer)
        {
            try
            {
                Writer.Write(_Signature);

                Writer.Write(_Previous);
                Writer.Write(_Next);

                Writer.Write(_Data, 0, _Data.Length);
                Writer.Write(_CRC = ReCRC());

                Dirty = false;
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        #endregion

        public int BlockRead(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
        {
            int Read = (_Data.Length - SourceOffset);

            if (Read > Length)
                Read = Length;

            try
            {
                Array.Copy(_Data, SourceOffset, Buffer, DestOffset, Read);
            }
            catch
            {
                throw new FSException("array read-access troubles");
            }

            return Read;
        }

        public int BlockWrite(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
        {
            int Read = (_Data.Length - SourceOffset);

            if (Read > _Data.Length)
                Read = Length;

            try
            {
                Array.Copy(Buffer, SourceOffset, _Data, DestOffset, Length);
                Dirty = true;
            }
            catch
            {
                throw new Exception("array write-access troubles");
            }


            return Read;
        }

        #region Helpers
        /// <summary>
        /// Berechnet die Checksumme für den Cluster.
        /// </summary>
        /// <returns>der CRC32 des Knoten</returns>
        public uint ReCRC()
        {
            var CRC = new CRC();

            CRC.Push(_Signature);
            CRC.Push(_Data, 0, _Data.Length);

            return CRC.CRC32;
        }
        #endregion

        public FSDataCluster(FS IDXFS, int Cluster)
            : base(IDXFS, Cluster)
        {
            _Signature = DEFAULT_SIGNATURE;

            _Data = new byte[IDXFS.Header.UseableClusterSize];

            _CRC = ReCRC();
        }

        ~FSDataCluster()
        {
            try
            {
                if (Dirty)
                    Write();
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }
    }

    public class FSDirectory
    {
        private FSNode                  _Node;

        private EventList<FSDirectory>  _Directories;
        private EventList<FSNode>       _Files;

        #region Properties
        /// <summary>
        /// Liefert die Id des zugrundeliegenden IDXFSNode-Objekts zurück
        /// </summary>
        public uint Id
        {
            get
            {
                return _Node.Id;
            }
        }

        /// <summary>
        /// Liefert den Vorgänger des zugrundeliegenden IDXFSNode-Objekts zurück
        /// </summary>
        public uint Parent
        {
            get
            {
                return _Node.Parent;
            }
        }

        public string Name
        {
            get
            {
                return _Node.Name;
            }
        }

        public uint Flags
        {
            get
            {
                return _Node.Flags;
            }
        }

        public DateTime Created
        {
            get
            {
                return _Node.Created;
            }
        }

        public DateTime Modified
        {
            get
            {
                return _Node.Modified;
            }
        }

        /// <summary>
        /// Liefert die Unterordner des Verzeichnisses zurück
        /// </summary>
        public EventList<FSDirectory> Directories
        {
            get
            {
                return _Directories;
            }
        }

        /// <summary>
        /// Lierfert die Dateien des Verzeichnisses zurück.
        /// </summary>
        public EventList<FSNode> Files
        {
            get
            {
                return _Files;
            }
        }
        #endregion

        public FSDirectory FindDirectory(string Name)
        {
            string FindName = Name.ToLower().Trim();

            for (int i = 0; i < _Directories.Count; i++)
                if (_Directories[i].Name.ToLower().Trim() == FindName)
                    return _Directories[i];

            return null;
        }

        public FSNode FindFile(string Name)
        {
            string FindName = Name.ToLower().Trim();

            for (int i = 0; i < _Files.Count; i++)
                if (_Files[i].Name.ToLower().Trim() == FindName)
                    return _Files[i];

            return null;
        }

        public FSDirectory(FSNode Node)
        {
            _Node = Node;

            _Directories = new EventList<FSDirectory>();
            _Files = new EventList<FSNode>();
        }
    }

    public class FSHeader : FSContainer
    {
        // Konstanten
        private const uint      DEFAULT_SIGNATURE   = 0x3F534652;
        private const ushort    CURRENT_VERSION     = 0x10A0;
        private const int       MAPCLUSTER_THRESHOLD= 4;
        private const ushort    NODE_SIZE           = 128;

        // Felder
        private uint        _Signature;

        private int         _Version;
        private int         _Build;

        private byte[]      _Name;

        private DateTime    _Created;
        private DateTime    _Modified;

        private int         _ClusterSize;
        private int[]       _ClusterMaps;

        private uint        _CRC;

        // Variablen
        private int         _ClusterMapThreshold;
        private int         _NodesPerBlock = -1;

        #region Properties
        /// <summary>
        /// Liefert die Version des Archives zurück.
        /// </summary>
        public int Version
        {
            get
            {
                return _Version;
            }
        }

        /// <summary>
        /// Liefert die Assembly-Build-Version von IDXFS zurück
        /// </summary>
        public int Build
        {
            get
            {
                return _Build;
            }
        }

        /// <summary>
        /// Liefert den Namen des Archives zurück oder legt ihn fest
        /// </summary>
        public string Name
        {
            get
            {
                return FSHelpers.BytesToString(_Name);
            }
            set
            {
                if (this.Name.ToLower() != value.ToLower())
                {
                    _Name = FSHelpers.GetStringBytes(value, 32);
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert den TimeStamp der Erstellung in UTC zurück oder legt ihn fest
        /// </summary>
        public DateTime Created
        {
            get
            {
                return _Created;
            }
            set
            {
                if (_Created != value)
                {
                    _Created = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert den TimeStamp der letzen Änderung in UTC zurück oder legt ihn fest
        /// </summary>
        public DateTime Modified
        {
            get
            {
                return _Modified;
            }
            set
            {
                if (_Modified != value)
                {
                    _Modified = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert die Größe eines Clusters zurück oder legt sie fest.
        /// </summary>
        public int ClusterSize
        {
            get
            {
                return _ClusterSize;
            }
            set
            {
                if (_ClusterSize != value)
                {
                    _ClusterSize = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert die Anzahl an MapClustern zurück.
        /// </summary>
        public int ClusterMapThreshold
        {
            get
            {
                return _ClusterMapThreshold;
            }
        }

        /// <summary>
        /// Liefert die Position eines Clusters zurück.
        /// </summary>
        /// <param name="Index">der Index eines Clusters der zurückgegeben oder zugewiesen werden soll</param>
        /// <returns>-1 wenn der Cluster nicht belegt ist, sonst größer 0</returns>
        public int this[int Index]
        {
            get
            {
                return _ClusterMaps[Index];
            }
            set
            {
                if (_ClusterMaps[Index] != value)
                {
                    _ClusterMaps[Index] = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert die ClusterNummer zurück in welchem das Root-Verzeichnis gespeichert ist.
        /// </summary>
        public int RootCluster
        {
            get
            {
                return ClusterMapThreshold + 1;
            }
        }

        /// <summary>
        /// Liefert den Offset des ersten Clusters zurück.
        /// </summary>
        public int FirstClusterOffset
        {
            get
            {
                return 0x400;
            }
        }

        /// <summary>
        /// Liefert die Größe eines Knoten in Bytes zurück.
        /// </summary>
        public int NodeSize
        {
            get
            {
                return NODE_SIZE;
            }
        }

        /// <summary>
        /// Liefert die maximal für Nutzdaten verwendbare Größe zurück.
        /// </summary>
        public int UseableClusterSize
        {
            get
            {
                return ClusterSize - 16;
            }
        }

        /// <summary>
        /// Liefert die maximale Anzahl an Knoten zurück, welche in einem NodeBlock gespeichert werden können.
        /// </summary>
        public int NodesPerBlock
        {
            get
            {
                if (_NodesPerBlock == -1)
                {
                    _NodesPerBlock = 32;
                    while (((_NodesPerBlock * NodeSize) + ((int)M.Ceiling(_NodesPerBlock / (double)8)) + 4) < _ClusterSize)
                        _NodesPerBlock++;

                    while (((_NodesPerBlock * NodeSize) + ((int)M.Ceiling(_NodesPerBlock / (double)8)) + 4) > _ClusterSize)
                        _NodesPerBlock--;
                }

                return _NodesPerBlock;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an möglichen Positionen pro Cluster zurück.
        /// </summary>
        public int PositionsPerCluster
        {
            get
            {
                return _ClusterSize - 12;
            }
        }
        #endregion

        #region I/O
        /// <summary>
        /// Liest die Daten des Clusters
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public override void Read()
        {
            try
            {
                FSBase.Handle.Position = 0;
                BinaryReader Reader = new BinaryReader(FSBase.Handle);

                if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
                    throw new FSException("signature mismatch");

                if ((_Version = Reader.ReadInt32()) > CURRENT_VERSION)
                    throw new FSException("version mismatch");

                _Build = Reader.ReadInt32();
                _Name = Reader.ReadBytes(32);

                _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
                _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());

                _ClusterSize = Reader.ReadInt32();
                for (int i = 0; i < _ClusterMapThreshold; i++)
                    _ClusterMaps[i] = Reader.ReadInt32();

                _CRC = Reader.ReadUInt32();
                if (ReCRC() != _CRC)
                    throw new FSException("Header CRC mismatch");
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }

        }
        public override void Write()
        {
            try
            {
                FSBase.Handle.Position = 0;
                BinaryWriter Writer = new BinaryWriter(FSBase.Handle);

                Writer.Write(_Signature);

                Writer.Write(_Version);
                Writer.Write(_Build);

                Writer.Write(_Name, 0, 32);

                Writer.Write(_Created.ToFileTimeUtc());
                Writer.Write(_Modified.ToFileTimeUtc());

                Writer.Write(_ClusterSize);
                for (int i = 0; i < _ClusterMapThreshold; i++)
                    Writer.Write(_ClusterMaps[i]);

                _CRC = ReCRC();
                Writer.Write(_CRC);
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Berechnet die Checksumme für den Kopfsatz.
        /// </summary>
        /// <returns>der CRC32 des Kopfsatzes</returns>
        public uint ReCRC()
        {
            var CRC = new CRC();
            CRC.Push(_Signature);

            CRC.Push(_Version);
            CRC.Push(_Build);

            CRC.Push(_Name);

            CRC.Push(_Created.ToFileTimeUtc());
            CRC.Push(_Modified.ToFileTimeUtc());

            CRC.Push(_ClusterSize);

            CRC.Push(_ClusterMapThreshold);
            for (int i = 0; i < _ClusterMapThreshold; i++)
                CRC.Push(_ClusterMaps[i]);

            return CRC.CRC32;
        }
        #endregion

        public FSHeader(FS filesystem, int ClusterSize = 8192)
            : base(filesystem)
        {
            _Signature = DEFAULT_SIGNATURE;

            _Version = CURRENT_VERSION;
            _Build = Assembly.GetEntryAssembly().GetName().Version.Build;

            _Name = FSHelpers.GetStringBytes(Guid.NewGuid().ToString(), 64);

            _Created = DateTime.UtcNow;
            _Modified = DateTime.UtcNow;

            _ClusterSize = ClusterSize;

            _ClusterMaps = new int[_ClusterMapThreshold = MAPCLUSTER_THRESHOLD];
            for (int i = 0; i < _ClusterMaps.Length; i++)
                _ClusterMaps[i] = i;

            _CRC = ReCRC();
        }
    }

    class FSHelpers
    {
        public static byte[] GetStringBytes(string Value, int Length = -1)
        {
            if (Length == -1)
                return System.Text.Encoding.ASCII.GetBytes(Value);
            else
                if (Value.Length > Length)
                return System.Text.Encoding.ASCII.GetBytes(Value.Substring(0, Length));
            else
                return System.Text.Encoding.ASCII.GetBytes(Value.PadRight(Length));
        }
        public static string BytesToString(byte[] Raw)
        {
            return System.Text.Encoding.ASCII.GetString(Raw).TrimEnd();
        }

        public static string FlagsToString(uint Flags)
        {
            return (((Flags & (uint)FSFlags.Archive) == (uint)FSFlags.Archive) ? "A" : "-") +
                    (((Flags & (uint)FSFlags.ReadOnly) == (uint)FSFlags.ReadOnly) ? "R" : "-") +
                    (((Flags & (uint)FSFlags.Hidden) == (uint)FSFlags.Hidden) ? "H" : "-") +
                    (((Flags & (uint)FSFlags.Encrypted) == (uint)FSFlags.Encrypted) ? "E" : "-") +
                    (((Flags & (uint)FSFlags.SymLink) == (uint)FSFlags.SymLink) ? "L" : "-") +
                    (((Flags & (uint)FSFlags.Transformed) == (uint)FSFlags.Transformed) ? "T" : "-") +
                    (((Flags & (uint)FSFlags.SystemUseOnly) == (uint)FSFlags.SystemUseOnly) ? "S" : "-") +
                    (((Flags & (uint)FSFlags.Directory) == (uint)FSFlags.Directory) ? "D" : "-");
        }
    }

    public class FSMap : FSElement
    {
        private byte[]      _Map;

        private int _SlotCount;
        private int _SlotsFree;

        #region Properties
        /// <summary>
        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
        /// </summary>
        /// <param name="Index">Der 0-basierte Index des Clusters</param>
        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
        public bool this[int Index]
        {
            get
            {
                byte Mask = (byte)(1 << (Index & 7));
                return (_Map[Index >> 3] & Mask) == Mask;
            }
            set
            {
                byte Mask = (byte)(1 << (Index & 7));
                bool Used = (_Map[Index >> 3] & Mask) == Mask;

                if (value)
                {
                    if (!Used)
                        _SlotsFree++;

                    _Map[Index >> 3] |= Mask;
                }
                else
                {
                    if (Used)
                        _SlotsFree--;

                    _Map[Index >> 3] &= (byte)~Mask;
                }
            }
        }

        /// <summary>
        /// Liefert die Anzahl an Slot zurück.
        /// </summary>
        public int SlotCount
        {
            get
            {
                return _SlotCount;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an freien Slots zurück.
        /// </summary>
        public int SlotsFree
        {
            get
            {
                return _SlotsFree;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an belegten Slots zurück
        /// </summary>
        public int SlotsUsed
        {
            get
            {
                return _SlotCount - _SlotsFree;
            }
        }

        /// <summary>
        /// Liefert die Größe der Karte in Bytes zurück.
        /// </summary>
        public int MapSize
        {
            get
            {
                return _Map.Length;
            }
        }
        #endregion

        #region I/O
        public override void Read(BinaryReader Reader)
        {
            try
            {
                for (int i = 0; i < _Map.Length; i++)
                {
                    byte t = _Map[i] = Reader.ReadByte();

                    if (t == 0xFF)
                        _SlotsFree -= 8;
                    else
                        for (int j = 0; j < 8; j++, t >>= 1)
                            _SlotsFree -= (byte)(t & 1);
                }
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }

        public override void Write(BinaryWriter Writer)
        {
            try
            {
                for (int i = 0; i < _Map.Length; i++)
                    Writer.Write(_Map[i]);
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        #endregion

        /// <summary>
        /// Ermittelt einen freien Slot und liefert ihn zurück
        /// </summary>
        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
        public int GetFreeSlot()
        {
            if (_SlotsFree == 0)
                return -1;
            else
                for (int i = 0; i < _Map.Length; i++)
                    if (_Map[i] != 0xFF)
                    {
                        int Start = i << 3;
                        for (int j = 0; j < 8; j++)
                            if (!this[Start + j])
                                return Start + j;
                    }

            return -1;
        }

        public FSMap(FS IDXFS, int SlotCount)
            : base(IDXFS)
        {
            _SlotsFree = _SlotCount = SlotCount;
            _Map = new byte[(int)System.Math.Ceiling(SlotCount / (double)8)];
        }
    }

    public class FSNode : FSElement
    {
        // Konstanten
        private const uint  DEFAULT_SIGNATURE = 0xA0BA0700;

        // Vars
        private uint        _Signature;

        private uint        _Id;
        private uint        _Parent;

        private uint        _Flags;

        private byte[]      _Name;

        private int         _FileSize;
        private int         _ClusterCount;

        private DateTime    _Created;
        private DateTime    _Modified;

        private int         _FirstCluster;
        private int         _LastCluster;

        private uint        _CRC;

        #region Properties
        /// <summary>
        /// Liefert die Id des Knoten zurück oder legt sie fest
        /// </summary>
        public uint Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    _Id = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert den Vater des Knoten zurück oder legt ihn fest
        /// </summary>
        public uint Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                if (_Parent != value)
                {
                    _Parent = value;
                    Dirty = true;
                }
            }
        }

        #region Flags
        public uint Flags
        {
            get
            {
                return _Flags;
            }
            set
            {
                if (_Flags != value)
                {
                    Dirty = true;
                    _Flags = value;
                }
            }
        }

        public bool IsHidden
        {
            get
            {
                return (_Flags & (uint)FSFlags.Hidden) == (uint)FSFlags.Hidden;
            }
            set
            {
                Flags |= (int)FSFlags.Hidden;
            }
        }
        public bool IsArchive
        {
            get
            {
                return (_Flags & (uint)FSFlags.Archive) == (uint)FSFlags.Archive;
            }
            set
            {
                Flags |= (int)FSFlags.Archive;
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return (_Flags & (uint)FSFlags.ReadOnly) == (uint)FSFlags.ReadOnly;
            }
            set
            {
                Flags |= (int)FSFlags.ReadOnly;
            }
        }
        public bool IsEncrypted
        {
            get
            {
                return (_Flags & (uint)FSFlags.Encrypted) == (uint)FSFlags.Encrypted;
            }
            set
            {
                Flags |= (int)FSFlags.Encrypted;
            }
        }
        public bool IsSymLink
        {
            get
            {
                return (_Flags & (uint)FSFlags.SymLink) == (uint)FSFlags.SymLink;
            }
            set
            {
                Flags |= (int)FSFlags.SymLink;
            }
        }
        public bool IsSystemUseOnly
        {
            get
            {
                return (_Flags & (uint)FSFlags.SystemUseOnly) == (uint)FSFlags.SystemUseOnly;
            }
            set
            {
                Flags |= (int)FSFlags.SystemUseOnly;
            }
        }
        public bool IsDirectory
        {
            get
            {
                return (_Flags & (uint)FSFlags.Directory) == (uint)FSFlags.Directory;
            }
            set
            {
                Flags |= (int)FSFlags.Directory;
            }
        }
        #endregion

        /// <summary>
        /// Liefert den Namen der Datei zurück oder legt ihn fest
        /// </summary>
        public string Name
        {
            get
            {
                return FSHelpers.BytesToString(_Name);
            }
            set
            {
                if (FSHelpers.BytesToString(_Name) != value)
                {
                    _Name = FSHelpers.GetStringBytes(value, 32);
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert die Größe der Datei zurück oder legt sie fest
        /// </summary>
        public int FileSize
        {
            get
            {
                return _FileSize;
            }
            set
            {
                if (_FileSize != value)
                {
                    _FileSize = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert die Anzahl an Clustern zurück die von der Datei verwendet werden oder legt sie fest.
        /// </summary>
        public int ClusterCount
        {
            get
            {
                return _ClusterCount;
            }
            set
            {
                if (_ClusterCount != value)
                {
                    _ClusterCount = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert das Datum und die Zeit der Anlage zurück oder legt es fest
        /// </summary>
        public DateTime Created
        {
            get
            {
                return _Created;
            }
            set
            {
                if (_Created != value)
                {
                    _Created = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert das Datum und die Zeit der letzten Änderung zurück oder legt es fest
        /// </summary>
        public DateTime Modified
        {
            get
            {
                return _Modified;
            }
            set
            {
                if (_Modified != value)
                {
                    _Modified = value;
                    Dirty = true;
                }
            }
        }

        public int FirstCluster
        {
            get
            {
                return _FirstCluster;
            }
            set
            {
                if (_FirstCluster != value)
                {
                    _FirstCluster = value;
                    Dirty = true;
                }
            }
        }

        public int LastCluster
        {
            get
            {
                return _LastCluster;
            }
            set
            {
                if (_LastCluster != value)
                {
                    _LastCluster = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert den CRC des Knotens zurück oder legt ihn fest.
        /// </summary>
        public uint CRC
        {
            get
            {
                return _CRC;
            }
            set
            {
                if (_CRC != value)
                {
                    _CRC = value;
                    Dirty = true;
                }
            }
        }
        #endregion

        #region I/O
        public override void Read(BinaryReader Reader)
        {
            try
            {
                if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
                    throw new FSException("signature mismatch");

                _Id = Reader.ReadUInt32();
                _Parent = Reader.ReadUInt32();

                _Flags = Reader.ReadUInt32();

                _Name = Reader.ReadBytes(32);

                _FileSize = Reader.ReadInt32();
                _ClusterCount = Reader.ReadInt32();

                try
                {
                    _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
                }
                catch
                {
                    _Created = DateTime.MinValue;
                }
                try
                {
                    _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());
                }
                catch
                {
                    _Modified = DateTime.MinValue;
                }

                _FirstCluster = Reader.ReadInt32();
                _LastCluster = Reader.ReadInt32();

                _CRC = Reader.ReadUInt32();
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        public override void Write(BinaryWriter Writer)
        {
            try
            {
                Writer.Write(_Signature);

                Writer.Write(_Id);
                Writer.Write(_Parent);

                Writer.Write(_Flags);

                Writer.Write(_Name, 0, _Name.Length);

                Writer.Write(_FileSize);
                Writer.Write(_ClusterCount);

                Writer.Write(_Created.ToFileTimeUtc());
                Writer.Write(_Modified.ToFileTimeUtc());

                Writer.Write(_FirstCluster);
                Writer.Write(_LastCluster);

                Writer.Write(_CRC = ReCRC());
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Berechnet die Checksumme für den Knoten.
        /// </summary>
        /// <returns>der CRC32 des Knoten</returns>
        public uint ReCRC()
        {
            var CRC = new CRC();
            CRC.Push(_Signature);
            CRC.Push(_Id);
            CRC.Push(_Parent);
            CRC.Push(_Flags);
            CRC.Push(_Name);
            CRC.Push(_FileSize);
            CRC.Push(_ClusterCount);
            CRC.Push(_Created.ToFileTimeUtc());
            CRC.Push(_Modified.ToFileTimeUtc());

            CRC.Push(_FirstCluster);
            CRC.Push(_LastCluster);

            return CRC.CRC32;
        }
        #endregion

        public FSNode(FS filesystem)
            : base(filesystem)
        {
            _Signature = DEFAULT_SIGNATURE;
            _Name = FSHelpers.GetStringBytes("", 32);

            _Created = DateTime.UtcNow;
            _Modified = DateTime.UtcNow;

            _FirstCluster = -1;
            _LastCluster = -1;

            _CRC = ReCRC();
        }
    }

    public class FSNodeCluster : FSCluster
    {
        // Konstanten
        private const uint  DEFAULT_SIGNATURE       = 0x6CD353FA;

        private uint        _Signature = DEFAULT_SIGNATURE;

        private int         _NextBlock;
        private byte[]      _Reserved;

        private FSMap      _NodeMap;
        private FSNode[]   _Nodes;

        private FS           _IDXFS;

        #region Properties
        /// <summary>
        /// Liefert den ClusterIndex des nächsten Knotens zurück
        /// </summary>
        public int NextBlock
        {
            get
            {
                return _NextBlock;
            }
            set
            {
                if (_NextBlock != value)
                {
                    _NextBlock = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Liefert den Offset der lokalen Karte zurück
        /// </summary>
        private int LocalMapOffset
        {
            get
            {
                return sizeof(uint) + sizeof(int) + _Reserved.Length;
            }
        }

        /// <summary>
        /// Liefert den Offset des ersten Knoten zurück
        /// </summary>
        private int LocalNodeOffset
        {
            get
            {
                return LocalMapOffset + _NodeMap.MapSize;
            }
        }

        /// <summary>
        /// LIefert die Anzahl an freien Knoten zurück
        /// </summary>
        public int NodesFree
        {
            get
            {
                return _NodeMap.SlotsFree;
            }
        }

        /// <summary>
        /// Liefert die Anzahl an belegten Knoten zurück.
        /// </summary>
        public int NodesUsed
        {
            get
            {
                return _NodeMap.SlotsUsed;
            }
        }

        public override bool Dirty
        {
            get
            {
                if (!base.Dirty)
                    for (int i = 0; i < _Nodes.Length; i++)
                        if (_Nodes[i] != null)
                            if (_Nodes[i].Dirty)
                                return true;

                return base.Dirty;
            }
            protected set
            {
                base.Dirty = value;
            }
        }
        #endregion

        #region I/O
        public override void ReadUserData(BinaryReader Reader)
        {
            try
            {
                if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
                    throw new FSException("signature mismatch");

                _NextBlock = Reader.ReadInt32();
                _Reserved = Reader.ReadBytes(_Reserved.Length);

                _NodeMap.Read(Reader);

                // Nodes einlesen...
                byte[] Blank = new byte[FSBase.Header.NodeSize];

                for (int i = 0; i < _Nodes.Length; i++)
                {
                    if (_NodeMap[i])
                    {
                        _Nodes[i] = new FSNode(_IDXFS);
                        _Nodes[i].Read(Reader);
                    }
                    else
                        Reader.Read(Blank, 0, Blank.Length);
                }
            }
            catch (FSException)
            {
                throw;
            }
        }

        public override void WriteUserData(BinaryWriter Writer)
        {
            try
            {
                Writer.Write(_Signature);

                Writer.Write(_NextBlock);
                Writer.Write(_Reserved);

                _NodeMap.Write(Writer);


                byte[] Blank = new byte[FSBase.Header.NodeSize];
                for (int i = 0; i < _Nodes.Length; i++)
                {
                    if (_NodeMap[i])
                        _Nodes[i].Write(Writer);
                    else
                        Writer.Write(Blank);
                }
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
            catch (FSException)
            {
                throw;
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Ermittelt den nächsten freien Slot.
        /// </summary>
        /// <returns>den nächsten freien Slot wenn erfolgreich, sonst -1</returns>
        public int GetFreeSlot()
        {
            return _NodeMap.GetFreeSlot();
        }

        /// <summary>
        /// Erstellt einen neuen Knoten.
        /// </summary>
        /// <returns>ein IDXFSNode wenn erfolgreich, sonst null</returns>
        public FSNode CreateNode(uint NodeId, uint Parent = 0xFFFFFFFF)
        {
            int Slot;
            if ((Slot = GetFreeSlot()) != -1)
            {
                _NodeMap[Slot] = true;
                Dirty = true;

                _Nodes[Slot] = new FSNode(_IDXFS);
                _Nodes[Slot].Id = NodeId;
                _Nodes[Slot].Parent = Parent;

                return _Nodes[Slot];
            }
            else
                return null;
        }

        /// <summary>
        /// Entfernt einen Knoten aus der Auflistung
        /// </summary>
        /// <param name="iNodeId">Die Id des Knoten</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void RemoveNode(uint NodeId)
        {
            for (int i = 0; i < _NodeMap.SlotCount; i++)
                if (_NodeMap[i])
                    if (_Nodes[i].Id == NodeId)
                    {
                        _NodeMap[i] = false;
                        _Nodes[i] = null;

                        Dirty = true;
                        return;
                    }

            throw new FSException("NODE NOT FOUND");
        }

        /// <summary>
        /// Liefert den Knoten mit dem angegebenen Index zurück
        /// </summary>
        /// <param name="Index"></param>
        /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
        public FSNode GetNodeAt(int Index)
        {
            return _NodeMap[Index] ? _Nodes[Index] : null;
        }

        /// <summary>
        /// Liefert den Knoten mit dem angegebenen Namen zurück
        /// </summary>
        /// <param name="Name">Der Name nach dem gesucht werden soll</param>
        /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
        public FSNode FindNode(string Name)
        {
            for (int i = 0; i < _Nodes.Length; i++)
                if (_NodeMap[i])
                    if (_Nodes[i].Name.ToLower() == Name.ToLower())
                        return _Nodes[i];

            return null;
        }

        /// <summary>
        /// Liefert den Knoten mit dem angegebenen Namen zurück
        /// </summary>
        /// <param name="NodeId">Die Id nach der gesucht werden soll</param>
        /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
        public FSNode FindNode(uint NodeId)
        {
            for (int i = 0; i < _Nodes.Length; i++)
                if (_NodeMap[i])
                    if (_Nodes[i].Id == NodeId)
                        return _Nodes[i];

            return null;
        }
        #endregion

        public FSNodeCluster(FS IDXFS, int Cluster)
            : base(IDXFS, Cluster)
        {
            _IDXFS = IDXFS;

            _NodeMap = new FSMap(IDXFS, _IDXFS.Header.NodesPerBlock);
            _NextBlock = -1;

            _Nodes = new FSNode[_IDXFS.Header.NodesPerBlock];
            _Reserved = new byte[_IDXFS.Header.ClusterSize - ((_IDXFS.Header.NodesPerBlock *
                _IDXFS.Header.NodeSize) + _NodeMap.MapSize + 4)];

        }
    }

    public class FSStream : System.IO.Stream
    {
        private FS               _FSBase;
        private FSNode           _Node;

        private FSDataCluster    _CurrentCluster;
        private int             _CurrentIndex;

        private long            _ClusterStart;
        private long            _ClusterEnd;

        private long            _CurrentPosition = 0;

        #region Properties
        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return true; }
        }
        public override bool CanTimeout
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }
        public override long Length
        {
            get { return _Node.FileSize; }
        }
        public override long Position
        {
            get
            {
                return _CurrentPosition;
            }
            set
            {
                _CurrentPosition = value;
            }
        }

        public Stream BaseStream { get { return this; } }

        private string _LastError = "";
        /// <summary>
        /// Liefert den letzten Fehler zurück.
        /// </summary>
        public string LastError
        {
            get
            {
                var Result = _LastError;
                _LastError = "";

                return Result;
            }
            private set
            {
                _LastError = value;
            }
        }
        #endregion

        #region Stream Methods
        public override void Flush()
        {
            try
            {
                _FSBase.Flush();
            }
            catch (FSException)
            {
                // pass through
                throw;
            }

        }
        public override void Close()
        {
            try
            {
                Flush();
                base.Close();
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }

        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int Read = 0, BufferOffset = offset;
            while (Read < count)
            {
                if (!SetClusterMatchPosition(Position))
                    return Read;

                int LoopCount = (int)(_ClusterEnd - _CurrentPosition) + 1;
                if (_ClusterEnd > _Node.FileSize)
                    LoopCount -= (int)(_ClusterEnd - _Node.FileSize) + 1;

                if (LoopCount == 0)
                    return Read;

                int LoopPos = (int)(_CurrentPosition - _ClusterStart);
                int DataCount = ((count - Read) < LoopCount ? count - Read : LoopCount);

                _CurrentCluster.BlockRead(buffer, LoopPos, BufferOffset, DataCount);

                BufferOffset += DataCount;
                Read += DataCount;
                Position += DataCount;
            }
            return Read;
        }

        public override int ReadByte()
        {
            byte[] Result = new byte[1];
            int Read = this.Read(Result, 0, 1);

            if (Read == 0)
                return -1;
            else
                return (int)Result[0];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long NewOffset;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    NewOffset = offset;
                    break;
                case SeekOrigin.Current:
                    NewOffset = _CurrentPosition + offset;
                    break;
                case SeekOrigin.End:
                    NewOffset = _Node.FileSize + offset;
                    break;
                default:
                    NewOffset = 0;
                    break;
            }

            if (NewOffset > Length)
            {
                int NewClusterCount = ClusterCountRequirement(NewOffset);
                if (!EnhanceClustersTo(NewClusterCount))
                    throw new IOException("Oops");
            }

            if (SetClusterMatchPosition(NewOffset))
                return _CurrentPosition = NewOffset;
            else
                throw new IOException();
        }

        public override void SetLength(long value)
        {
            try
            {

                if (value > Length)
                {
                    int NewClusterCount = ClusterCountRequirement(value);
                    if (!EnhanceClustersTo(NewClusterCount))
                        throw new IOException("Oops");
                }
                else
                {
                    int NewClusterCount = ClusterCountRequirement(value);
                    ReduceClustersTo(NewClusterCount);
                }
                _Node.FileSize = (int)value;
            }
            catch (FSException)
            {
                // pass through
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if ((_CurrentPosition + count) > Length)
                SetLength(_CurrentPosition + count);

            if (!SetClusterMatchPosition(_CurrentPosition))
                throw new Exception(LastError);

            int Written = 0, BufferOffset = offset;
            while (Written < count)
            {
                if (!SetClusterMatchPosition(_CurrentPosition))
                    throw new IndexOutOfRangeException();

                int LoopCount = (int)(_ClusterEnd - _CurrentPosition) + 1;

                int LoopPos = (int)(_CurrentPosition - _ClusterStart);
                int DataCount = ((count - Written) < LoopCount ? count - Written : LoopCount);

                _CurrentCluster.BlockWrite(buffer, BufferOffset, LoopPos, DataCount);

                BufferOffset += DataCount;
                Written += DataCount;
                Position += DataCount;
            }
        }

        public override void WriteByte(byte value)
        {
            Write(new byte[] { value }, 0, 1);
        }
        #endregion

        private bool ReadCluster(int FileIndex, int Cluster)
        {
            // read next cluster
            if (Cluster != -1)
            {
                _CurrentCluster = _FSBase.ReadCluster(Cluster);

                _ClusterStart = (FileIndex * _FSBase.Header.UseableClusterSize);
                _ClusterEnd = (FileIndex * _FSBase.Header.UseableClusterSize) + _FSBase.Header.UseableClusterSize - 1;

                _CurrentIndex = FileIndex;
            }
            else
            {
                // position points to an unknown cluster.. wait and see
                _CurrentCluster = null;

                _ClusterStart = (FileIndex * _FSBase.Header.UseableClusterSize);
                _ClusterEnd = (FileIndex * _FSBase.Header.UseableClusterSize) + _FSBase.Header.UseableClusterSize - 1;

                _CurrentIndex = FileIndex;
            }

            return true;
        }

        /// <summary>
        /// Versucht den Cluster aktuellen Cluster zu laden. 
        /// </summary>
        /// <param name="Position">Die Position die den Cluster angibt.</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        private bool SetClusterMatchPosition(long Position)
        {
            while (Position < _ClusterStart)
                if (!ReadCluster(_CurrentIndex - 1, _CurrentCluster.Previous))
                    return false;


            while (Position > _ClusterEnd)
                if (!ReadCluster(_CurrentIndex + 1, _CurrentCluster.Next))
                    return false;

            return true;
        }

        private bool EnhanceClustersTo(int ClusterCount)
        {
            while (_Node.ClusterCount < ClusterCount)
            {
                int Slot = _FSBase.Maps.GetFreeSlot();

                FSDataCluster CurrentCluster; int CurrentClusterSlot = -1;
                // Sonderfall, kein Cluster existiert - Setze FirstCluster
                if (_Node.ClusterCount == 0)
                    _FSBase.Maps[_Node.FirstCluster = _Node.LastCluster = Slot] = true;
                else
                {
                    CurrentCluster = _FSBase.ReadCluster(_Node.LastCluster);
                    CurrentClusterSlot = CurrentCluster.Cluster;

                    _FSBase.Maps[CurrentCluster.Next = _Node.LastCluster = Slot] = true;
                }

                var NewCluster = _FSBase.CreateDataCluster(Slot);
                NewCluster.Previous = CurrentClusterSlot;

                _Node.ClusterCount++;
            }
            if ((_CurrentCluster == null) && (ClusterCount > 0))
                if (!ReadCluster(0, _Node.FirstCluster))
                    return WithError(LastError);

            return true;
        }
        private void ReduceClustersTo(int ClusterCount)
        {
            try
            {
                FSDataCluster CurrentCluster;
                if (_Node.LastCluster == -1)
                    return;
                else
                {
                    int Cluster = _Node.LastCluster, Previous = -1;
                    while (_Node.ClusterCount > ClusterCount)
                    {
                        CurrentCluster = _FSBase.ReadCluster(Cluster);
                        Previous = _Node.LastCluster = CurrentCluster.Previous;

                        _FSBase.ClearCluster(Cluster);

                        _FSBase.Maps[Cluster] = false;
                        _Node.ClusterCount--;

                        Cluster = Previous;
                    }

                    if (_Node.ClusterCount > 0)
                    {
                        CurrentCluster = _FSBase.ReadCluster(_Node.LastCluster);
                        CurrentCluster.Next = -1;
                    }
                    else
                        _Node.FirstCluster = _Node.LastCluster;
                }
            }
            catch (FSException)
            {
                throw;
            }
        }

        public bool WithError(string Error)
        {
            LastError = Error;
            return false;
        }

        private int ClusterCountRequirement(long FileLength)
        {
            return (int)Math.Ceiling(FileLength / (double)_FSBase.Header.UseableClusterSize);
        }

        public FSStream(FS IDXFS, FSNode Node)
            : base()
        {
            _FSBase = IDXFS;

            _Node = Node;
            if (_Node.ClusterCount > 0)
                ReadCluster(0, _Node.FirstCluster);
        }
    }

    public class FSTree
    {
        private FSDirectory _Root;

        #region Properties
        /// <summary>
        /// Liefert das Root-Objekt zurück
        /// </summary>
        public FSDirectory Root
        {
            get
            {
                return _Root;
            }
        }
        #endregion

        /// <summary>
        /// Sucht rekursiv ein Verzeichnis
        /// </summary>
        /// <param name="Id">Die Id nach der gesucht werden soll</param>
        /// <returns>Ein IDXFSDirectory-Objekt wenn erfolgreich, sonst null</returns>
        public FSDirectory FindDirectory(uint Id)
        {
            var Stack = new Stack<FSDirectory>(); FSDirectory Next;
            Stack.Push(_Root);

            while (Stack.Count > 0)
                if ((Next = Stack.Pop()).Id == Id)
                    return Next;
                else
                    foreach (var Item in Next.Directories)
                        if (Item.Id == Id)
                            return Item;
                        else
                            Stack.Push(Item);
            return null;
        }

        public bool InsertNode(FSNode Node)
        {
            return true;
        }

        public bool RemoveNode(FSNode Node)
        {
            return true;
        }

        public FSTree(FSNode Root)
        {
            _Root = new FSDirectory(Root);
        }

        public FSTree(FSDirectory Root)
        {
            _Root = Root;
        }
    }

    public class FS
    {
        public const string    DEFAULT_EXT = ".vfs";

        public struct DirectoryInfo
        {
            public string Name { get; set; }
            public string Attributes { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }
        }

        public struct FileInfo
        {
            public string Name { get; set; }
            public string Attributes { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }
            public int Size { get; set; }
        }

        // Variablen
        private string                  _Filename = "";
        private FileStream              _FileHandle = null;

        private FSHeader                _Header = null;
        private FSClusterMaps           _Maps;
        private List<FSNodeCluster>     _NodeBlocks;

        private FSTree                   _Root;
        private uint                    rootID;

        private FSDirectory             _CurrentFolder;

        private FSCache<FSDataCluster>  _Cache;

        private byte[]              _IDXFS_KEY = {  0x72, 0x7A, 0x62, 0x45, 0x66, 0x5A, 0x55, 0x31,
                                                    0x59, 0x63, 0x32, 0x37, 0x61, 0x44, 0x73, 0x37,
                                                    0x51, 0x75, 0x62, 0x4C, 0x64, 0xA7, 0x71, 0x6F,
                                                    0x67, 0x41, 0x75, 0x43, 0x55, 0x31, 0x75, 0x4B };
        private byte[]              _IDXFS_IV = {   0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8,
                                                    0xD7, 0xE6, 0xF5, 0x04, 0x5F, 0x6E, 0x7D, 0x8C,
                                                    0x9B, 0xAA, 0xB9, 0xC8, 0xD7, 0xE6, 0xF5, 0x04,
                                                    0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8 };

        #region Properties
        /// <summary>
        /// Wahr wenn der Container geöffnet ist, sonst Falsch.
        /// </summary>
        public bool isOpen { get { return (_FileHandle != null); } }

        /// <summary>
        /// Liefert den Datennamen des Containers zurück
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
        }

        /// <summary>
        /// Liefert den vollständigen Pfades des aktuellen Verzeichnisses zurück 
        /// </summary>
        public string FullPath
        {
            get
            {
                var Result = new StringBuilder();

                FSDirectory Current = _CurrentFolder;

                if (Current.Parent == 0xFFFFFFFF)
                    return "\\";
                else
                    while (Current.Parent != 0xFFFFFFFF)
                    {
                        Result.Insert(0, Current.Name + "\\");
                        Current = _Root.FindDirectory(Current.Parent);
                    }

                return Result.ToString();
            }
        }

        /// <summary>
        /// Liefert den Header zurück.
        /// </summary>
        internal FSHeader Header
        {
            get
            {
                return _Header;
            }
        }

        internal FSClusterMaps Maps { get { return _Maps; } }

        internal FileStream Handle { get { return _FileHandle; } }
        #endregion

        #region FS-Helpers
        /// <summary>
        /// Erstellt einen Baum aus den vorhandenen Verzeichnissen und Dateien
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        private bool BuildTree()
        {
            List<FSDirectory> FreeDirectories = new List<FSDirectory>();
            List<FSNode> FreeNodes = new List<FSNode>();

            for (int i = 0; i < _NodeBlocks.Count; i++)
                for (int j = 0; j < _Header.NodesPerBlock; j++)
                {
                    FSNode Node = _NodeBlocks[i].GetNodeAt(j);
                    if (Node != null)
                    {
                        if (Node.IsDirectory)
                            FreeDirectories.Add(new FSDirectory(Node));
                        else
                            FreeNodes.Add(Node);
                    }
                }

            // Assign Files
            for (int i = FreeDirectories.Count - 1; i >= 0; i--)
                for (int j = FreeNodes.Count - 1; j >= 0; j--)
                    if (FreeDirectories[i].Id == FreeNodes[j].Parent)
                    {
                        FreeDirectories[i].Files.Add(FreeNodes[j]);
                        FreeNodes.RemoveAt(j);
                    }

            if (FreeNodes.Count > 0)
                throw new FSException("LOST AND FOUND!");

            // Merge Folders
            while (FreeDirectories.Count > 1)     // root remains
            {
                for (int i = FreeDirectories.Count - 1; i >= 0; i--)
                    for (int j = FreeDirectories.Count - 1; j >= 0; j--)
                        if (FreeDirectories[i].Id == FreeDirectories[j].Parent)
                        {
                            FreeDirectories[i].Directories.Add(FreeDirectories[j]);
                            FreeDirectories.RemoveAt(j);
                        }
            }

            if (FreeDirectories[0].Id != HashFilename("ROOT"))
                throw new FSException("ROOT NOT FOUND!");
            else
                _Root = new FSTree(FreeDirectories[0]);

            return true;
        }

        /// <summary>
        /// Liest die Knotenblöcke aus dem Dateisystem
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void ReadNodeBlocks()
        {
            _NodeBlocks = new List<FSNodeCluster>();

            int ClusterIndex = _Header.RootCluster;
            do
            {
                long BlockOffset = _Header.FirstClusterOffset + (ClusterIndex * _Header.ClusterSize);

                var NodeBlock = new FSNodeCluster(this, ClusterIndex);
                NodeBlock.Read();

                _NodeBlocks.Add(NodeBlock);
                ClusterIndex = NodeBlock.NextBlock;
            } while (ClusterIndex != -1);

            // Sort Blocks
            for (int i = 1; i < _NodeBlocks.Count; i++)
                for (int j = i; j < _NodeBlocks.Count; j++)
                    if ((_NodeBlocks[j].Cluster == _NodeBlocks[i - 1].NextBlock) & (i != j))
                    {
                        var Temp = _NodeBlocks[i];
                        _NodeBlocks[i] = _NodeBlocks[j];
                        _NodeBlocks[j] = Temp;
                    }
        }

        /// <summary>
        /// Schreibt die Knotenblöcke in das Dateisystem
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void WriteNodeBlocks()
        {
            for (int i = 0; i < _NodeBlocks.Count; i++)
                _NodeBlocks[i].Write();
        }

        /// <summary>
        /// Erstellt einen neuen Knotenblock und fügt ihn ans Ende der Liste
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public bool CreateNodeBlock()
        {
            int ClusterSlot = _Maps.GetFreeSlot();
            var NextBlock = new FSNodeCluster(this, ClusterSlot);

            _NodeBlocks[_NodeBlocks.Count - 1].NextBlock = ClusterSlot;
            _Maps[ClusterSlot] = true;

            _NodeBlocks.Add(NextBlock);
            return true;
        }

        /// <summary>
        /// Erstellt einen neuen Knoten im Dateisystem
        /// </summary>
        /// <param name="Node">Das Ziel für das Ergebnis</param>
        /// <param name="Id">Die Id welche dem Knoten zugewiesen werden soll</param>
        /// <param name="Parent">Der Vorgänger des Knotens, 0xFFFF für das Stammverzeichnis</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSNode CreateNode(uint Id, uint Parent = 0xFFFFFFFF)
        {
            for (int i = 0; i < _NodeBlocks.Count; i++)
                if (_NodeBlocks[i].NodesFree > 0)
                    return _NodeBlocks[i].CreateNode(Id, Parent);

            try
            {
                CreateNodeBlock();
            }
            catch (FSException)
            {
                Reload();
                throw;
            }

            return _NodeBlocks[_NodeBlocks.Count - 1].CreateNode(Id, Parent);
        }

        internal void ClearCluster(int Cluster)
        {
            try
            {
                long Offset = _Header.FirstClusterOffset + (Cluster * _Header.ClusterSize);
                byte[] Buffer = new byte[_Header.ClusterSize];

                if (_FileHandle.Seek(Offset, SeekOrigin.Begin) == Offset)
                    _FileHandle.Write(Buffer, 0, Buffer.Length);
            }
            catch (IOException IOe)
            {
                //Log.WriteException(IOe);
                throw new FSException("could not clear cluster", IOe);
            }
        }

        public FSDataCluster ReadCluster(int Cluster)
        {
            FSDataCluster Result = _Cache.Item(Cluster);

            if (Result == null)
            {
                Result = new FSDataCluster(this, Cluster);
                Result.Read();

                _Cache.Append(Result);
            }

            return Result;
        }

        public FSDataCluster CreateDataCluster(int Cluster)
        {
            var Result = new FSDataCluster(this, Cluster);
            _Cache.Append(Result);

            return Result;
        }

        #endregion

        #region FS-Methods
        /// <summary>
        /// Erstellt eine neues Dateisystem
        /// </summary>
        /// <param name="ForceOverwrite">Überschreibt eine vorhandene Datei wenn wahr.</param>
        /// <param name="VolumeName">Der Name des Dateisystems</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Create(bool ForceOverwrite = false, string VolumeName = "")
        {
            Close();

            _Filename = IO.RemoveExtension(Filename);
            if ((File.Exists(this.Filename + DEFAULT_EXT)) & (!ForceOverwrite))
                throw new FSException("file already exists");

            try
            {
                _FileHandle = File.Open(this.Filename + DEFAULT_EXT, FileMode.Create);

                Format(VolumeName);

                _FileHandle.Flush();
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new FSException("could not create file.", IOe);
            }
        }

        /// <summary>
        /// Öffnet ein vorhandenes Verzeichnis
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Open()
        {
            Close();

            _Filename = IO.RemoveExtension(Filename);
            if (!File.Exists(Filename + DEFAULT_EXT))
                throw new FSException("file not found");

            try
            {
                _FileHandle = File.Open(Filename + DEFAULT_EXT, FileMode.Open);

                Reload();

                _FileHandle.Flush();
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new FSException("could not open file", IOe);
            }
        }

        /// <summary>
        /// Schreibt eventuelle Änderungen in das Dateisystem zurück
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Flush()
        {
            if (isOpen)
            {
                if (_Header != null)
                    _Header.Write();

                if (_Maps != null)
                    _Maps.Write();

                if (_NodeBlocks != null)
                    WriteNodeBlocks();

                if (_Cache != null)
                    _Cache.Flush();
            }
        }

        /// <summary>
        /// Schliesst das Dateisystem
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Close()
        {
            if (_FileHandle != null)
            {
                try
                {
                    Flush();

                    _FileHandle.Close();
                    _FileHandle = null;
                }
                catch (IOException IOe)
                {
                    //IDXLogs.WriteException(IOe);
                    throw new FSException("could not close file", IOe);
                }
                finally
                {
                    if (_Header != null)
                        _Header = null;

                    if (_NodeBlocks != null)
                        _NodeBlocks = null;
                }
            }
        }

        /// <summary>
        /// Lädt den Datenbestand erneut vom Dateisystem.
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Reload()
        {
            _Header = new FSHeader(this);
            _Header.Read();

            _Cache = new FSCache<FSDataCluster>(this, 64);

            _Maps = new FSClusterMaps(this);
            _Maps.Read();

            ReadNodeBlocks();

            BuildTree();

            _CurrentFolder = _Root.Root;
        }

        /// <summary>
        /// Weist dem Dateisystem einen neuen Namen zu
        /// </summary>
        /// <param name="Name">Der neue Name für das Dateisystem</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Label(string Name)
        {
            if (isOpen)
                _Header.Name = Name.ToUpper();

            Flush();
        }

        /// <summary>
        /// Liefert den Name des Dateisystems zurück
        /// </summary>
        /// <returns>Der Name des Dateisystems</returns>
        public string GetLabel()
        {
            if (isOpen)
                if (_Header.Name.TrimEnd() != "")
                    return _Header.Name;
                else
                    return Filename;
            else
                return Filename;
        }

        /// <summary>
        /// Formatiert das Dateisystem und legt das Hauptverzeichnis an.
        /// </summary>
        /// <param name="Name">Der Name des Dateisystems</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public void Format(string Name)
        {
            if (isOpen)
            {
                _Header = new FSHeader(this);
                _Header.Name = Name;

                _Cache = new FSCache<FSDataCluster>(this, 32);

                _Maps = new FSClusterMaps(this);
                _Maps.CreateNewClusterMaps();

                _NodeBlocks = new List<FSNodeCluster>() { new FSNodeCluster(this, _Header.RootCluster) };
                _Maps[_Header.RootCluster] = true;  // Root

                var Root = _NodeBlocks[0].CreateNode(rootID);
                Root.Name = "ROOT";
                Root.IsDirectory = true;

                _Root = new FSTree(Root);

                Flush();

                try
                {
                    _FileHandle.SetLength(_Header.FirstClusterOffset + (_Header.ClusterSize * (_Header.ClusterMapThreshold + 1)));
                }
                catch (IOException IOe)
                {
                    //IDXLogs.WriteException(IOe);
                    throw new FSException("couldn't clear Cluster", IOe);
                }

                BuildTree();

                _CurrentFolder = _Root.Root;
            }
        }

        /// <summary>
        /// Erstellt ein neues Verzeichnis im aktuellen
        /// </summary>
        /// <param name="DirectoryName">Der Name des neuen Verzeichnisses</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSDirectory CreateFolder(string DirectoryName)
        {
            if (_CurrentFolder.FindDirectory(DirectoryName) != null)
                throw new FSException("directory already exists");
            else
            {
                try
                {
                    var NewDirectory = CreateNode(HashFilename(FullPath + DirectoryName), _CurrentFolder.Id);

                    NewDirectory.IsDirectory = true;
                    NewDirectory.Name = DirectoryName;

                    var Result = new FSDirectory(NewDirectory);

                    _CurrentFolder.Directories.Add(Result);

                    Flush();

                    return Result;
                }
                catch (FSException IDXFSe)
                {
                    try
                    {
                        Reload();
                        throw;
                    }
                    catch (FSException iIDXFSe)
                    {
                        throw new FSException(iIDXFSe.Message, IDXFSe);
                    }
                }

            }
        }

        /// <summary>
        /// Wechselt in einen Unterordner
        /// </summary>
        /// <param name="DirectoryName">Der Name des vorhandene Verzeichnisses</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSDirectory ChangeToFolder(string DirectoryName)
        {
            var Result = _CurrentFolder.FindDirectory(DirectoryName);
            if (Result == null)
                throw new FSException("directory not found");
            else
                return _CurrentFolder = Result;
        }

        /// <summary>
        /// Wechselt in das root-Verzeichnis
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSDirectory ChangeToRoot()
        {
            var Result = _Root.FindDirectory(rootID);
            if (Result == null)
                throw new FSException("root not found");
            else
                return _CurrentFolder = Result;
        }

        /// <summary>
        /// Wechselt ein Verzeichnis nach oben
        /// </summary>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSDirectory ChangeOneUp()
        {
            if (_CurrentFolder.Id == rootID)
                return _CurrentFolder;
            else
            {
                var Result = _Root.FindDirectory(_CurrentFolder.Parent);
                if (Result == null)
                    throw new FSException("directory not found");
                else
                    return _CurrentFolder = Result;
            }
        }

        /// <summary>
        /// Erstellt eine Liste der in CurrentDirectory enthaltenen Verzeichnissen
        /// </summary>
        /// <returns>Eine Liste mit DirectoryInfo-Objekten</returns>
        public DirectoryInfo[] GetDirectories()
        {
            var Result = new DirectoryInfo[_CurrentFolder.Directories.Count];

            for (int i = 0; i < _CurrentFolder.Directories.Count; i++)
            {
                Result[i] = new DirectoryInfo()
                {
                    Name = _CurrentFolder.Directories[i].Name,
                    Attributes = FSHelpers.FlagsToString(_CurrentFolder.Directories[i].Flags),
                    Created = _CurrentFolder.Directories[i].Created,
                    Modified = _CurrentFolder.Directories[i].Modified
                };
            }

            return Result;
        }

        /// <summary>
        /// Erstellt eine Liste der in CurrentDirectory enthaltenen Dateien
        /// </summary>
        /// <returns>eine Liste von FileInfo-Objekten</returns>
        public FileInfo[] GetFiles()
        {
            var Result = new FileInfo[_CurrentFolder.Files.Count];
            for (int i = 0; i < _CurrentFolder.Files.Count; i++)
            {
                Result[i] = new FileInfo()
                {
                    Name = _CurrentFolder.Files[i].Name,
                    Attributes = FSHelpers.FlagsToString(_CurrentFolder.Files[i].Flags),
                    Modified = _CurrentFolder.Files[i].Created,
                    Size = _CurrentFolder.Files[i].FileSize
                };
            }
            return Result;
        }

        /// <summary>
        /// Erstellt eine Datei mit der Größe 0
        /// </summary>
        /// <param name="Filename">Der Name der Datei</param>
        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
        public FSNode Touch(string Filename)
        {
            var Result = _CurrentFolder.FindFile(Filename);

            if (Result != null)
                throw new FSException("FILE ALREADY EXISTS");
            else
            {
                FSNode NewFile = null;

                try
                {
                    NewFile = CreateNode(HashFilename(FullPath + Filename), _CurrentFolder.Id);

                    NewFile.Name = Filename;
                    _CurrentFolder.Files.Add(NewFile);

                    Flush();

                    return NewFile;
                }
                catch (FSException IDXFSe)
                {
                    try
                    {
                        Reload();
                        throw;
                    }
                    catch (FSException iIDXFSe)
                    {
                        throw new FSException(iIDXFSe.Message, IDXFSe);
                    }
                }
            }
        }

        public FSStream Patch(string SourceFilePath, string DestFilePath, IFSPatchTransform Transform = null)
        {
            if (!File.Exists(SourceFilePath))
                throw new FSException("SOURCEFILE NOT FOUND");

            FSNode DestFileNode = GetFile(DestFilePath);
            if (DestFileNode == null)
                DestFileNode = Touch(DestFilePath);

            bool PatchFile = false;
            byte[] Buffer = new byte[4096];
            int Read = 0;

            if (Transform == null)
            {
                var SourceCRC = new CRC();
                using (FileStream Source = File.Open(SourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    while ((Read = Source.Read(Buffer, 0, Buffer.Length)) > 0)
                        SourceCRC.Push(Buffer, 0, Read);

                    Source.Close();
                }

                PatchFile = (SourceCRC.CRC32 != DestFileNode.CRC);
            }
            else
                PatchFile = true;   // file is transformed, crc-validation is not possible

            if (PatchFile)
            {
                try
                {
                    using (var Dest = GetFileStream(DestFileNode))
                    {
                        using (FileStream Source = File.Open(SourceFilePath, FileMode.Open))
                        {
                            string SourceExtension = IO.ExtensionOnly(SourceFilePath).ToLower();
                            FileStream SourceAtLast = Source;
                            if (Transform != null)
                                if (SourceExtension == ((Transform.Extension.StartsWith(".") ? String.Concat(".", Transform.Extension) : Transform.Extension).ToLower()))
                                    SourceAtLast = Transform.Convert(Source);

                            // convert succeed or skipped
                            Dest.SetLength(0);

                            // go top
                            if (SourceAtLast.Position > 0)
                                SourceAtLast.Position = 0;

                            while ((Read = SourceAtLast.Read(Buffer, 0, Buffer.Length)) > 0)
                                Dest.Write(Buffer, 0, Read);

                            SourceAtLast.Close();

                            Dest.Flush();
                            Dest.Position = 0;

                            return Dest;
                        }
                    }
                }
                catch (IOException IOe)
                {
                    throw new FSException("error while copying file", IOe);
                }
            }
            else
                return GetFileStream(DestFileNode);
        }

        public FSStream Copy(string SourceFile)
        {
            if (!File.Exists(SourceFile))
                throw new FSException("SOURCEFILE NOT FOUND");

            using (var Dest = GetFileStream(Touch(IO.FilenameOnly(SourceFile))))
            {
                try
                {
                    Dest.SetLength(0);
                    using (FileStream Source = File.Open(SourceFile, FileMode.Open))
                    {
                        byte[] Buffer = new byte[4096];
                        int Read = 0;

                        while ((Read = Source.Read(Buffer, 0, Buffer.Length)) > 0)
                            Dest.Write(Buffer, 0, Read);

                        Source.Close();
                    }
                    Dest.Flush();

                    Dest.Position = 0;

                    return Dest;
                }
                catch (IOException IOe)
                {
                    throw new FSException("error while copying file", IOe);
                }
            }
        }

        public void RemoveDirectory(string Name)
        {
            var Directory = _CurrentFolder.FindDirectory(Name);
            if (Directory == null)
                throw new FSException("DIRECTORY NOT FOUND");

            if ((Directory.Files.Count == 0) & (Directory.Directories.Count == 0))
            {
                var Parent = _Root.FindDirectory(Directory.Parent);
                if (!Parent.Directories.Remove(Directory))
                    throw new FSException("UNABLE TO REMOVE DIRECTORY FROM TREE");

                for (int i = 0; i < _NodeBlocks.Count; i++)
                {
                    FSNode Node = _NodeBlocks[i].FindNode(Directory.Id);
                    if (Node != null)
                    {
                        try
                        {
                            _NodeBlocks[i].RemoveNode(Directory.Id);
                            Flush();
                        }
                        catch (FSException IDXFSe)
                        {
                            try
                            {
                                Reload();
                                throw;
                            }
                            catch (FSException iIDXFSe)
                            {
                                throw new FSException(iIDXFSe.Message, IDXFSe);
                            }
                        }
                    }
                    else
                        throw new FSException("UNABLE TO FIND NODE TO REMOVE");
                }
            }
            else
                throw new FSException("DIRECTORY NOT EMPTY");
        }

        public void DeleteFile(string Name)
        {
            try
            {
                FSNode File = _CurrentFolder.FindFile(Name);
                if (File == null)
                    throw new FSException("FILE NOT FOUND");

                using (var FS = GetFileStream(Name))
                    FS.SetLength(0);

                FSNode Node;
                for (int i = 0; i < _NodeBlocks.Count; i++)
                    if ((Node = _NodeBlocks[i].FindNode(File.Id)) != null)
                    {
                        try
                        {
                            _CurrentFolder.Files.Remove(Node);

                            _NodeBlocks[i].RemoveNode(File.Id);
                            Flush();

                            return;
                        }
                        catch (FSException IDXFSe)
                        {
                            try
                            {
                                Reload();
                                throw;
                            }
                            catch (FSException iIDXFSe)
                            {
                                throw new FSException(iIDXFSe.Message, IDXFSe);
                            }
                        }
                    }
            }
            catch (IOException IOe)
            {
                throw new FSException(IOe.Message);
            }
        }

        public FSNode GetFile(string Name)
        {
            if (Name.Contains("\\"))
            {
                FSDirectory Runner = _CurrentFolder;

                // switch to root if name starts with /
                if (Name.StartsWith("\\"))
                    Runner = _Root.Root;

                var Items = Name.ToLower().Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < Items.Length; i++)
                {
                    var Item = Items[i]; var Last = (i == Items.Length - 1);
                    switch (Item)
                    {
                        case ".":   // ignore
                            break;
                        case "..":  // unusual and unperformant.. but.. well, that's okay
                            Runner = _Root.FindDirectory(Runner.Parent);
                            break;
                        default:
                            if (Last)
                            {
                                FSNode File = Runner.FindFile(Item);
                                if (File == null)
                                    throw new FSException("file " + Item + " not found in " + Name);
                                else
                                    return File;
                            }
                            else
                            {
                                var Next = Runner.FindDirectory(Item);
                                if (Next != null)
                                {
                                    Runner = Next;
                                }
                                else
                                    throw new FSException("directory " + Item + " not found in " + Name);
                            }

                            break;
                    }
                }
            }
            else
                return _CurrentFolder.FindFile(Name);

            return null;
        }

        public FSStream GetFileStream(string Name)
        {
            FSNode Node = GetFile(Name);

            if (Node != null)
                return new FSStream(this, Node);
            else
                return null;
        }

        public FSStream GetFileStream(FSNode Node)
        {
            if (Node != null)
                return new FSStream(this, Node);
            else
                return null;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Berechnet einen Hash aus dem Dateinamen
        /// </summary>
        /// <param name="Filename">Der Dateiname</param>
        /// <returns>der Hashwert</returns>
        internal uint HashFilename(string Filename)
        {
            const uint OFFSET   = 2166136261U;
            const uint PRIME    = 16777619;

            uint Result = OFFSET;
            foreach (var Item in Filename)
            {
                Result ^= (byte)Item;
                Result *= PRIME;
            }

            return Result;
        }

        public ICryptoTransform CreateEncryptor()
        {
            var AES = new RijndaelManaged()
            {
                BlockSize = 256,
                KeySize = 256,
                Padding = PaddingMode.Zeros,
                FeedbackSize = 256
            };
            return AES.CreateEncryptor(_IDXFS_KEY, _IDXFS_IV);
        }

        public ICryptoTransform CreateDecryptor()
        {
            var AES = new RijndaelManaged()
            {
                BlockSize = 256,
                KeySize = 256,
                Padding = PaddingMode.Zeros,
                FeedbackSize = 256
            };
            return AES.CreateDecryptor(_IDXFS_KEY, _IDXFS_IV);
        }

        #endregion

        public FS(string Filename)
        {
            _Filename = Filename;
            rootID = HashFilename("ROOT");
        }
    }
    
    public class CRC
    {
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;

        private UInt32 Result;
        private byte[] Buffer;

        public void Push(byte[] Buffer, int Start, int Length)
        {
            unchecked
            {
                int Index = Start;
                for (int i = 0; i < Length; i++)
                    Result = ((Result) >> 8) ^ crc32Table[(Buffer[Index++]) ^ ((Result) & 0x000000FF)];
            }
        }
        public void Push(byte[] Buffer)
        {
            Push(Buffer, 0, Buffer.Length);
        }

        public void Push(byte Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }
        public void Push(int Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }
        public void Push(long Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }

        public UInt32 CRC32 { get { return ~Result; } }

        public CRC()
        {
            unchecked
            {
                // This is the official polynomial used by CRC32 in PKZip.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                UInt32 dwPolynomial = 0xEDB88320;
                UInt32 i, j;

                crc32Table = new UInt32[256];

                UInt32 dwCrc;
                for (i = 0; i < 256; i++)
                {
                    dwCrc = i;
                    for (j = 8; j > 0; j--)
                        if ((dwCrc & 1) == 1)
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        else
                            dwCrc >>= 1;
                    crc32Table[i] = dwCrc;
                }
            }

            Result = 0xFFFFFFFF;
            Buffer = new byte[BUFFER_SIZE];
        }
    }
}
