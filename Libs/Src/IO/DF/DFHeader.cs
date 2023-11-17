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
using Nox.Component;
using Nox.IO.Buffer;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;

namespace Nox.IO.DF;

public class DFHeader
    : DFBase, IHeader, IDFCRCSupport
{
    public const uint CURRENT_VERSION = 0x0001;

    // Felder
    private uint _Signature;

    private uint _Version;
    private int _Build;

    private byte[] _Name;

    private DateTime _Created;
    private DateTime _Modified;

    private int _ContainerCount;
    private int[] _ContainerSizes;

    private int _ClusterMapCount;

    private int _ClusterSize;

    private uint _CRC;

    private DFClusterMaps _ClusterMaps;

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
        => SetProperty(ref _ContainerSizes[Index], Size);

    public int ClusterMapCount
    {
        get => _ClusterMapCount;
        set => SetProperty(ref _ClusterMapCount, value);
    }

    public DFClusterMaps ClusterMaps {  get =>  _ClusterMaps; }

    public int ClusterSize
    {
        get => _ClusterSize;
        set => SetProperty(ref _ClusterSize, value);
    }

    public uint CRC { get => _CRC; }

    public virtual bool Dirty { get; protected set; }
    #endregion

    #region I/O
    public void Read()
    {
        try
        {
            DF.FileHandle.Position = 0;
            BinaryReader Reader = new BinaryReader(DF.FileHandle);

            if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
                throw new InvalidDataException("signature mismatch");

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
            
            _ClusterMaps.CreateNewClusterMaps();
            _ClusterMaps.Read();

            _CRC = Reader.ReadUInt32();
            if (ReCRC() != _CRC)
                throw new InvalidDataException("Header CRC mismatch");
        }
        catch (IOException)
        {
            throw;
        }

    }
    public void Write()
    {
        try
        {
            DF.FileHandle.Position = 0;
            BinaryWriter Writer = new BinaryWriter(DF.FileHandle);

            Writer.Write(_Signature);

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

            for (int i = 0; i < _ClusterMapCount; i++)
                Writer.Write(_ClusterMaps[i]);

            _CRC = ReCRC();
            Writer.Write(_CRC);
        }
        catch (IOException)
        {
            throw;
        }
    }
    #endregion

    #region Helpers
    public void UserDataCRC(tinyCRC CRC)
    {

    }

    /// <summary>
    /// Berechnet die Checksumme für den Kopfsatz.
    /// </summary>
    /// <returns>der CRC32 des Kopfsatzes</returns>
    public uint ReCRC()
    {
        var CRC = new tinyCRC();
        CRC.Push(_Signature);

        CRC.Push(_Version);
        CRC.Push(_Build);

        CRC.Push(_Name);

        CRC.Push(_Created.ToFileTimeUtc());
        CRC.Push(_Modified.ToFileTimeUtc());

        CRC.Push(_ContainerCount);
        for (int i = 0; i < _ContainerCount; i++)
            CRC.Push(_ContainerSizes[i]);

        CRC.Push(_ClusterMapCount);
        CRC.Push(_ClusterSize);

        for (int i = 0; i < _ClusterMapCount; i++)
            _ClusterMaps.UserDataCRC(CRC);

        UserDataCRC(CRC);

        return CRC.CRC32;
    }

    public int HeaderSize()
    {
        int Size = 0;
        Size += sizeof(uint);   // _Signature;
        Size += sizeof(uint);   // _Version;
        Size += sizeof(int);    // _Build;
        Size += 32;             // _Name;
        Size += sizeof(long);   // _Created;
        Size += sizeof(long);   // _Modified;

        Size += sizeof(int);    // _ContainerCount;
        Size += _ContainerCount * sizeof(uint);  // _ContainerSizes;

        Size += sizeof(int);    //_ClusterMapCount;
        Size += _ClusterMapCount * sizeof(uint); // _ClusterMaps;

        Size += sizeof(int);    // _ClusterSize;

        Size += _ClusterMapCount * ClusterSize; // ClusterMaps

        Size += sizeof(uint);   // _CRC;

        return Size;
    }

    public int ContainerOffset(int Index = 0)
    {
        int Size = HeaderSize();
        for (int i = 0; i < M.Min(Index, ContainerCount); i++)
            Size += _ContainerSizes[i];

        return Size;
    }

    public int ClusterMapOffset(int Index = 0)
    {
        int Size = ContainerOffset(ContainerCount);
        Size += M.Min(Index, ClusterMapCount) * ClusterSize;

        return Size;
    }

    public int ClusterOffset(int Index = 0)
        => ClusterMapOffset(ClusterMapCount) + ClusterSize;

    public int FirstClusterOffset()
        => ClusterOffset(0);
    #endregion

    //public int SizeInBytes { get => HeaderSize(); }

    public DFHeader(IDF DF, int ClusterSize = 8192)
        : base(DF)
    {
        PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(Modified))
                {
                    _Modified = DateTime.UtcNow;
                }

                Dirty = true;
            };

        _Signature = DefaultSignature;

        _Version = CURRENT_VERSION;
        _Build = Assembly.GetEntryAssembly().GetName().Version.Build;

        _Name = GetStringBytes(Guid.NewGuid().ToString(), 64);

        _Created = DateTime.UtcNow;
        _Modified = DateTime.UtcNow;

        _ClusterSize = ClusterSize;
        _ClusterMaps = new DFClusterMaps(DF, ClusterMapCount);

        _CRC = ReCRC();
    }
}