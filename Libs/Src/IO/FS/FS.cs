/*
 * Copyright (c) 2014-20203 Anrá aka Nox
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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nox.IO.Buffer;
using Nox.IO.DF;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Nox.IO.FS;

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
public interface IFSGuardian
    : IGuardian
{
    int NodeSize { get; }
    int NodesPerBlock { get; }

    int UseableClusterSize { get; }
}
public interface IFSPatchTransform : IDisposable
{
    string Extension { get; }

    FileStream Convert(string SourceFile);
    FileStream Convert(FileStream SourceFile);
}
public class FSException
        : DFException
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
public class FSMap<T>
    : Element<T>
    where T : class, IFSGuardian
{
    private int _SlotCount;
    private int _SlotsFree;

    private byte[] _Map;

    #region Properties
    /// <summary>
    /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
    /// </summary>
    /// <param name="Index">Der 0-basierte Index des Clusters</param>
    /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
    public bool this[int index]
    {
        get
        {
            byte Mask = (byte)(1 << (index & 7));
            return (_Map[index >> 3] & Mask) == Mask;
        }
        set
        {
            byte Mask = (byte)(1 << (index & 7));
            bool Used = (_Map[index >> 3] & Mask) == Mask;

            if (value)
            {
                if (!Used)
                    _SlotsFree++;

                SetProperty<byte>(ref _Map[index >> 3], (byte)(_Map[index >> 3] | Mask));
            }
            else
            {
                if (Used) { _SlotsFree--; }
                SetProperty<byte>(ref _Map[index >> 3], (byte)(_Map[index >> 3] & (byte)~Mask));
            }
        }
    }

    /// <summary>
    /// Liefert die Anzahl an Slot zurück.
    /// </summary>
    public int SlotCount
    {
        get => _SlotCount;
    }

    /// <summary>
    /// Liefert die Anzahl an freien Slots zurück.
    /// </summary>
    public int SlotsFree
    {
        get => _SlotsFree;
    }

    /// <summary>
    /// Liefert die Anzahl an belegten Slots zurück
    /// </summary>
    public int SlotsUsed
    {
        get => _SlotCount - _SlotsFree;
    }

    /// <summary>
    /// Liefert die Größe der Karte in Bytes zurück.
    /// </summary>
    public int MapSize
    {
        get => _Map.Length;
    }
    #endregion

    #region I/O
    public override void Read(BinaryReader Reader)
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

    public override void Write(BinaryWriter Writer)
    {
        for (int i = 0; i < _Map.Length; i++)
            Writer.Write(_Map[i]);
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

    public override void UserDataCRC(tinyCRC CRC)
    {
        CRC.Push(_SlotCount);
        CRC.Push(_SlotsFree);
        CRC.Push(_Map);
    }

    public FSMap(T guardian, int SlotCount)
            : base(guardian)
    {
        _SlotsFree = _SlotCount = SlotCount;
        _Map = new byte[(int)System.Math.Ceiling(SlotCount / (double)8)];
    }
}
public class FSNode<T>
    : Element<T>
    where T : class, IFSGuardian
{
    // Vars
    private uint _Id;
    private uint _Parent;

    private uint _Flags;

    private byte[] _Name;

    private int _FileSize;
    private int _ClusterCount;

    private DateTime _Created;
    private DateTime _Modified;

    private int _FirstCluster;
    private int _LastCluster;

    #region Properties
    /// <summary>
    /// Liefert die Id des Knoten zurück oder legt sie fest
    /// </summary>
    public uint Id
    {
        get => _Id;
        set => SetProperty(ref _Id, value);
    }

    /// <summary>
    /// Liefert den Vater des Knoten zurück oder legt ihn fest
    /// </summary>
    public uint Parent
    {
        get => _Parent;
        set => SetProperty(ref _Parent, value);
    }

    #region Flags
    public uint Flags
    {
        get => _Flags;
        set => SetProperty(ref _Flags, value);
    }

    public bool IsHidden
    {
        get => (_Flags & (uint)FSFlags.Hidden) == (uint)FSFlags.Hidden;
        set => Flags |= (int)FSFlags.Hidden;
    }
    public bool IsArchive
    {
        get => (_Flags & (uint)FSFlags.Archive) == (uint)FSFlags.Archive;
        set => Flags |= (int)FSFlags.Archive;
    }
    public bool IsReadOnly
    {
        get => (_Flags & (uint)FSFlags.ReadOnly) == (uint)FSFlags.ReadOnly;
        set => Flags |= (int)FSFlags.ReadOnly;
    }
    public bool IsEncrypted
    {
        get => (_Flags & (uint)FSFlags.Encrypted) == (uint)FSFlags.Encrypted;
        set => Flags |= (int)FSFlags.Encrypted;
    }
    public bool IsSymLink
    {
        get => (_Flags & (uint)FSFlags.SymLink) == (uint)FSFlags.SymLink;
        set => Flags |= (int)FSFlags.SymLink;
    }
    public bool IsSystemUseOnly
    {
        get => (_Flags & (uint)FSFlags.SystemUseOnly) == (uint)FSFlags.SystemUseOnly;
        set => Flags |= (int)FSFlags.SystemUseOnly;
    }
    public bool IsDirectory
    {
        get => (_Flags & (uint)FSFlags.Directory) == (uint)FSFlags.Directory;
        set => Flags |= (int)FSFlags.Directory;
    }
    #endregion

    /// <summary>
    /// Liefert den Namen der Datei zurück oder legt ihn fest
    /// </summary>
    public string Name
    {
        get => BytesToString(_Name);
        set => SetProperty(ref _Name, GetStringBytes(value, 32));
    }

    /// <summary>
    /// Liefert die Größe der Datei zurück oder legt sie fest
    /// </summary>
    public int FileSize
    {
        get => _FileSize;
        set => SetProperty(ref _FileSize, value);
    }

    /// <summary>
    /// Liefert die Anzahl an Clustern zurück die von der Datei verwendet werden oder legt sie fest.
    /// </summary>
    public int ClusterCount
    {
        get => _ClusterCount;
        set => SetProperty(ref _ClusterCount, value);
    }

    /// <summary>
    /// Liefert das Datum und die Zeit der Anlage zurück oder legt es fest
    /// </summary>
    public DateTime Created
    {
        get => _Created;
        set => SetProperty(ref _Created, value);
    }

    /// <summary>
    /// Liefert das Datum und die Zeit der letzten Änderung zurück oder legt es fest
    /// </summary>
    public DateTime Modified
    {
        get => _Modified;
        set => SetProperty(ref _Modified, value);
    }

    public int FirstCluster
    {
        get => _FirstCluster;
        set => SetProperty(ref _FirstCluster, value);
    }

    public int LastCluster
    {
        get => _LastCluster;
        set => SetProperty(ref _LastCluster, value);
    }
    #endregion

    #region I/O
    public override void Read(BinaryReader Reader)
    {
        _Id = Reader.ReadUInt32();
        _Parent = Reader.ReadUInt32();

        _Flags = Reader.ReadUInt32();

        _Name = Reader.ReadBytes(32);

        _FileSize = Reader.ReadInt32();
        _ClusterCount = Reader.ReadInt32();

        _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
        _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());

        _FirstCluster = Reader.ReadInt32();
        _LastCluster = Reader.ReadInt32();
    }
    public override void Write(BinaryWriter Writer)
    {
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
    }
    #endregion

    public override void UserDataCRC(tinyCRC CRC)
    {
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
    }

    public FSNode(T guardian)
        : base(guardian)
    {
        _Name = GetStringBytes("", 32);

        _Created = DateTime.UtcNow;
        _Modified = DateTime.UtcNow;

        _FirstCluster = -1;
        _LastCluster = -1;
    }
}
public class FSHeader<T>
    : ContainerCustom<T>
    where T : class, IFSGuardian
{
    // Konstanten
    private const uint CURRENT_REVISION = 0x10A0;
    private const ushort NODE_SIZE = 128;

    // Variablen
    private uint _Revision = CURRENT_REVISION;
    //private int _NodesPerBlock = -1;

    #region Properties
    public uint Revision
    {
        get => _Revision;
    }

    public override bool Encrypted => false;
    #endregion

    public override void UserDataCRC(tinyCRC CRC)
    {

    }

    public override void ReadUserData(BinaryReader Reader)
    {
        
    }

    public override void WriteUserData(BinaryWriter Writer)
    {
        
    }

    public override int UserDataSize()
    {
        return 0;
    }

    public FSHeader(T guardian)
        : base(guardian, 0)
    {

    }
}
public class FSNodeCluster<T>
    : Cluster<T>
    where T : class, IFSGuardian
{
    private int _NextBlock;
    private byte[] _Reserved;

    private FSMap<T> _NodeMap; 
    private FSNode<T>[] _Nodes;

    public override bool Encrypted { get => true; }

    #region Properties
    /// <summary>
    /// Liefert den ClusterIndex des nächsten Knotens zurück
    /// </summary>
    public int NextBlock
    {
        get => _NextBlock;
        set => SetProperty(ref _NextBlock, value);
    }

    /// <summary>
    /// Liefert den Offset der lokalen Karte zurück
    /// </summary>
    private int LocalMapOffset
    {
        get => sizeof(uint) + sizeof(int) + _Reserved.Length;
    }

    /// <summary>
    /// Liefert den Offset des ersten Knoten zurück
    /// </summary>
    private int LocalNodeOffset
    {
        get => LocalMapOffset + _NodeMap.MapSize;
    }

    /// <summary>
    /// LIefert die Anzahl an freien Knoten zurück
    /// </summary>
    public int NodesFree
    {
        get => _NodeMap.SlotsFree;
    }

    /// <summary>
    /// Liefert die Anzahl an belegten Knoten zurück.
    /// </summary>
    public int NodesUsed
    {
        get => _NodeMap.SlotsUsed;
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
    public override void Read()
        => base.Read(GuardianGet.FileHandle, GuardianGet.ClusterOffset(ClusterId));

    public override void ReadUserData(BinaryReader Reader)
    {
        _NextBlock = Reader.ReadInt32();
        _Reserved = Reader.ReadBytes(_Reserved.Length);

        _NodeMap.Read(Reader);

        // Nodes einlesen...
        byte[] Blank = new byte[GuardianGet.NodeSize];

        for (int i = 0; i < _Nodes.Length; i++)
        {
            if (_NodeMap[i])
            {
                _Nodes[i] = new FSNode<T>(GuardianGet);
                _Nodes[i].Read(Reader);
            }
            else
                Reader.Read(Blank, 0, Blank.Length);
        }
    }

    public override void Write()
        => base.Write(GuardianGet.FileHandle, GuardianGet.ClusterOffset(ClusterId));
    public override void WriteUserData(BinaryWriter Writer)
    {
        Writer.Write(_NextBlock);
        Writer.Write(_Reserved);
        _NodeMap.Write(Writer);


        byte[] Blank = new byte[GuardianGet.NodeSize];
        for (int i = 0; i < _Nodes.Length; i++)
        {
            if (_NodeMap[i])
                _Nodes[i].Write(Writer);
            else
                Writer.Write(Blank);
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
    public FSNode<T> CreateNode(uint NodeId, uint Parent = 0xFFFFFFFF)
    {
        int Slot;
        if ((Slot = GetFreeSlot()) != -1)
        {
            _NodeMap[Slot] = true;
            Dirty = true;

            return _Nodes[Slot] = new FSNode<T>(GuardianGet)
            {
                Id = NodeId,
                Parent = Parent
            };
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
    public FSNode<T> GetNodeAt(int Index)
    {
        return _NodeMap[Index] ? _Nodes[Index] : null;
    }

    /// <summary>
    /// Liefert den Knoten mit dem angegebenen Namen zurück
    /// </summary>
    /// <param name="Name">Der Name nach dem gesucht werden soll</param>
    /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
    public FSNode<T> FindNode(string Name)
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
    public FSNode<T> FindNode(uint NodeId)
    {
        for (int i = 0; i < _Nodes.Length; i++)
            if (_NodeMap[i])
                if (_Nodes[i].Id == NodeId)
                    return _Nodes[i];

        return null;
    }

    public override void UserDataCRC(tinyCRC CRC)
    {

    }
    #endregion

    public FSNodeCluster(T guardian, int ClusterSize, int ClusterId)
        : base(guardian, ClusterSize, ClusterId)
    {
        _NodeMap = new FSMap<T>(guardian, GuardianGet.NodesPerBlock);
        _NextBlock = -1;

        _Nodes = new FSNode<T>[GuardianGet.NodesPerBlock];
        _Reserved = new byte[GuardianGet.ClusterSize - (GuardianGet.NodesPerBlock *
            GuardianGet.NodeSize + _NodeMap.MapSize + 4)];
    }
}
public class FSDataCluster<T>
    : Cluster<T>
    where T : class, IFSGuardian
{
    private int _Previous;
    private int _Next;

    private byte[] _Data;

    public override bool Encrypted { get => true; }

    #region Properties
    /// <summary>
    /// Liefert ein Byte aus dem Datenpuffer zurück oder legt es fest
    /// </summary>
    /// <param name="Index">Der Index an dem das Byte gelesen oder geschrieben werden soll</param>
    /// <returns>Das gelesene Byte</returns>
    public byte this[int index]
    {
        get => _Data[index];
        set => SetProperty(ref _Data[index], value);
    }

    public int Previous
    {
        get => _Previous;
        set => SetProperty(ref _Previous, value);
    }

    public int Next
    {
        get => _Next;
        set => SetProperty(ref _Next, value);
    }
    #endregion

    #region I/O

    public override void Read()
        => base.Read(GuardianGet.FileHandle, GuardianGet.ClusterOffset(ClusterId));
    public override void ReadUserData(BinaryReader Reader)
    {
        base.ReadUserData(Reader);

        _Previous = Reader.ReadInt32();
        _Next = Reader.ReadInt32();

        Reader.Read(_Data, 0, _Data.Length);
    }

    public override void Write()
        => base.Write(GuardianGet.FileHandle, GuardianGet.ClusterOffset(ClusterId));
    public override void WriteUserData(BinaryWriter Writer)
    {
        base.WriteUserData(Writer);

        Writer.Write(_Previous);
        Writer.Write(_Next);

        Writer.Write(_Data, 0, _Data.Length);
    }
    #endregion

    public int BlockRead(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
    {
        int Read = (_Data.Length - SourceOffset);

        if (Read > Length)
            Read = Length;

        Array.Copy(_Data, SourceOffset, Buffer, DestOffset, Read);

        return Read;
    }

    public int BlockWrite(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
    {
        int Read = (_Data.Length - SourceOffset);

        if (Read > _Data.Length)
            Read = Length;

        Array.Copy(Buffer, SourceOffset, _Data, DestOffset, Length);

        return Read;
    }

    public override int UserDataSize()
        => base.UserDataSize() +
        // prev and next
        sizeof(int) << 1 +

        // data maybe null
        _Data?.Length ?? 0;

    public override void UserDataCRC(tinyCRC CRC)
    {
        base.UserDataCRC(CRC);

        CRC.Push(_Previous);
        CRC.Push(_Next);
        CRC.Push(_Data);
    }

    public FSDataCluster(T guardian, int ClusterSize, int Cluster)
        : base(guardian, ClusterSize, Cluster)
    {
        // keep bytes free for prev and next
        _Data = new byte[ClusterSize - ContainerSize() - UserDataSize()];
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

public class FSStream<T>
    : Stream
    where T : class, IFSGuardian
{
    private FS<T> _FS;
    private T _Guardian;
    private FSNode<T> _Node;

    private FSDataCluster<T> _CurrentCluster;
    private int _CurrentIndex;

    private long _ClusterStart;
    private long _ClusterEnd;

    private long _CurrentPosition = 0;

    #region Properties
    public override bool CanRead { get => true; }
    public override bool CanSeek { get => true; }
    public override bool CanTimeout { get => false; }
    public override bool CanWrite { get => true; }
    public override long Length { get => _Node.FileSize; }
    public override long Position
    {
        get => _CurrentPosition;
        set => _CurrentPosition = value;
    }

    public Stream BaseStream { get => this; }

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
        private set => _LastError = value;
    }
    #endregion

    #region Stream Methods
    public override void Flush()
        => _FS.Flush();

    public override void Close()
    {
        Flush();
        base.Close();
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
            _CurrentCluster = _FS.ReadCluster(Cluster);

            _ClusterStart = (FileIndex * _Guardian.UseableClusterSize);
            _ClusterEnd = (FileIndex * _Guardian.UseableClusterSize) + _Guardian.UseableClusterSize - 1;

            _CurrentIndex = FileIndex;
        }
        else
        {
            // position points to an unknown cluster.. wait and see
            _CurrentCluster = null;

            _ClusterStart = FileIndex * _Guardian.UseableClusterSize;
            _ClusterEnd = (FileIndex * _Guardian.UseableClusterSize) + _Guardian.UseableClusterSize - 1;
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
            int Slot = _Guardian.ClusterMaps.GetFreeSlot();

            FSDataCluster<T> CurrentCluster; int CurrentClusterSlot = -1;
            // Sonderfall, kein Cluster existiert - Setze FirstCluster
            if (_Node.ClusterCount == 0)
                _Guardian.ClusterMaps[_Node.FirstCluster = _Node.LastCluster = Slot] = true;
            else
            {
                CurrentCluster = _FS.ReadCluster(_Node.LastCluster);
                CurrentClusterSlot = CurrentCluster.ClusterId;

                _Guardian.ClusterMaps[CurrentCluster.Next = _Node.LastCluster = Slot] = true;
            }

            var NewCluster = _FS.CreateDataCluster(Slot);
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
            FSDataCluster<T> CurrentCluster;
            if (_Node.LastCluster == -1)
                return;
            else
            {
                int Cluster = _Node.LastCluster, Previous = -1;
                while (_Node.ClusterCount > ClusterCount)
                {
                    CurrentCluster = _FS.ReadCluster(Cluster);
                    Previous = _Node.LastCluster = CurrentCluster.Previous;
                    _FS.ClearCluster(Cluster);

                    _Guardian.ClusterMaps[Cluster] = false;
                    _Node.ClusterCount--;

                    Cluster = Previous;
                }

                if (_Node.ClusterCount > 0)
                {
                    CurrentCluster = _FS.ReadCluster(_Node.LastCluster);
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
        return (int)Math.Ceiling(FileLength / (double)_Guardian.UseableClusterSize);
    }

    public FSStream(FS<T> FS, T guardian, FSNode<T> Node)
        : base()
    {
        _FS = FS;
        _Guardian = guardian;

        _Node = Node;
        if (_Node.ClusterCount > 0)
            ReadCluster(0, _Node.FirstCluster);
    }
}

public class FSTree<T>
    where T : class, IFSGuardian
{
    private FSDirectory<T> _Root;

    #region Properties
    /// <summary>
    /// Liefert das Root-Objekt zurück
    /// </summary>
    public FSDirectory<T> Root
    {
        get => _Root;
    }
    #endregion

    /// <summary>
    /// Sucht rekursiv ein Verzeichnis
    /// </summary>
    /// <param name="Id">Die Id nach der gesucht werden soll</param>
    /// <returns>Ein IDXFSDirectory-Objekt wenn erfolgreich, sonst null</returns>
    public FSDirectory<T> FindDirectory(uint Id)
    {
        var Stack = new Stack<FSDirectory<T>>(); FSDirectory<T> Next;
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

    public bool InsertNode(FSNode<T> Node)
    {
        return true;
    }

    public bool RemoveNode(FSNode<T> Node)
    {
        return true;
    }

    public FSTree(FSNode<T> Root)
    {
        _Root = new FSDirectory<T>(Root);
    }

    public FSTree(FSDirectory<T> Root)
    {
        _Root = Root;
    }
}
public class FSDirectory<T>
    where T : class, IFSGuardian
{
    private FSNode<T> _Node;

    private EventList<FSDirectory<T>> _Directories;
    private EventList<FSNode<T>> _Files;

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
    public EventList<FSDirectory<T>> Directories
    {
        get
        {
            return _Directories;
        }
    }

    /// <summary>
    /// Lierfert die Dateien des Verzeichnisses zurück.
    /// </summary>
    public EventList<FSNode<T>> Files
    {
        get
        {
            return _Files;
        }
    }
    #endregion

    public FSDirectory<T> FindDirectory(string Name)
    {
        string FindName = Name.ToLower().Trim();

        for (int i = 0; i < _Directories.Count; i++)
            if (_Directories[i].Name.ToLower().Trim() == FindName)
                return _Directories[i];

        return null;
    }

    public FSNode<T> FindFile(string Name)
    {
        string FindName = Name.ToLower().Trim();

        for (int i = 0; i < _Files.Count; i++)
            if (_Files[i].Name.ToLower().Trim() == FindName)
                return _Files[i];

        return null;
    }

    public FSDirectory(FSNode<T> Node)
    {
        _Node = Node;

        _Directories = new EventList<FSDirectory<T>>();
        _Files = new EventList<FSNode<T>>();
    }
}
public class FS<T>
    : DF<IFSGuardian>
    where T : class, IFSGuardian
{
    protected class FSGuardian
        : DFGuardian, IFSGuardian
    {
        #region Properties

        #endregion

        #region Helpers
       
        #endregion

        public FSGuardian()
            : base()
        {
            
        }

        public FSGuardian(uint FileSignature)
            : base(FileSignature) 
        {
            // overwrite
            Log = Hosting.Hosting.CreateDefaultLogger<FS>();
        }

        public int NodeSize { get; set; } = 128;

        private int _NodesPerBlock = -1;
        public int NodesPerBlock 
        { 
            get
            {
                if (_NodesPerBlock == -1)
                {
                    _NodesPerBlock = 32;
                    while (_NodesPerBlock * NodeSize < UseableClusterSize)
                        _NodesPerBlock++;

                    while (_NodesPerBlock * NodeSize > UseableClusterSize)
                        _NodesPerBlock--;
                }

                return _NodesPerBlock;
            }
                }

        public int UseableClusterSize 
        {
            get => ClusterSize - 16;
        }
    }

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
    private FSHeader<T> _Header;

    private FSTree<T> _Root;
    private uint rootID;
    private FSDirectory<T> _CurrentFolder;
    private List<FSNodeCluster<T>> _NodeBlocks;

    public override string Extension => ".vfs";

    #region Properties       
    public string FullPath
    {
        get
        {
            var Result = new StringBuilder();

            FSDirectory<T> Current = _CurrentFolder;

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
    #endregion

    #region FS-Helpers
    private bool BuildTree()
    {
        List<FSDirectory<T>> FreeDirectories = new();
        List<FSNode<T>> FreeNodes = new();

        for (int i = 0; i < _NodeBlocks.Count; i++)
            for (int j = 0; j < GuardianGet.NodesPerBlock; j++)
            {
                FSNode<T> Node = _NodeBlocks[i].GetNodeAt(j);
                if (Node != null)
                {
                    if (Node.IsDirectory)
                        FreeDirectories.Add(new(Node));
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

        if (FreeDirectories[0].Id != Hash.HashFNV1a32("ROOT"))
            throw new FSException("ROOT NOT FOUND!");
        else
            _Root = new FSTree<T>(FreeDirectories[0]);

        return true;
    }

    public void ReadNodeBlocks()
    {
        _NodeBlocks = new List<FSNodeCluster<T>>();

        int ClusterIndex = GuardianGet.ClusterOffset();
        do
        {
            long BlockOffset = GuardianGet.ClusterOffset(ClusterIndex);

            var NodeBlock = new FSNodeCluster<T>((T)GuardianGet, GuardianGet.ClusterSize, ClusterIndex);
            NodeBlock.Read();

            _NodeBlocks.Add(NodeBlock);
            ClusterIndex = NodeBlock.NextBlock;
        } while (ClusterIndex != -1);

        // Sort Blocks
        for (int i = 1; i < _NodeBlocks.Count; i++)
            for (int j = i; j < _NodeBlocks.Count; j++)
                if ((_NodeBlocks[j].ClusterId == _NodeBlocks[i - 1].NextBlock) & (i != j))
                {
                    var Temp = _NodeBlocks[i];
                    _NodeBlocks[i] = _NodeBlocks[j];
                    _NodeBlocks[j] = Temp;
                }
    }

    public void WriteNodeBlocks()
    {
        for (int i = 0; i < _NodeBlocks.Count; i++)
            _NodeBlocks[i].Write();
    }

    public bool CreateNodeBlock()
    {
        int ClusterSlot = GuardianGet.ClusterMaps.GetFreeSlot();
        var NextBlock = new FSNodeCluster<T>((T)GuardianGet, GuardianGet.ClusterSize, ClusterSlot);

        _NodeBlocks[_NodeBlocks.Count - 1].NextBlock = ClusterSlot;
        GuardianGet.ClusterMaps[ClusterSlot] = true;

        _NodeBlocks.Add(NextBlock);
        return true;
    }

    public FSNode<T> CreateNode(uint Id, uint Parent = 0xFFFFFFFF)
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

    public FSDataCluster<T> ReadCluster(int Cluster)
    {
        FSDataCluster<T> Result = GuardianGet.Cache.Item(Cluster) as FSDataCluster<T>;

        if (Result == null)
        {
            Result = new FSDataCluster<T>((T)GuardianGet, GuardianGet.ClusterSize, Cluster);
            Result.Read();

            GuardianGet.Cache.Append(Result);
        }

        return Result;
    }

    public FSDataCluster<T> CreateDataCluster(int Cluster)
    {
        var Result = new FSDataCluster<T>((T)GuardianGet, GuardianGet.ClusterSize, Cluster);
        GuardianGet.Cache.Append(Result);

        return Result;
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
    #endregion

    #region FS-Methods
    public override void Create(bool ForceOverwrite, string VolumeName = "")
        => Create(ForceOverwrite, VolumeName, 1, 2048, 4, 8192);

    protected override void Create(bool ForceOverwrite, string VolumeName, int CustomContainerSize, int CustomContainerCount, int ClusterMapCount, int ClusterSize)
    {
        base.Create(ForceOverwrite, VolumeName, CustomContainerSize, CustomContainerCount, ClusterMapCount, ClusterSize);
        
        _Header = new FSHeader<T>((T)GuardianGet);
        _Header.Write();
    }

    public override void Flush()
    {
        base.Flush();
        if (IsOpen)
        {
            if (_Header != null)
                _Header.Write();
            if (_NodeBlocks != null)
                WriteNodeBlocks();
        }
    }

    /// <summary>
    /// Schliesst das Dateisystem
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public override void Close()
    {
        base.Close();
        if (GuardianGet.FileHandle == null)
        {
            if (_Header != null)
                _Header = null;
            if (_NodeBlocks != null)
                _NodeBlocks = null;
        }
    }

    /// <summary>
    /// Lädt den Datenbestand erneut vom Dateisystem.
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public override void Reload()
    {
        base.Reload();

        _Header = new FSHeader<T>((T)GuardianGet);
        _Header.Read();

        ReadNodeBlocks();
        BuildTree();

        _CurrentFolder = _Root.Root;
    }

    protected override void Format(string Name, int CustomContainerSize, int CustomContainerCount, int ClusterMapCount, int ClusterSize)
    {
        base.Format(Name, CustomContainerSize, CustomContainerCount, ClusterMapCount, ClusterSize);
        if (IsOpen)
        {
            _Header = new FSHeader<T>((T)GuardianGet);
            _Header.Write();

            int first = 0;
            _NodeBlocks = new List<FSNodeCluster<T>>() { new FSNodeCluster<T>((T)GuardianGet, GuardianGet.ClusterSize, first) };
            GuardianGet.ClusterMaps[first] = true;  // Root

            var Root = _NodeBlocks[first].CreateNode(rootID);
            Root.Name = "ROOT";
            Root.IsDirectory = true;

            _Root = new FSTree<T>(Root);

            Flush();

            GuardianGet.FileHandle.SetLength(GuardianGet.ClusterOffset() + GuardianGet.ClusterSize);

            BuildTree();

            _CurrentFolder = _Root.Root;
        }
    }

    /// <summary>
    /// Erstellt ein neues Verzeichnis im aktuellen
    /// </summary>
    /// <param name="DirectoryName">Der Name des neuen Verzeichnisses</param>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public FSDirectory<T> CreateFolder(string DirectoryName)
    {
        if (_CurrentFolder.FindDirectory(DirectoryName) != null)
            throw new FSException("directory already exists");
        else
        {
            try
            {
                var NewDirectory = CreateNode(Hash.HashFNV1a32(FullPath + DirectoryName), _CurrentFolder.Id);

                NewDirectory.IsDirectory = true;
                NewDirectory.Name = DirectoryName;

                var Result = new FSDirectory<T>(NewDirectory);

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
    public FSDirectory<T> ChangeToFolder(string DirectoryName)
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
    public FSDirectory<T> ChangeToRoot()
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
    public FSDirectory<T> ChangeOneUp()
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
                Attributes = FlagsToString(_CurrentFolder.Directories[i].Flags),
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
                Attributes = FlagsToString(_CurrentFolder.Files[i].Flags),
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
    public FSNode<T> Touch(string Filename)
    {
        var Result = _CurrentFolder.FindFile(Filename);

        if (Result != null)
            throw new FSException("FILE ALREADY EXISTS");
        else
        {
            FSNode<T> NewFile = null;

            try
            {
                NewFile = CreateNode(Hash.HashFNV1a32(FullPath + Filename), _CurrentFolder.Id);

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

    public FSStream<T> Patch(string SourceFilePath, string DestFilePath, IFSPatchTransform Transform = null)
    {
        if (!File.Exists(SourceFilePath))
            throw new FSException("SOURCEFILE NOT FOUND");

        FSNode<T> DestFileNode = GetFile(DestFilePath);
        if (DestFileNode == null)
            DestFileNode = Touch(DestFilePath);

        bool PatchFile = false;
        byte[] Buffer = new byte[4096];
        int Read = 0;

        if (Transform == null)
        {
            var SourceCRC = new tinyCRC();
            using (FileStream Source = File.Open(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                while ((Read = Source.Read(Buffer, 0, Buffer.Length)) > 0)
                    SourceCRC.Push(Buffer, 0, Read);

                Source.Close();
            }

            // PatchFile = (SourceCRC.CRC32 != DestFileNode.CRC);
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
                        string SourceExtension = SourceFilePath.ExtensionOnly().ToLower();
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

    public FSStream<T> Copy(string SourceFile)
    {
        if (!File.Exists(SourceFile))
            throw new FSException("SOURCEFILE NOT FOUND");

        using (var Dest = GetFileStream(Touch(SourceFile.FilenameOnly())))
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
                FSNode<T> Node = _NodeBlocks[i].FindNode(Directory.Id);
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
            FSNode<T> File = _CurrentFolder.FindFile(Name);
            if (File == null)
                throw new FSException("FILE NOT FOUND");

            using (var FS = GetFileStream(Name))
                FS.SetLength(0);

            FSNode<T> Node;
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

    public FSNode<T> GetFile(string Name)
    {
        if (Name.Contains("\\"))
        {
            FSDirectory<T> Runner = _CurrentFolder;

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
                            FSNode<T> File = Runner.FindFile(Item);
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

    public FSStream<T> GetFileStream(string Name)
    {
        FSNode<T> Node = GetFile(Name);

        if (Node != null)
            return new FSStream<T>(this, (T)GuardianGet, Node);
        else
            return null;
    }

    public FSStream<T> GetFileStream(FSNode<T> Node)
    {
        if (Node != null)
            return new FSStream<T>(this, (T)GuardianGet, Node);
        else
            return null;
    }
    #endregion

    public FS(string Filename)
        : base(new FSGuardian(FILE_SIGNATURE), Filename)
    {
        rootID = Hash.HashFNV1a32("ROOT");
    }


}
public class FS
    : FS<IFSGuardian>
{
    public FS(string Filename) 
        : base(Filename)
    {
    }
}
