/*
 * Copyright (c) 2014-2023 Anrá aka Nox
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
using Microsoft.Extensions.Logging;
using Nox.Component;
using Nox.Hosting;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Nox.IO.DF;

public interface IDFGuardian
{
    ILogger Log { get;  }
    Laverna Laverna { get;  }

    string Filename { get;  }
    FileStream FileHandle { get;  }
    
    DFCache<DFCluster> Cache { get;  }
    DFHeader Header { get; }

    // security
    ICryptoTransform CreateEncryptor();
    ICryptoTransform CreateDecryptor();

    // properties for lazy coding
    int ClusterMapCount { get; }
    int ContainerCount { get; }
    int ClusterSize { get; }

    // calculating methods
    int HeaderOffset();
    int ContainerOffset(int Index = 0);
    int ClusterMapOffset(int Index = 0);
    int ClusterOffset(int Index = 0);

    // access methods

}


public abstract class DFBase
    : ObservableObject
{
    protected const uint HASH_MASTER = 0x1494BFDA;
    protected readonly uint _DefaultSignature;

    #region Properties
    protected IDFGuardian GuardianGet { get; }

    public uint DefaultSignature { get => _DefaultSignature; }
    #endregion

    #region Helpers
    public static byte[] GetStringBytes(string Value, int Length = -1)
    {
        if (Length == -1)
            return Encoding.ASCII.GetBytes(Value);
        else
            if (Value.Length > Length)
            return Encoding.ASCII.GetBytes(Value.Substring(0, Length));
        else
            return Encoding.ASCII.GetBytes(Value.PadRight(Length));
    }
    public static string BytesToString(byte[] Raw)
        => Encoding.ASCII.GetString(Raw).TrimEnd();

    public static uint HashSignature(Type type)
        => (uint)HASH_MASTER ^ Hash.HashFNV1a32(type.FullName);


    public static int SizeOf<T>(T value) where T : struct
        => Marshal.SizeOf(value);
    #endregion

    public DFBase(IDFGuardian guardian)
    {
        GuardianGet = guardian;
        _DefaultSignature = HashSignature(GetType());
    }
}

public class DFException
    : Exception
{
    #region Konstruktoren
    public DFException(string Message)
        : base(Message)
    {
    }
    public DFException(string Message, Exception innerException)
        : base(Message, innerException)
    {
    }
    #endregion
}


public abstract class DFElement
    : DFBase
{
    public virtual bool Dirty { get; protected set; }

    public abstract void Read(BinaryReader Reader);

    public abstract void Write(BinaryWriter Writer);

    public abstract void UserDataCRC(tinyCRC CRC);

    public DFElement(IDFGuardian guardian)
        : base(guardian)
    {
        PropertyChanged += (s, e) =>
            Dirty = true;
    }
}

public class DFContainerDescriptors
    : DFElement
{
    private int _Size;
    private int _Offset;

    #region Properties
    public int Size
    {
        get => _Size;
        set => SetProperty(ref _Size, value);
    }

    public int Offset
    {
        get => _Offset;
        set => SetProperty(ref _Offset, value);
    }
    #endregion

    public override void Read(BinaryReader Reader)
    {
        _Size = Reader.ReadInt32();
        _Offset = Reader.ReadInt32();
    }

    public override void UserDataCRC(tinyCRC CRC)
    {
        CRC.Push(_Size);
        CRC.Push(_Offset);
    }

    public override void Write(BinaryWriter Writer)
    {
        Writer.Write(Size);
        Writer.Write(Offset);
    }

    public DFContainerDescriptors(IDFGuardian guardian) 
        : base(guardian)
    {

    }

}

public class DFClusterMap
    : DFElement
{
    private uint[] _Map;

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
                throw new DFException(e.Message);
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
    public override void Read(BinaryReader Reader)
    {
        try
        {
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
            throw new DFException(IOe.Message);
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
            throw new DFException(IOe.Message);
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

    public override void UserDataCRC(tinyCRC CRC)
    {
        for (int i = 0; i < _Map.Length; i++)
            CRC.Push(_Map[i]);
    }

    public DFClusterMap(IDFGuardian guardian, int ClusterSize)
        : base(guardian)
    {
        _SlotsFree = _SlotCount = (ClusterSize) << 3;
        _Map = new uint[_SlotCount >> 5];
    }
}

public abstract class DFContainer
    : DFBase
{
    private uint _Signature;
    private uint _CRC;

    #region Properties
    public abstract bool Encrypted { get; }

    public virtual bool Dirty { get; protected set; }

    public uint CRC { get => _CRC; }
    #endregion

    public abstract void Read();
    public virtual void Read(FileStream FileHandle, int Offset)
    {
        try
        {
            FileHandle.Position = Offset;

            BinaryReader Reader = new BinaryReader(FileHandle);
            if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
                throw new InvalidDataException("signature mismatch");

            if (Encrypted)
            {
                var CryptoStream = new CryptoStream(FileHandle, GuardianGet.CreateEncryptor(), CryptoStreamMode.Read);
                BinaryReader CSReader = new BinaryReader(CryptoStream);

                ReadUserData(CSReader);
            }
            else
                ReadUserData(Reader);

            _CRC = Reader.ReadUInt32();
            if (ReCRC() != _CRC)
                throw new InvalidDataException("CRC mismatch");

            Dirty = false;
        }
        catch (Exception ex)
        {
            GuardianGet.Log?.LogError($"{ex}");

            throw;
        }
    }
    public abstract void ReadUserData(BinaryReader Reader);

    public abstract void Write();
    public virtual void Write(FileStream FileHandle, int Offset)
    {
        if (Dirty)
        {
            try
            {
                FileHandle.Position = Offset;

                // unencrypted writer
                BinaryWriter Writer = new BinaryWriter(FileHandle);
                Writer.Write(_Signature);

                if (Encrypted)
                {
                    // cryptowriter 
                    var CryptoStream = new CryptoStream(FileHandle, GuardianGet.CreateEncryptor(), CryptoStreamMode.Write);
                    BinaryWriter CSWriter = new BinaryWriter(CryptoStream);

                    // userdata are always encrypted

                    WriteUserData(CSWriter);
                    
                    CSWriter.Flush(); // force
                    CryptoStream.Flush(); // flush 
                    CryptoStream.FlushFinalBlock(); // finalize
                } else
                    WriteUserData(Writer);

                // write crc of unencrypted data plain 
                _CRC = ReCRC();
                Writer.Write(_CRC);

                Writer.Flush();

                Dirty = false;
            }
            catch (DFException)
            {
                // pass through
                throw;
            }
            catch (IOException IOe)
            {
                throw new DFException(IOe.Message);
            }
            catch (Exception e)
            {
                throw new DFException(e.Message);
            }
        }
    }
    public abstract void WriteUserData(BinaryWriter Writer);

    #region Helpers
    public abstract void UserDataCRC(tinyCRC CRC);

    /// <summary>
    /// Berechnet die Checksumme für den Kopfsatz.
    /// </summary>
    /// <returns>der CRC32 des Kopfsatzes</returns>
    public uint ReCRC()
    {
        var CRC = new tinyCRC();
        CRC.Push(_Signature);

        UserDataCRC(CRC);

        return CRC.CRC32;
    }

    public abstract int UserDataSize();

    public int ContainerSize()
        // signature and crc
        => sizeof(uint) << 1 + UserDataSize();
    #endregion

    public DFContainer(IDFGuardian guardian, uint Signature)
        : this(guardian)
    {
        _Signature = Signature;
    }

    public DFContainer(IDFGuardian guardian)
        : base(guardian)
    {
        PropertyChanged += (s, e) => { Dirty = true; };

        _Signature = DefaultSignature;
        Dirty = true;
    }
}

public class DFClusterMaps
    : DFContainer
{
    private DFClusterMap[] _Map;

    public override bool Encrypted { get => false; }

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
            for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
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
            for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
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
            for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
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

            for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
                Result |= _Map[i].Dirty;

            return Result;
        }
        protected set
        {
            base.Dirty = value;
        }
    }
    #endregion
    public override void Read()
        => Read(GuardianGet.FileHandle, 0);
    public override void ReadUserData(BinaryReader Reader)
    {
        _Map = new DFClusterMap[GuardianGet.ClusterMapCount];
        for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
        {
            _Map[i] = new DFClusterMap(GuardianGet, GuardianGet.ClusterSize);
            _Map[i].Read(Reader);
        }
    }

    public override void Write()
        => base.Write(GuardianGet.FileHandle, 0);
    public override void WriteUserData(BinaryWriter Writer)
    {
        for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
            if (_Map[i].Dirty)
                _Map[i].Write(Writer);
            else
                // move one cluster
                Writer.Seek(GuardianGet.ClusterSize, SeekOrigin.Current);
    }

    /// <summary>
    /// Ermittelt einen freien Slot und liefert ihn zurück
    /// </summary>
    /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
    public int GetFreeSlot()
    {
        int Base = 0;
        for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
        {
            if (_Map[i].SlotsFree > 0)
                return Base + _Map[i].GetFreeSlot();

            Base += _Map[i].SlotCount;
        }

        return -1;
    }

    public override int UserDataSize()
        => _Map.Length * GuardianGet.ClusterSize;

    public override void UserDataCRC(tinyCRC CRC)
    {
        for (int i = 0; i < _Map.Length; i++)
            _Map[i].UserDataCRC(CRC);
    }

    public DFClusterMaps(IDFGuardian guardian)
        : base(guardian)
    {
        _Map = new DFClusterMap[GuardianGet.ClusterMapCount];
        for (int i = 0; i < GuardianGet.ClusterMapCount; i++)
            _Map[i] = new DFClusterMap(GuardianGet, GuardianGet.ClusterSize);
    }
}

public abstract class DFCluster
    : DFContainer
{
    private int _ClusterSize;

    private int _ClusterId;

    #region Properties
    public int ClusterSize { get => _ClusterSize; }

    public int ClusterId { get => _ClusterId; }
    #endregion
    /// <summary>
    /// Kann überladen werden, um benutzerdefinierte Daten zu lesen
    /// </summary>
    /// <param name="Reader">Der BinaryReader von dem gelesen werden soll</param>
    public override void ReadUserData(BinaryReader Reader)
    {
        _ClusterId = Reader.ReadInt32();
    }

    /// <summary>
    /// Kann überladen werden, um benutzerdefinierte Daten zu schreiben
    /// </summary>
    /// <param name="Writer">Der BinaryWriter in den geschrieben werden soll</param>
    public override void WriteUserData(BinaryWriter Writer)
    {
        Writer.Write(_ClusterId);
    }

    public override int UserDataSize()
        => sizeof(uint);

    public override void UserDataCRC(tinyCRC CRC)
    {
        CRC.Push(_ClusterId);
    }

    public DFCluster(IDFGuardian guardian, int ClusterSize, int ClusterId)
        : base(guardian)
    {
        PropertyChanged += (s, e)
            => Dirty = true;

        _ClusterSize = ClusterSize;
        _ClusterId = ClusterId;

        Dirty = true;
    }
}

public class DFCache<U>
    : DFBase
    where U : DFCluster
{
    public const int FREE = -1;

    private U[] Clusters;
    private int Index;

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
                if (Clusters[i].ClusterId == Cluster)
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
    public U Item(int Cluster)
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
    public void Append(U Value)
    {
        try
        {
            RemoveAt(Index);

            Clusters[Index] = Value;
            Index = (++Index) >= Clusters.Length ? 0 : Index;
        }
        catch (DFException)
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
        catch (DFException)
        {
            // pass
            throw;
        }
    }
    #endregion

    public DFCache(IDFGuardian guardian, int CacheSize)
        : base(guardian)
    {
        Clusters = new U[CacheSize];
        for (int i = 0; i < CacheSize; i++)
            Clusters[i] = null;

        Index = 0;
    }
}


public class DFHeader
    : DFContainer
{
    public const uint CURRENT_VERSION = 0x0001;

    private uint _Version;
    private int _Build;

    private byte[] _Name;

    private DateTime _Created = DateTime.UtcNow;
    private DateTime _Modified = DateTime.UtcNow;

    private int _ClusterMapCount = 4;
    private int _ClusterSize = 8192;

    private int _ContainerCount;
    private int[] _ContainerSizes;

    public override bool Encrypted { get => false; }

    #region Properties
    public uint Version { get => _Version; }

    public int Build { get => _Build; }

    public string Name
    {
        get => BytesToString(_Name);
        set => SetProperty(ref _Name, GetStringBytes(value, 32));
    }

    public DateTime Created
    {
        get => _Created;
        set => SetProperty(ref _Created, value);
    }

    public DateTime Modified
    {
        get => _Modified;
        set => SetProperty(ref _Modified, value);
    }

    public int ContainerCount
    {
        get => _ContainerCount;
        set => SetProperty(ref _ContainerCount, value);
    }

    public int GetContainerSize(int Index)
        => _ContainerSizes[Index];
    public void SetContainerSize(int Index, int Size)
        => SetProperty(_ContainerSizes[Index], Size, (d) => _ContainerSizes[Index] = d);

    public int ClusterMapCount
    {
        get => _ClusterMapCount;
        set => SetProperty(ref _ClusterMapCount, value);
    }

    public int ClusterSize
    {
        get => _ClusterSize;
        set => SetProperty(ref _ClusterSize, value);
    }
    #endregion

    #region I/O

    public override void Read()
        => Read(GuardianGet.FileHandle, GuardianGet.HeaderOffset());

    public override void ReadUserData(BinaryReader Reader)
    {
        if ((_Version = Reader.ReadUInt32()) > CURRENT_VERSION)
            throw new InvalidDataException("version mismatch");

        _Build = Reader.ReadInt32();
        _Name = Reader.ReadBytes(32);

        _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
        _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());

        _ContainerCount = Reader.ReadInt32();
        _ContainerSizes = new int[_ContainerCount];
        for (int i = 0; i < _ContainerCount; i++)
            _ContainerSizes[i] = Reader.ReadInt32();

        _ClusterMapCount = Reader.ReadInt32();
        _ClusterSize = Reader.ReadInt32();
    }

    public override void Write()
        => Write(GuardianGet.FileHandle, GuardianGet.HeaderOffset());
    public override void WriteUserData(BinaryWriter Writer)
    {
        Writer.Write(_Version);
        Writer.Write(_Build);

        Writer.Write(_Name, 0, 32);

        Writer.Write(_Created.ToFileTimeUtc());
        Writer.Write(_Modified.ToFileTimeUtc());

        Writer.Write(_ContainerCount);
        for (int i = 0; i < _ContainerSizes.Length; i++)
            Writer.Write(_ContainerSizes[i]);

        Writer.Write(_ClusterMapCount);
        Writer.Write(_ClusterSize);
    }
    #endregion

    public override int UserDataSize()
    {
        int Size = 0;
        Size += SizeOf(_Version);
        Size += SizeOf(_Build);
        Size += Name.Length;
        Size += SizeOf(_Created);
        Size += SizeOf(_Modified);

        Size += SizeOf(_ContainerCount);
        Size += _ContainerCount * sizeof(uint);  // _ContainerSizes;

        Size += Size += SizeOf(_ClusterSize);
        Size += Size += SizeOf(_ClusterMapCount);

        return Size;
    }

    public override void UserDataCRC(tinyCRC CRC)
    {
        CRC.Push(_Version);
        CRC.Push(_Build);
        CRC.Push(_Name);
        CRC.Push(_Modified.ToFileTimeUtc());
        CRC.Push(_Created.ToFileTimeUtc());
        
        CRC.Push(_ContainerCount);
        for (int i = 0; i < _ContainerCount; i++)
            CRC.Push(_ContainerSizes[i]);

        CRC.Push(_ClusterSize);
        CRC.Push(_ClusterMapCount);
    }

    public DFHeader(IDFGuardian guardian, int ClusterSize)
        : this(guardian)
        => this.ClusterSize = ClusterSize;

    public DFHeader(IDFGuardian guardian)
        : base(guardian)
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != nameof(Modified)) { _Modified = DateTime.UtcNow; }
            Dirty = true;
        };

        _Version = CURRENT_VERSION;
        _Build = Assembly.GetEntryAssembly().GetName().Version.Build;

        _Name = GetStringBytes(Guid.NewGuid().ToString(), 64);
    }
}

public class DF
    : DFContainer
{
    // Konstanten
    public const int DEFAULT_CLUSTER_SIZE = 32768;

    protected class DFGuardian
        : IDFGuardian
    {
        // Variablen
        private byte[] _DF_KEY = {
            0x72, 0x7A, 0x62, 0x45, 0x66, 0x5A, 0x55, 0x31,
            0x59, 0x63, 0x32, 0x37, 0x61, 0x44, 0x73, 0x37,
            0x51, 0x75, 0x62, 0x4C, 0x64, 0xA7, 0x71, 0x6F,
            0x67, 0x41, 0x75, 0x43, 0x55, 0x31, 0x75, 0x4B };
        private byte[] _DF_IV = {
            0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8,
            0xD7, 0xE6, 0xF5, 0x04, 0x5F, 0x6E, 0x7D, 0x8C,
            0x9B, 0xAA, 0xB9, 0xC8, 0xD7, 0xE6, 0xF5, 0x04,
            0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8 };

        private uint _FileSignature;

        #region Properties
        public uint FileSignature { get; set; }

        public ILogger Log { get; set; }
        public Laverna Laverna { get; set; }

        public string Filename { get; set; }
        public FileStream FileHandle { get; set; }
        
        public DFCache<DFCluster> Cache { get; set; }

        public DFHeader Header { get; set; }
        #endregion

        public ICryptoTransform CreateEncryptor()
        {
            using (var myAes = Aes.Create())
            {
                myAes.BlockSize = 128;
                myAes.KeySize = 128;
                myAes.Padding = PaddingMode.Zeros;
                myAes.Mode = CipherMode.CBC;
                myAes.FeedbackSize = 128;

                return myAes.CreateEncryptor(_DF_KEY, _DF_IV);
            }
        }
        public ICryptoTransform CreateDecryptor()
        {
            using (var myAes = Aes.Create())
            {
                myAes.BlockSize = 128;
                myAes.KeySize = 128;
                myAes.Padding = PaddingMode.Zeros;
                myAes.Mode = CipherMode.CBC;
                myAes.FeedbackSize = 128;

                return myAes.CreateEncryptor(_DF_KEY, _DF_IV);
            }
        }

        #region Helpers
        public int ClusterMapCount { get => Header.ClusterMapCount; }
        public int ContainerCount { get => Header.ContainerCount; }
        public int ClusterSize { get => Header.ClusterSize; }

        public int HeaderOffset()
            => SizeOf(_FileSignature);

        public int ContainerOffset(int Index = 0)
        {
            int Size = HeaderOffset() + Header.ContainerSize();

            for (int i = 0; i < Index; i++)
                Size += Header.GetContainerSize(i);

            return Size;
        }

        public int ClusterMapOffset(int Index = 0)
        {
            int Size = ContainerOffset(Header.ContainerCount + 1);
            
            Size += Math.Min(Index, Header.ClusterMapCount) * Header.ClusterSize;

            return Size;
        }

        public int ClusterOffset(int Index = 0)
            => ClusterMapOffset(Header.ClusterMapCount + 1) + Header.ClusterSize;
        #endregion

        private DFGuardian()
        {
            Laverna = new Laverna(_DF_KEY, _DF_IV);
            Hosting.Hosting.CreateDefaultLogger<DF>();
        }

        public DFGuardian(uint FileSignature)
            : this() => this.FileSignature = FileSignature;
    }

    public override bool Encrypted { get => false; }

    #region Properties
    public virtual string Extension { get; } = ".dfs";

    public string Filename { get => GuardianGet.Filename; }

    public bool IsOpen { get => (GuardianGet.FileHandle != null); }
    #endregion

    private DFGuardian GuardianSet { get => (GuardianGet as DFGuardian); }

    #region Container Methods
    private List<DFContainer> _Containers = new();
    public void RegisterContainer<T>(T container)
        where T : DFContainer, new()
        => _Containers.Add(container);
    #endregion

    #region FS-Helpers
    internal void ClearCluster(int Cluster)
    {
        try
        {
            long Offset = GuardianGet.ClusterOffset(Cluster+1);
            byte[] Buffer = new byte[GuardianGet.ClusterSize];

            if (GuardianGet.FileHandle.Seek(Offset, SeekOrigin.Begin) == Offset)
                GuardianGet.FileHandle.Write(Buffer, 0, Buffer.Length);
        }
        catch (IOException IOe)
        {
            //Log.WriteException(IOe);
            throw new DFException("could not clear cluster", IOe);
        }
    }
    #endregion

    private DFHeader CreateHeader()
        => new DFHeader(GuardianGet);
    private DFHeader CreateHeader(int ClusterSize)
        => new DFHeader(GuardianGet, ClusterSize);

    #region FS-Methods
    public void Create(bool ForceOverwrite, string VolumeName = "")
        => Create(ForceOverwrite, VolumeName, DEFAULT_CLUSTER_SIZE);

    public void Create(bool ForceOverwrite, string VolumeName, int ClusterSize)
    {
        Close();

        GuardianSet.Filename = Filename.RemoveExtension();
        if ((File.Exists(this.Filename + Extension)) & (!ForceOverwrite))
            throw new DFException("file already exists");

        try
        {
            GuardianSet.FileHandle = File.Open(this.Filename + Extension, FileMode.Create);

            Format(VolumeName, ClusterSize);

            GuardianSet.FileHandle.Flush();
        }
        catch (IOException IOe) 
        {
            //Log.WriteException(IOe);
            throw new DFException("could not create file.", IOe);
        }
    }

    public void Open()
    {
        Close();

        GuardianSet.Filename = Filename.RemoveExtension();
        if (!File.Exists(Filename + Extension))
            throw new DFException("file not found");

        try
        {
            GuardianSet.FileHandle = File.Open(Filename + Extension, FileMode.Open);

            Reload();

            GuardianSet.FileHandle.Flush();
        }
        catch (IOException IOe)
        {
            //IDXLogs.WriteException(IOe);
            throw new DFException("could not open file", IOe);
        }
    }

    /// <summary>
    /// Schreibt eventuelle Änderungen in das Dateisystem zurück
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Flush()
    {
        if (IsOpen)
        {
            if (GuardianGet.Header != null)
                GuardianGet.Header.Write();

            if (GuardianGet.Cache != null)
                GuardianGet.Cache.Flush();
        }
    }

    /// <summary>
    /// Schliesst das Dateisystem
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Close()
    {
        if (GuardianGet.FileHandle != null)
        {
            try
            {
                Flush();

                GuardianGet.FileHandle.Close();
                GuardianSet.FileHandle = null;
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new DFException("could not close file", IOe);
            }
            finally
            {
                Dispose();
            }
        }
    }

    /// <summary>
    /// Lädt den Datenbestand erneut vom Dateisystem.
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Reload()
    {
        GuardianSet.Header = CreateHeader(GuardianGet.ClusterSize);
        GuardianGet.Header.Read();
        GuardianSet.Cache = new DFCache<DFCluster>(GuardianGet, 64);
    }

    public void Label(string Name)
    {
        if (IsOpen)
            GuardianSet.Header.Name = Name.ToUpper();

        Flush();
    }

    public void Format(string Name)
        => Format(Name, DEFAULT_CLUSTER_SIZE);

    public virtual void Format(string Name, int ClusterSize)
    {
        if (IsOpen)
        {
            GuardianSet.Header = CreateHeader(GuardianGet.ClusterSize);
            GuardianGet.Header.Name = Name;

            GuardianSet.Cache = new DFCache<DFCluster>(GuardianGet, 64);
            Flush();

            try
            {
                GuardianGet.FileHandle.SetLength(GuardianGet.ClusterOffset());
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new DFException("couldn't clear Cluster", IOe);
            }
        }
    }
    #endregion


    public override void Read()
        => Read(GuardianGet.FileHandle, GuardianGet.HeaderOffset());

    public override void ReadUserData(BinaryReader Reader)
    {
        
    }
    public override void Write()
        => Write(GuardianGet.FileHandle, GuardianGet.HeaderOffset());

    public override void WriteUserData(BinaryWriter Writer)
    {
        throw new NotImplementedException();
    }
    public override int UserDataSize()
    {
        throw new NotImplementedException();
    }
    public override void UserDataCRC(tinyCRC CRC)
    {
        throw new NotImplementedException();
    }

    public DF(string Filename)
        : base(null)
    {
        GuardianSet.Filename = Filename;
    }

    public void Dispose()
    {
        Flush();
        Close();
    }
}


