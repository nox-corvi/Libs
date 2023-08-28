///*
// * Copyright (c) 2014-2018 Anrá aka Nox
// * 
// * This code is licensed under the MIT license (MIT) 
// * 
// * Permission is hereby granted, free of charge, to any person obtaining a copy 
// * of this software and associated documentation files (the "Software"), to deal 
// * in the Software without restriction, including without limitation the rights 
// * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// * copies of the Software, and to permit persons to whom the Software is 
// * furnished to do so, subject to the following conditions:
// * 
// * The above copyright notice and this permission notice shall be included 
// * in all copies or substantial portions of the Software.
// * 
// * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// * THE SOFTWARE.
// * 
//*/
//using Nox.IO.Buffer;
//using Nox.Security;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Reflection;
//using System.Security.Cryptography;
//using System.Text;
//using M = System.Math;

//namespace Nox.IO.DF
//{
//    public abstract class DFObject
//    {
//        public virtual IDF DF { get; } = null!;

//        public DFObject(IDF DF)
//            => this.DF = DF;
//    }

//    public abstract class DFElement
//        : DFObject
//    {
//        public virtual bool Dirty { get; protected set; }

//        public abstract void Read(BinaryReader Reader);

//        public abstract void Write(BinaryWriter Writer);


//        public DFElement(IDF DF)
//            : base(DF)
//        {
//        }
//    }

//    public abstract class DFContainer 
//        : DFObject
//    {
//        public virtual bool Dirty { get; protected set; }

//        public abstract void Read();

//        public abstract void Write();

//        public DFContainer(IDF DF)
//            : base(DF)
//        {
//        }
//    }

//    public abstract class DFCluster 
//        : DFObject
//    {
//        private int _Cluster;

//        #region Properties
//        public int Cluster { get { return _Cluster; } }
//        public virtual bool Dirty { get; protected set; }
//        #endregion

//        public virtual void Read()
//        {
//            try
//            {
//                DF.Handle.Position = (DF.Header.ClusterSize * Cluster) + DF.Header.FirstClusterOffset;

//                var CryptoStream = new CryptoStream(DF.Handle, DF.Laverna.createDecryptorTransformObject(), CryptoStreamMode.Read);
//                BinaryReader Reader = new BinaryReader(CryptoStream);

//                ReadUserData(Reader);
//                Dirty = false;
//            }
//            catch (Exception ex)
//            {
//                DF.Log?.LogException(ex);

//                throw;
//            }
//        }

//        /// <summary>
//        /// Kann überladen werden, um benutzerdefinierte Daten zu lesen
//        /// </summary>
//        /// <param name="Reader">Der BinaryReader von dem gelesen werden soll</param>
//        public abstract void ReadUserData(BinaryReader Reader);

//        /// <summary>
//        /// Schreibt die Daten in die Datei
//        /// </summary>
//        public virtual void Write()
//        {
//            if (Dirty)
//            {
//                try
//                {
//                    DF.Handle.Position = (DF.Header.ClusterSize * Cluster) + DF.Header.FirstClusterOffset;

//                    var CryptoStream = new CryptoStream(DF.Handle, DF.Laverna.createDecryptorTransformObject(), CryptoStreamMode.Write);
//                    BinaryWriter Writer = new BinaryWriter(CryptoStream);

//                    WriteUserData(Writer);

//                    // Leeren des Schreib-Puffers erzwingen
//                    Writer.Flush();

//                    // CryptoStream gefüllt, Puffer leeren
//                    CryptoStream.Flush();

//                    // und abschliessen
//                    CryptoStream.FlushFinalBlock();

//                    Dirty = false;
//                }
//                catch (FSException)
//                {
//                    // pass through
//                    throw;
//                }
//                catch (IOException IOe)
//                {
//                    throw new FSException(IOe.Message);
//                }
//                catch (Exception e)
//                {
//                    throw new FSException(e.Message);
//                }
//            }
//        }

//        /// <summary>
//        /// Kann überladen werden, um benutzerdefinierte Daten zu schreiben
//        /// </summary>
//        /// <param name="Writer">Der BinaryWriter in den geschrieben werden soll</param>
//        public abstract void WriteUserData(BinaryWriter Writer);

//        public DFCluster(IDF DF, int Cluster)
//            : base(DF)
//        {
//            _Cluster = Cluster;
//            Dirty = true;
//        }
//    }

//    public class DFClusterMap : DFCluster
//    {
//        private const int DEFAULT_SIGNATURE = 0x1494BFDA;

//        private int _Signature = DEFAULT_SIGNATURE;
//        private uint[] _Map;

//        // Felder
//        private int _SlotCount;
//        private int _SlotsFree;

//        #region Properties
//        /// <summary>
//        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
//        /// </summary>
//        /// <param name="Index">Der nullbasierte Index des Clusters</param>
//        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
//        public bool this[int Index]
//        {
//            get
//            {
//                uint Mask = (uint)(1 << (Index & 0x1F));
//                return (_Map[Index >> 5] & Mask) == Mask;
//            }
//            set
//            {
//                try
//                {
//                    uint Mask = (uint)(1 << (Index & 0x1F));
//                    bool Used = (_Map[Index >> 5] & Mask) == Mask;

//                    int Modified = (Index >> 5);
//                    if (value)
//                    {
//                        if (!Used)
//                            _SlotsFree--;

//                        _Map[Modified] |= Mask;
//                    }
//                    else
//                    {
//                        if (Used)
//                            _SlotsFree++;

//                        _Map[Modified] &= (uint)~Mask;
//                    }

//                    Dirty = true;
//                }
//                catch (Exception e)
//                {
//                    throw new FSException(e.Message);
//                }
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an Slot zurück.
//        /// </summary>
//        public int SlotCount
//        {
//            get
//            {
//                return _SlotCount;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an freien Slots zurück.
//        /// </summary>
//        public int SlotsFree
//        {
//            get
//            {
//                return _SlotsFree;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an belegten Slots zurück
//        /// </summary>
//        public int SlotsUsed
//        {
//            get
//            {
//                return _SlotCount - _SlotsFree;
//            }
//        }
//        #endregion

//        #region I/O
//        public override void ReadUserData(BinaryReader Reader)
//        {
//            try
//            {
//                if ((_Signature = Reader.ReadInt32()) != DEFAULT_SIGNATURE)
//                    throw new FSException("signature mismatch");

//                for (int i = 0; i < _Map.Length; i++)
//                {
//                    uint r = _Map[i] = Reader.ReadUInt32();

//                    if (r == 0xFFFFFFFF)
//                        _SlotsFree -= 32;
//                    else
//                        for (int k = 0; k < 32; k++, r >>= 1)
//                            _SlotsFree -= (byte)(r & 1);
//                }
//            }
//            catch (IOException IOe)
//            {
//                throw new FSException(IOe.Message);
//            }
//        }

//        public override void WriteUserData(BinaryWriter Writer)
//        {
//            try
//            {
//                Writer.Write(_Signature);
//                for (int i = 0; i < _Map.Length; i++)
//                    Writer.Write(_Map[i]);
//            }
//            catch (IOException IOe)
//            {
//                throw new FSException(IOe.Message);
//            }
//        }
//        #endregion

//        /// <summary>
//        /// Ermittelt einen freien Slot und liefert ihn zurück
//        /// </summary>
//        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
//        public int GetFreeSlot()
//        {
//            if (_SlotsFree == 0)
//                return -1;
//            else
//            {
//                for (int i = 0; i < _Map.Length; i++)
//                    if (_Map[i] != 0xFFFFFFFF)
//                    {
//                        int Start = i << 5;
//                        for (int j = 0; j < 32; j++)
//                            if (!this[Start + j])
//                                return Start + j;
//                    }
//            }

//            return -1;
//        }

//        public DFClusterMap(DF.DF DF, int Cluster)
//            : base(DF, Cluster)
//        {
//            _SlotsFree = _SlotCount = ((DF.Header.ClusterSize - 8) << 3);
//            _Map = new uint[_SlotCount >> 5];
//        }
//    }

//    public class DFClusterMaps : DFContainer
//    {
//        private DFClusterMap[] _Map;

//        #region Properties
//        /// <summary>
//        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
//        /// </summary>
//        /// <param name="Index">Der 0-basierte Index des Clusters</param>
//        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
//        public bool this[int Index]
//        {
//            get
//            {
//                int Map = 0, R = Index;
//                while (R >= _Map[Map].SlotCount)
//                    R -= _Map[Map++].SlotCount;

//                return _Map[Map][R];
//            }
//            set
//            {
//                int Map = 0, R = Index;
//                while (R >= _Map[Map].SlotCount)
//                    R -= _Map[Map++].SlotCount;

//                _Map[Map][R] = value;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an Slot zurück.
//        /// </summary>
//        public int SlotCount
//        {
//            get
//            {
//                int Result = 0;
//                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
//                    Result += _Map[i].SlotCount;

//                return Result;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an freien Slots zurück.
//        /// </summary>
//        public int SlotsFree
//        {
//            get
//            {
//                int Result = 0;
//                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
//                    Result += _Map[i].SlotsFree;

//                return Result;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an belegten Slots zurück
//        /// </summary>
//        public int SlotsUsed
//        {
//            get
//            {
//                int Result = 0;
//                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
//                    Result += _Map[i].SlotsUsed;

//                return Result;
//            }
//        }

//        /// <summary>
//        /// Liefert zurück ob die Karte geändert worde ist
//        /// </summary>
//        public override bool Dirty
//        {
//            get
//            {
//                var Result = base.Dirty;

//                for (int i = 0; i < FSBase.Header.ClusterMapThreshold; i++)
//                    Result |= _Map[i].Dirty;

//                return Result;
//            }
//            protected set
//            {
//                base.Dirty = value;
//            }
//        }
//        #endregion

//        /// <summary>
//        /// Liest die ClusterMap vom Dateisystem
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public override void Read()
//        {

//            try
//            {
//                _Map = new DFClusterMap[DF.Header.ClusterMapThreshold];
//                for (int i = 0; i < DF.Header.ClusterMapThreshold; i++)
//                {
//                    _Map[i] = new DFClusterMap(DF, DF.Header[i]);
//                    _Map[i].Read();
//                }
//            }
//            catch (DFException)
//            {
//                // pass through
//                throw;
//            }
//        }

//        /// <summary>
//        /// Schreibt die ClusterMap in das Dateisystem
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public override void Write()
//        {
//            try
//            {
//                for (int i = 0; i < DF.Header.ClusterMapThreshold; i++)
//                    _Map[i].Write();
//            }
//            catch (DFException)
//            {
//                // pass through
//                throw;
//            }
//        }

//        /// <summary>
//        /// Ermittelt einen freien Slot und liefert ihn zurück
//        /// </summary>
//        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
//        public int GetFreeSlot()
//        {
//            int Base = 0;
//            for (int i = 0; i < DF.Header.ClusterMapThreshold; i++)
//            {
//                if (_Map[i].SlotsFree > 0)
//                    return Base + _Map[i].GetFreeSlot();

//                Base += _Map[i].SlotCount;
//            }

//            return -1;
//        }

//        /// <summary>
//        /// Erstellt die ClusterMaps neu und schreibt sie auf den Datenträger
//        /// </summary>
//        /// <returns></returns>
//        public void CreateNewClusterMaps()
//        {
//            try
//            {
//                for (int i = 0; i < DF.Header.ClusterMapThreshold; i++)
//                {
//                    _Map[i] = new DFClusterMap(DF, DF.Header[i]);
//                    _Map[i].Write();

//                    DF.Maps[_Map[i].Cluster] = true;
//                }
//            }
//            catch (DFException)
//            {
//                // pass through
//                throw;
//            }
//        }

//        public DFClusterMaps(DF DF)
//            : base(DF)
//        {
//            _Map = new DFClusterMap[DF.Header.ClusterMapThreshold];
//        }
//    }

//    public class DFDataCluster 
//        : DFCluster
//    {
//        private const uint DEFAULT_SIGNATURE = 0xFAD53F33;

//        private uint _Signature;

//        private int _Previous;
//        private int _Next;

//        private byte[] _Data;
//        private uint _CRC;

//        #region Properties
//        /// <summary>
//        /// Liefert ein Byte aus dem Datenpuffer zurück oder legt es fest
//        /// </summary>
//        /// <param name="Index">Der Index an dem das Byte gelesen oder geschrieben werden soll</param>
//        /// <returns>Das gelesene Byte</returns>
//        public byte this[int Index]
//        {
//            get
//            {
//                return _Data[Index];
//            }
//            set
//            {
//                _Data[Index] = value;
//                Dirty = true;
//            }
//        }

//        public int Previous
//        {
//            get
//            {
//                return _Previous;
//            }
//            set
//            {
//                if (_Previous != value)
//                {
//                    _Previous = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int Next
//        {
//            get
//            {
//                return _Next;
//            }
//            set
//            {
//                if (_Next != value)
//                {
//                    _Next = value;
//                    Dirty = true;
//                }
//            }
//        }
//        #endregion

//        #region I/O
//        public override void ReadUserData(BinaryReader Reader)
//        {
//            try
//            {
//                if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
//                    throw new DFException("signature mismatch");

//                _Previous = Reader.ReadInt32();
//                _Next = Reader.ReadInt32();

//                int DataRead = Reader.Read(_Data, 0, _Data.Length);

//                _CRC = Reader.ReadUInt32();
//            }
//            catch (IOException IOe)
//            {
//                throw new DFException(IOe.Message);
//            }
//        }

//        public override void WriteUserData(BinaryWriter Writer)
//        {
//            try
//            {
//                Writer.Write(_Signature);

//                Writer.Write(_Previous);
//                Writer.Write(_Next);

//                Writer.Write(_Data, 0, _Data.Length);
//                Writer.Write(_CRC = ReCRC());

//                Dirty = false;
//            }
//            catch (IOException IOe)
//            {
//                throw new DFException(IOe.Message);
//            }
//        }
//        #endregion

//        public int BlockRead(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
//        {
//            int Read = (_Data.Length - SourceOffset);

//            if (Read > Length)
//                Read = Length;

//            try
//            {
//                Array.Copy(_Data, SourceOffset, Buffer, DestOffset, Read);
//            }
//            catch
//            {
//                throw new DFException("array read-access troubles");
//            }

//            return Read;
//        }

//        public int BlockWrite(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
//        {
//            int Read = (_Data.Length - SourceOffset);

//            if (Read > _Data.Length)
//                Read = Length;

//            try
//            {
//                Array.Copy(Buffer, SourceOffset, _Data, DestOffset, Length);
//                Dirty = true;
//            }
//            catch
//            {
//                throw new Exception("array write-access troubles");
//            }


//            return Read;
//        }

//        #region Helpers
//        /// <summary>
//        /// Berechnet die Checksumme für den Cluster.
//        /// </summary>
//        /// <returns>der CRC32 des Knoten</returns>
//        public uint ReCRC()
//        {
//            var CRC = new tinyCRC();

//            CRC.Push(_Signature);
//            CRC.Push(_Data, 0, _Data.Length);

//            return CRC.CRC32;
//        }
//        #endregion

//        public DFDataCluster(DF DF, int Cluster)
//            : base(DF, Cluster)
//        {
//            _Signature = DEFAULT_SIGNATURE;

//            _Data = new byte[DF.Header.UseableClusterSize];

//            _CRC = ReCRC();
//        }

//        ~DFDataCluster()
//        {
//            try
//            {
//                if (Dirty)
//                    Write();
//            }
//            catch (DFException)
//            {
//                // pass through
//                throw;
//            }
//        }
//    }

//    public class IHeader
//    {

//    }

//    public abstract class DFHeader
//        : DFContainer
//    {
//        public virtual uint DefaultSignature { get; } = 0x3F534652;
//        public virtual uint CurrentVersion { get; } = 0x10A0;
//        public virtual int MapClusterThreshold { get; } = 4;

//        // Felder
//        private uint _Signature;

//        private uint _Version;
//        private int _Build;

//        private byte[] _Name;

//        private DateTime _Created;
//        private DateTime _Modified;

//        private int _ContainerCount;
//        private int[] _ContainerSizes;

//        private int _ClusterMapCount;
//        private int[] _ClusterMaps;

//        private int _ClusterSize;

//        private uint _CRC;

//        // Variablen
//        private int _ClusterMapThreshold;
//        private int _NodesPerBlock = -1;

//        #region Properties
//        public uint Version { get => _Version; }

//        public int Build { get => _Build; }

//        public string Name
//        {
//            get
//            {
//                return DFHelpers.BytesToString(_Name);
//            }
//            set
//            {
//                if (this.Name.ToLower() != value.ToLower())
//                {
//                    _Name = DFHelpers.GetStringBytes(value, 32);
//                    Dirty = true;
//                }
//            }
//        }

//        public DateTime Created
//        {
//            get => _Created;
//            set
//            {
//                if (_Created != value)
//                {
//                    _Created = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public DateTime Modified
//        {
//            get => _Modified;
//            set
//            {
//                if (_Modified != value)
//                {
//                    _Modified = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int ContainerCount
//        {
//            get => _ContainerCount;
//            set
//            {
//                if (_ContainerCount != value)
//                {
//                    _ContainerCount = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int[] ContainerSizes
//        {
//            get => _ContainerSizes;
//            set
//            {
//                if (_ContainerSizes != value)
//                {
//                    _ContainerSizes = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int ClusterMapCount
//        {
//            get => _ClusterMapCount;
//            set
//            {
//                if (_ClusterMapCount != value)
//                {
//                    _ClusterMapCount = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int this[int Index]
//        {
//            get => _ClusterMaps[Index];
//            set
//            {
//                if (_ClusterMaps[Index] != value)
//                {
//                    _ClusterMaps[Index] = value;
//                    Dirty = true;
//                }
//            }
//        }


//        public int ClusterSize
//        {
//            get
//            {
//                return _ClusterSize;
//            }
//            set
//            {
//                if (_ClusterSize != value)
//                {
//                    _ClusterSize = value;
//                    Dirty = true;
//                }
//            }
//        }

//        public int ClusterMapThreshold { get => _ClusterMapThreshold; }

//        public int RootCluster
//        {
//            get
//            {
//                return ClusterMapCount + 1;
//            }
//        }

//        public int FirstClusterOffset { get => 0x400; }

//        public int UseableClusterSize
//        {
//            get
//            {
//                return ClusterSize - 16;
//            }
//        }

//        ///// <summary>
//        ///// Liefert die maximale Anzahl an Knoten zurück, welche in einem NodeBlock gespeichert werden können.
//        ///// </summary>
//        //public int NodesPerBlock
//        //{
//        //    get
//        //    {
//        //        if (_NodesPerBlock == -1)
//        //        {
//        //            _NodesPerBlock = 32;
//        //            while (((_NodesPerBlock * NodeSize) + ((int)M.Ceiling(_NodesPerBlock / (double)8)) + 4) < _ClusterSize)
//        //                _NodesPerBlock++;

//        //            while (((_NodesPerBlock * NodeSize) + ((int)M.Ceiling(_NodesPerBlock / (double)8)) + 4) > _ClusterSize)
//        //                _NodesPerBlock--;
//        //        }

//        //        return _NodesPerBlock;
//        //    }
//        //}
//        #endregion

//        #region I/O
//        public override void Read()
//        {
//            try
//            {
//                DF.Handle.Position = 0;
//                BinaryReader Reader = new BinaryReader(DF.Handle);

//                if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
//                    throw new InvalidDataException("signature mismatch");

//                if ((_Version = Reader.ReadUInt32()) > CurrentVersion)
//                    throw new InvalidDataException("version mismatch");

//                _Build = Reader.ReadInt32();
//                _Name = Reader.ReadBytes(32);

//                _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
//                _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());

//                _ContainerCount = Reader.ReadInt32();
//                _ContainerSizes = new int[_ContainerCount];
//                for (int i = 0; i < _ContainerCount; i++)
//                    _ContainerSizes[i] = Reader.ReadInt32();

//                _ClusterMapCount = Reader.ReadInt32();

//                _ClusterSize = Reader.ReadInt32();
//                for (int i = 0; i < _ClusterMapThreshold; i++)
//                    _ClusterMaps[i] = Reader.ReadInt32();

//                _CRC = Reader.ReadUInt32();
//                if (ReCRC() != _CRC)
//                    throw new InvalidDataException("Header CRC mismatch");
//            }
//            catch (IOException IOe)
//            {
//                throw;
//            }

//        }
//        public override void Write()
//        {
//            try
//            {
//                DF.Handle.Position = 0;
//                BinaryWriter Writer = new BinaryWriter(DF.Handle);

//                Writer.Write(_Signature);

//                Writer.Write(_Version);
//                Writer.Write(_Build);

//                Writer.Write(_Name, 0, 32);

//                Writer.Write(_Created.ToFileTimeUtc());
//                Writer.Write(_Modified.ToFileTimeUtc());

//                Writer.Write(_ClusterSize);
//                for (int i = 0; i < _ClusterMapThreshold; i++)
//                    Writer.Write(_ClusterMaps[i]);

//                _CRC = ReCRC();
//                Writer.Write(_CRC);
//            }
//            catch (IOException IOe)
//            {
//                throw;
//            }
//        }
//        #endregion

//        #region Helpers
//        /// <summary>
//        /// Berechnet die Checksumme für den Kopfsatz.
//        /// </summary>
//        /// <returns>der CRC32 des Kopfsatzes</returns>
//        public uint ReCRC()
//        {
//            var CRC = new tinyCRC();
//            CRC.Push(_Signature);

//            CRC.Push(_Version);
//            CRC.Push(_Build);

//            CRC.Push(_Name);

//            CRC.Push(_Created.ToFileTimeUtc());
//            CRC.Push(_Modified.ToFileTimeUtc());

//            CRC.Push(_ClusterSize);

//            CRC.Push(_ClusterMapThreshold);
//            for (int i = 0; i < _ClusterMapThreshold; i++)
//                CRC.Push(_ClusterMaps[i]);

//            return CRC.CRC32;
//        }
//        #endregion

//        public DFHeader(IDF DF, int ClusterSize = 8192)
//            : base(DF)
//        {
//            _Signature = DefaultSignature;

//            _Version = CurrentVersion;
//            _Build = Assembly.GetEntryAssembly().GetName().Version.Build;

//            _Name = DFHelpers.GetStringBytes(Guid.NewGuid().ToString(), 64);

//            _Created = DateTime.UtcNow;
//            _Modified = DateTime.UtcNow;

//            _ClusterSize = ClusterSize;

//            _ClusterMaps = new int[_ClusterMapThreshold = MapClusterThreshold];
//            for (int i = 0; i < _ClusterMaps.Length; i++)
//                _ClusterMaps[i] = i;

//            _CRC = ReCRC();
//        }
//    }

//    public class DFMap : DFElement
//    {
//        private byte[] _Map;

//        private int _SlotCount;
//        private int _SlotsFree;

//        #region Properties
//        /// <summary>
//        /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
//        /// </summary>
//        /// <param name="Index">Der 0-basierte Index des Clusters</param>
//        /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
//        public bool this[int Index]
//        {
//            get
//            {
//                byte Mask = (byte)(1 << (Index & 7));
//                return (_Map[Index >> 3] & Mask) == Mask;
//            }
//            set
//            {
//                byte Mask = (byte)(1 << (Index & 7));
//                bool Used = (_Map[Index >> 3] & Mask) == Mask;

//                if (value)
//                {
//                    if (!Used)
//                        _SlotsFree++;

//                    _Map[Index >> 3] |= Mask;
//                }
//                else
//                {
//                    if (Used)
//                        _SlotsFree--;

//                    _Map[Index >> 3] &= (byte)~Mask;
//                }
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an Slot zurück.
//        /// </summary>
//        public int SlotCount
//        {
//            get
//            {
//                return _SlotCount;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an freien Slots zurück.
//        /// </summary>
//        public int SlotsFree
//        {
//            get
//            {
//                return _SlotsFree;
//            }
//        }

//        /// <summary>
//        /// Liefert die Anzahl an belegten Slots zurück
//        /// </summary>
//        public int SlotsUsed
//        {
//            get
//            {
//                return _SlotCount - _SlotsFree;
//            }
//        }

//        /// <summary>
//        /// Liefert die Größe der Karte in Bytes zurück.
//        /// </summary>
//        public int MapSize
//        {
//            get
//            {
//                return _Map.Length;
//            }
//        }
//        #endregion

//        #region I/O
//        public override void Read(BinaryReader Reader)
//        {
//            try
//            {
//                for (int i = 0; i < _Map.Length; i++)
//                {
//                    byte t = _Map[i] = Reader.ReadByte();

//                    if (t == 0xFF)
//                        _SlotsFree -= 8;
//                    else
//                        for (int j = 0; j < 8; j++, t >>= 1)
//                            _SlotsFree -= (byte)(t & 1);
//                }
//            }
//            catch (IOException IOe)
//            {
//                throw new FSException(IOe.Message);
//            }
//        }

//        public override void Write(BinaryWriter Writer)
//        {
//            try
//            {
//                for (int i = 0; i < _Map.Length; i++)
//                    Writer.Write(_Map[i]);
//            }
//            catch (IOException IOe)
//            {
//                throw new FSException(IOe.Message);
//            }
//        }
//        #endregion

//        /// <summary>
//        /// Ermittelt einen freien Slot und liefert ihn zurück
//        /// </summary>
//        /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
//        public int GetFreeSlot()
//        {
//            if (_SlotsFree == 0)
//                return -1;
//            else
//                for (int i = 0; i < _Map.Length; i++)
//                    if (_Map[i] != 0xFF)
//                    {
//                        int Start = i << 3;
//                        for (int j = 0; j < 8; j++)
//                            if (!this[Start + j])
//                                return Start + j;
//                    }

//            return -1;
//        }

//        public DFMap(FS IDXFS, int SlotCount)
//            : base(IDXFS)
//        {
//            _SlotsFree = _SlotCount = SlotCount;
//            _Map = new byte[(int)System.Math.Ceiling(SlotCount / (double)8)];
//        }
//    }

//    public interface IDF
//        : IDisposable
//    {
//        bool Encrypt { get; }

//        Log4 Log { get; }
//        Laverna Laverna { get; }

//        IHeader Header { get; }

//        FileStream Handle { get; }
//    }

//    public class DF<T>
//        : IDF 
//        where T : IHeader, new()
//    {
//        public string DEFAULT_EXT = ".dfs";

//        // Variablen
//        private Log4 _Log = null!;
//        private Laverna _Laverna = null!;

//        private string _Filename = "";
//        private FileStream _FileHandle = null;

//        private IHeader _Header = null;
//        private DFCache<DFDataCluster> _Cache;

//        #region Properties
//        /// <summary>
//        /// Wahr wenn der Container geöffnet ist, sonst Falsch.
//        /// </summary>
//        public bool isOpen { get { return (_FileHandle != null); } }
         
//        /// <summary>
//        /// Liefert den Datennamen des Containers zurück
//        /// </summary>
//        public string Filename
//        {
//            get
//            {
//                return _Filename;
//            }
//        }

//        /// <summary>
//        /// Liefert den Header zurück.
//        /// </summary>
//        internal IHeader Header
//        {
//            get
//            {
//                return _Header;
//            }
//        }

//        internal FileStream Handle { get { return _FileHandle; } }
//        #endregion

//        #region FS-Helpers
        
//        internal void ClearCluster(int Cluster)
//        {
//            try
//            {
//                long Offset = _Header.FirstClusterOffset + (Cluster * _Header.ClusterSize);
//                byte[] Buffer = new byte[_Header.ClusterSize];

//                if (_FileHandle.Seek(Offset, SeekOrigin.Begin) == Offset)
//                    _FileHandle.Write(Buffer, 0, Buffer.Length);
//            }
//            catch (IOException IOe)
//            {
//                //Log.WriteException(IOe);
//                throw new FSException("could not clear cluster", IOe);
//            }
//        }

//        public FSDataCluster ReadCluster(int Cluster)
//        {
//            FSDataCluster Result = _Cache.Item(Cluster);

//            if (Result == null)
//            {
//                Result = new FSDataCluster(this, Cluster);
//                Result.Read();

//                _Cache.Append(Result);
//            }

//            return Result;
//        }

//        public FSDataCluster CreateDataCluster(int Cluster)
//        {
//            var Result = new FSDataCluster(this, Cluster);
//            _Cache.Append(Result);

//            return Result;
//        }

//        #endregion

//        #region FS-Methods
//        /// <summary>
//        /// Erstellt eine neues Dateisystem
//        /// </summary>
//        /// <param name="ForceOverwrite">Überschreibt eine vorhandene Datei wenn wahr.</param>
//        /// <param name="VolumeName">Der Name des Dateisystems</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Create(bool ForceOverwrite = false, string VolumeName = "")
//        {
//            Close();

//            _Filename = Filename.RemoveExtension();
//            if ((File.Exists(this.Filename + DEFAULT_EXT)) & (!ForceOverwrite))
//                throw new FSException("file already exists");

//            try
//            {
//                _FileHandle = File.Open(this.Filename + DEFAULT_EXT, FileMode.Create);

//                Format(VolumeName);

//                _FileHandle.Flush();
//            }
//            catch (IOException IOe)
//            {
//                //IDXLogs.WriteException(IOe);
//                throw new FSException("could not create file.", IOe);
//            }
//        }

//        /// <summary>
//        /// Öffnet ein vorhandenes Verzeichnis
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Open()
//        {
//            Close();

//            _Filename = Filename.RemoveExtension();
//            if (!File.Exists(Filename + DEFAULT_EXT))
//                throw new FSException("file not found");

//            try
//            {
//                _FileHandle = File.Open(Filename + DEFAULT_EXT, FileMode.Open);

//                Reload();

//                _FileHandle.Flush();
//            }
//            catch (IOException IOe)
//            {
//                //IDXLogs.WriteException(IOe);
//                throw new FSException("could not open file", IOe);
//            }
//        }

//        /// <summary>
//        /// Schreibt eventuelle Änderungen in das Dateisystem zurück
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Flush()
//        {
//            if (isOpen)
//            {
//                if (_Header != null)
//                    _Header.Write();

//                if (_Maps != null)
//                    _Maps.Write();

//                if (_NodeBlocks != null)
//                    WriteNodeBlocks();

//                if (_Cache != null)
//                    _Cache.Flush();
//            }
//        }

//        /// <summary>
//        /// Schliesst das Dateisystem
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Close()
//        {
//            if (_FileHandle != null)
//            {
//                try
//                {
//                    Flush();

//                    _FileHandle.Close();
//                    _FileHandle = null;
//                }
//                catch (IOException IOe)
//                {
//                    //IDXLogs.WriteException(IOe);
//                    throw new FSException("could not close file", IOe);
//                }
//                finally
//                {
//                    if (_Header != null)
//                        _Header = null;

//                    if (_NodeBlocks != null)
//                        _NodeBlocks = null;
//                }
//            }
//        }

//        /// <summary>
//        /// Lädt den Datenbestand erneut vom Dateisystem.
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Reload()
//        {
//            _Header = new FSHeader(this);
//            _Header.Read();

//            _Cache = new FSCache<FSDataCluster>(this, 64);

//            _Maps = new FSClusterMaps(this);
//            _Maps.Read();

//            ReadNodeBlocks();

//            BuildTree();

//            _CurrentFolder = _Root.Root;
//        }

//        /// <summary>
//        /// Weist dem Dateisystem einen neuen Namen zu
//        /// </summary>
//        /// <param name="Name">Der neue Name für das Dateisystem</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Label(string Name)
//        {
//            if (isOpen)
//                _Header.Name = Name.ToUpper();

//            Flush();
//        }

//        /// <summary>
//        /// Liefert den Name des Dateisystems zurück
//        /// </summary>
//        /// <returns>Der Name des Dateisystems</returns>
//        public string GetLabel()
//        {
//            if (isOpen)
//                if (_Header.Name.TrimEnd() != "")
//                    return _Header.Name;
//                else
//                    return Filename;
//            else
//                return Filename;
//        }

//        /// <summary>
//        /// Formatiert das Dateisystem und legt das Hauptverzeichnis an.
//        /// </summary>
//        /// <param name="Name">Der Name des Dateisystems</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Format(string Name)
//        {
//            if (isOpen)
//            {
//                _Header = new FSHeader(this);
//                _Header.Name = Name;

//                _Cache = new FSCache<FSDataCluster>(this, 32);

//                _Maps = new FSClusterMaps(this);
//                _Maps.CreateNewClusterMaps();

//                _NodeBlocks = new List<FSNodeCluster>() { new FSNodeCluster(this, _Header.RootCluster) };
//                _Maps[_Header.RootCluster] = true;  // Root

//                var Root = _NodeBlocks[0].CreateNode(rootID);
//                Root.Name = "ROOT";
//                Root.IsDirectory = true;

//                _Root = new FSTree(Root);

//                Flush();

//                try
//                {
//                    _FileHandle.SetLength(_Header.FirstClusterOffset + (_Header.ClusterSize * (_Header.ClusterMapThreshold + 1)));
//                }
//                catch (IOException IOe)
//                {
//                    //IDXLogs.WriteException(IOe);
//                    throw new FSException("couldn't clear Cluster", IOe);
//                }

//                BuildTree();

//                _CurrentFolder = _Root.Root;
//            }
//        }

//        /// <summary>
//        /// Erstellt ein neues Verzeichnis im aktuellen
//        /// </summary>
//        /// <param name="DirectoryName">Der Name des neuen Verzeichnisses</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public FSDirectory CreateFolder(string DirectoryName)
//        {
//            if (_CurrentFolder.FindDirectory(DirectoryName) != null)
//                throw new FSException("directory already exists");
//            else
//            {
//                try
//                {
//                    var NewDirectory = CreateNode(HashFilename(FullPath + DirectoryName), _CurrentFolder.Id);

//                    NewDirectory.IsDirectory = true;
//                    NewDirectory.Name = DirectoryName;

//                    var Result = new FSDirectory(NewDirectory);

//                    _CurrentFolder.Directories.Add(Result);

//                    Flush();

//                    return Result;
//                }
//                catch (FSException IDXFSe)
//                {
//                    try
//                    {
//                        Reload();
//                        throw;
//                    }
//                    catch (FSException iIDXFSe)
//                    {
//                        throw new FSException(iIDXFSe.Message, IDXFSe);
//                    }
//                }

//            }
//        }

//        /// <summary>
//        /// Wechselt in einen Unterordner
//        /// </summary>
//        /// <param name="DirectoryName">Der Name des vorhandene Verzeichnisses</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public FSDirectory ChangeToFolder(string DirectoryName)
//        {
//            var Result = _CurrentFolder.FindDirectory(DirectoryName);
//            if (Result == null)
//                throw new FSException("directory not found");
//            else
//                return _CurrentFolder = Result;
//        }

//        /// <summary>
//        /// Wechselt in das root-Verzeichnis
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public FSDirectory ChangeToRoot()
//        {
//            var Result = _Root.FindDirectory(rootID);
//            if (Result == null)
//                throw new FSException("root not found");
//            else
//                return _CurrentFolder = Result;
//        }
       
//        public ICryptoTransform CreateEncryptor()
//        {
//            using (var myAes = Aes.Create())
//            {
//                myAes.BlockSize = 128;
//                myAes.KeySize = 128;
//                myAes.Padding = PaddingMode.Zeros;
//                myAes.Mode = CipherMode.CBC;
//                myAes.FeedbackSize = 128;

//                return myAes.CreateEncryptor(_IDXFS_KEY, _IDXFS_IV);
//            }
//        }

//        public ICryptoTransform CreateDecryptor()
//        {
//            using (var myAes = Aes.Create())
//            {
//                myAes.BlockSize = 128;
//                myAes.KeySize = 128;
//                myAes.Padding = PaddingMode.Zeros;
//                myAes.Mode = CipherMode.CBC;
//                myAes.FeedbackSize = 128;

//                return myAes.CreateEncryptor(_IDXFS_KEY, _IDXFS_IV);
//            }
//        }

//        #endregion

//        public DF(string Filename)
//        {
//            _Filename = Filename;
//        }

//        public void Dispose()
//        {
//            Flush();
//            Close();
//        }
//    }

//    public class DFCache<T>
//        : DFObject where T : DFCluster
//    {
//        public const int FREE = -1;

//        private T[] Clusters;
//        private int Index;

//        #region Cache-Methods
//        /// <summary>
//        /// Liefert die Position des gesuchten DataClusters zurück
//        /// </summary>
//        /// <param name="Cluster">Die Nummer des gesuchten DataClusters</param>
//        /// <returns>Positiv wenn im Cache, sonst -1</returns>
//        public int Exists(int Cluster)
//        {
//            //TODO:Optimieren der Cache-Suche ...
//            int ItemsTested = Clusters.Length, i = Index;
//            while (ItemsTested-- > 0)
//            {
//                if (Clusters[i] != null)
//                    if (Clusters[i].Cluster == Cluster)
//                        return i;

//                i = (++i) >= Clusters.Length ? 0 : i;
//            }

//            return -1;
//        }

//        /// <summary>
//        /// Entfernt einen Element am angegebenen Index aus dem Cache und gibt das Feld frei
//        /// </summary>
//        /// <param name="Position">Der nullbasierte Index des zu entfernenden Elements</param>
//        public void RemoveAt(int Position)
//        {
//            try
//            {
//                if (Clusters[Position] != null)
//                {
//                    // Schreiben des Clusters auf die Festplatte
//                    if (Clusters[Position].Dirty)
//                        Clusters[Position].Write();

//                    Clusters[Position] = null;
//                }
//            }
//            catch (Exception)
//            {
//                // pass through
//                throw;
//            }
//        }

//        /// <summary>
//        /// Sucht einen DataCluster im Cache und gibt ihn zurück
//        /// </summary>
//        /// <param name="Cluster">Die ClusterNummer die gesucht werden soll</param>
//        /// <returns>FSDataCluster wenn vorhanden, sonst null</returns>
//        public T Item(int Cluster)
//        {
//            int Position = Exists(Cluster);

//            if (Position == FREE)
//                return null;
//            else
//                return Clusters[Position];
//        }

//        /// <summary>
//        /// Fügt einen DataCluster in den Cache ein
//        /// </summary>
//        /// <param name="Value">Der DataCluster der eingefügt werden soll</param>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Append(T Value)
//        {
//            try
//            {
//                RemoveAt(Index);

//                Clusters[Index] = Value;
//                Index = (++Index) >= Clusters.Length ? 0 : Index;
//            }
//            catch (DFException)
//            {
//                // pass through
//                throw;
//            }
//        }

//        /// <summary>
//        /// Schreibt sämtliche gespeicherten DataClusters auf den Datenträger.
//        /// </summary>
//        /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//        public void Flush()
//        {
//            try
//            {
//                int ItemsTested = Clusters.Length;
//                for (int i = Index; ItemsTested > 0; ItemsTested--)
//                {
//                    if (Clusters[i] != null)
//                        Clusters[i].Write();

//                    i = (--i) < 0 ? Clusters.Length - 1 : i;
//                }
//            }
//            catch (DFException)
//            {
//                // pass
//                throw;
//            }
//        }
//        #endregion

//        public DFCache(IDF DF, int CacheSize)
//            : base(DF)
//        {
//            Clusters = new T[CacheSize];
//            for (int i = 0; i < CacheSize; i++)
//                Clusters[i] = null;

//            Index = 0;
//        }
//    }

//    class DFHelpers
//    {
//        public static byte[] GetStringBytes(string Value, int Length = -1)
//        {
//            if (Length == -1)
//                return System.Text.Encoding.ASCII.GetBytes(Value);
//            else
//                if (Value.Length > Length)
//                return System.Text.Encoding.ASCII.GetBytes(Value.Substring(0, Length));
//            else
//                return System.Text.Encoding.ASCII.GetBytes(Value.PadRight(Length));
//        }
//        public static string BytesToString(byte[] Raw)
//        {
//            return System.Text.Encoding.ASCII.GetString(Raw).TrimEnd();
//        }
//    }
//}
