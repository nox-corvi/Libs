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
//using System.Runtime.CompilerServices;
//using System.Security.Cryptography;
//using System.Text;
//using M = System.Math;

//namespace Nox.IO.DF;

//public class DFNode 
//    : DFElement
//{
//    // Vars
//    private uint _Signature;

//    private uint _Id;
//    private uint _Parent;

//    private uint _Flags;

//    private byte[] _Name;

//    private int _FileSize;
//    private int _ClusterCount;

//    private DateTime _Created;
//    private DateTime _Modified;

//    private int _FirstCluster;
//    private int _LastCluster;

//    private uint _CRC;

//    #region Properties
//    /// <summary>
//    /// Liefert die Id des Knoten zurück oder legt sie fest
//    /// </summary>
//    public uint Id
//    {
//        get
//        {
//            return _Id;
//        }
//        set
//        {
//            if (_Id != value)
//            {
//                _Id = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert den Vater des Knoten zurück oder legt ihn fest
//    /// </summary>
//    public uint Parent
//    {
//        get
//        {
//            return _Parent;
//        }
//        set
//        {
//            if (_Parent != value)
//            {
//                _Parent = value;
//                Dirty = true;
//            }
//        }
//    }

//    #region Flags
//    public uint Flags
//    {
//        get
//        {
//            return _Flags;
//        }
//        set
//        {
//            if (_Flags != value)
//            {
//                Dirty = true;
//                _Flags = value;
//            }
//        }
//    }

//    public bool IsHidden
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.Hidden) == (uint)FSFlags.Hidden;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.Hidden;
//        }
//    }
//    public bool IsArchive
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.Archive) == (uint)FSFlags.Archive;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.Archive;
//        }
//    }
//    public bool IsReadOnly
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.ReadOnly) == (uint)FSFlags.ReadOnly;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.ReadOnly;
//        }
//    }
//    public bool IsEncrypted
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.Encrypted) == (uint)FSFlags.Encrypted;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.Encrypted;
//        }
//    }
//    public bool IsSymLink
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.SymLink) == (uint)FSFlags.SymLink;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.SymLink;
//        }
//    }
//    public bool IsSystemUseOnly
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.SystemUseOnly) == (uint)FSFlags.SystemUseOnly;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.SystemUseOnly;
//        }
//    }
//    public bool IsDirectory
//    {
//        get
//        {
//            return (_Flags & (uint)FSFlags.Directory) == (uint)FSFlags.Directory;
//        }
//        set
//        {
//            Flags |= (int)FSFlags.Directory;
//        }
//    }
//    #endregion

//    /// <summary>
//    /// Liefert den Namen der Datei zurück oder legt ihn fest
//    /// </summary>
//    public string Name
//    {
//        get
//        {
//            return FSHelpers.BytesToString(_Name);
//        }
//        set
//        {
//            if (FSHelpers.BytesToString(_Name) != value)
//            {
//                _Name = FSHelpers.GetStringBytes(value, 32);
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert die Größe der Datei zurück oder legt sie fest
//    /// </summary>
//    public int FileSize
//    {
//        get
//        {
//            return _FileSize;
//        }
//        set
//        {
//            if (_FileSize != value)
//            {
//                _FileSize = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert die Anzahl an Clustern zurück die von der Datei verwendet werden oder legt sie fest.
//    /// </summary>
//    public int ClusterCount
//    {
//        get
//        {
//            return _ClusterCount;
//        }
//        set
//        {
//            if (_ClusterCount != value)
//            {
//                _ClusterCount = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert das Datum und die Zeit der Anlage zurück oder legt es fest
//    /// </summary>
//    public DateTime Created
//    {
//        get
//        {
//            return _Created;
//        }
//        set
//        {
//            if (_Created != value)
//            {
//                _Created = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert das Datum und die Zeit der letzten Änderung zurück oder legt es fest
//    /// </summary>
//    public DateTime Modified
//    {
//        get
//        {
//            return _Modified;
//        }
//        set
//        {
//            if (_Modified != value)
//            {
//                _Modified = value;
//                Dirty = true;
//            }
//        }
//    }

//    public int FirstCluster
//    {
//        get
//        {
//            return _FirstCluster;
//        }
//        set
//        {
//            if (_FirstCluster != value)
//            {
//                _FirstCluster = value;
//                Dirty = true;
//            }
//        }
//    }

//    public int LastCluster
//    {
//        get
//        {
//            return _LastCluster;
//        }
//        set
//        {
//            if (_LastCluster != value)
//            {
//                _LastCluster = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert den CRC des Knotens zurück oder legt ihn fest.
//    /// </summary>
//    public uint CRC
//    {
//        get
//        {
//            return _CRC;
//        }
//        set
//        {
//            if (_CRC != value)
//            {
//                _CRC = value;
//                Dirty = true;
//            }
//        }
//    }
//    #endregion

//    #region I/O
//    public override void Read(BinaryReader Reader)
//    {
//        try
//        {
//            if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
//                throw new DFException("signature mismatch");

//            _Id = Reader.ReadUInt32();
//            _Parent = Reader.ReadUInt32();

//            _Flags = Reader.ReadUInt32();

//            _Name = Reader.ReadBytes(32);

//            _FileSize = Reader.ReadInt32();
//            _ClusterCount = Reader.ReadInt32();

//            try
//            {
//                _Created = DateTime.FromFileTimeUtc(Reader.ReadInt64());
//            }
//            catch
//            {
//                _Created = DateTime.MinValue;
//            }
//            try
//            {
//                _Modified = DateTime.FromFileTimeUtc(Reader.ReadInt64());
//            }
//            catch
//            {
//                _Modified = DateTime.MinValue;
//            }

//            _FirstCluster = Reader.ReadInt32();
//            _LastCluster = Reader.ReadInt32();

//            _CRC = Reader.ReadUInt32();
//        }
//        catch (IOException IOe)
//        {
//            throw new DFException(IOe.Message);
//        }
//    }
//    public override void Write(BinaryWriter Writer)
//    {
//        try
//        {
//            Writer.Write(_Signature);

//            Writer.Write(_Id);
//            Writer.Write(_Parent);

//            Writer.Write(_Flags);

//            Writer.Write(_Name, 0, _Name.Length);

//            Writer.Write(_FileSize);
//            Writer.Write(_ClusterCount);

//            Writer.Write(_Created.ToFileTimeUtc());
//            Writer.Write(_Modified.ToFileTimeUtc());

//            Writer.Write(_FirstCluster);
//            Writer.Write(_LastCluster);

//            Writer.Write(_CRC = ReCRC());
//        }
//        catch (IOException IOe)
//        {
//            throw new DFException(IOe.Message);
//        }
//    }
//    #endregion

//    #region Helpers
//    /// <summary>
//    /// Berechnet die Checksumme für den Knoten.
//    /// </summary>
//    /// <returns>der CRC32 des Knoten</returns>
//    public uint ReCRC()
//    {
//        var CRC = new tinyCRC();
//        CRC.Push(_Signature);
//        CRC.Push(_Id);
//        CRC.Push(_Parent);
//        CRC.Push(_Flags);
//        CRC.Push(_Name);
//        CRC.Push(_FileSize);
//        CRC.Push(_ClusterCount);
//        CRC.Push(_Created.ToFileTimeUtc());
//        CRC.Push(_Modified.ToFileTimeUtc());

//        CRC.Push(_FirstCluster);
//        CRC.Push(_LastCluster);

//        return CRC.CRC32;
//    }
//    #endregion

//    public DFNode(IDF DF)
//        : base(DF)
//    {
//        _Signature = DefaultSignature;
//        _Name = GetStringBytes("", 32);

//        _Created = DateTime.UtcNow;
//        _Modified = DateTime.UtcNow;

//        _FirstCluster = -1;
//        _LastCluster = -1;

//        _CRC = ReCRC();
//    }
//}