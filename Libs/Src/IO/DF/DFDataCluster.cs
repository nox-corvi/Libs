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

//namespace Nox.IO.DF;

//public class DFDataCluster
//    : DFCluster
//{
//    private const uint DEFAULT_SIGNATURE = 0xFAD53F33;

//    private uint _Signature;

//    private int _Previous;
//    private int _Next;

//    private byte[] _Data;
//    private uint _CRC;

//    #region Properties
//    /// <summary>
//    /// Liefert ein Byte aus dem Datenpuffer zurück oder legt es fest
//    /// </summary>
//    /// <param name="Index">Der Index an dem das Byte gelesen oder geschrieben werden soll</param>
//    /// <returns>Das gelesene Byte</returns>
//    public byte this[int Index]
//    {
//        get
//        {
//            return _Data[Index];
//        }
//        set
//        {
//            _Data[Index] = value;
//            Dirty = true;
//        }
//    }

//    public int Previous
//    {
//        get
//        {
//            return _Previous;
//        }
//        set
//        {
//            if (_Previous != value)
//            {
//                _Previous = value;
//                Dirty = true;
//            }
//        }
//    }

//    public int Next
//    {
//        get
//        {
//            return _Next;
//        }
//        set
//        {
//            if (_Next != value)
//            {
//                _Next = value;
//                Dirty = true;
//            }
//        }
//    }
//    #endregion

//    #region I/O
//    public override void ReadUserData(BinaryReader Reader)
//    {
//        try
//        {
//            if ((_Signature = Reader.ReadUInt32()) != DEFAULT_SIGNATURE)
//                throw new DFException("signature mismatch");

//            _Previous = Reader.ReadInt32();
//            _Next = Reader.ReadInt32();

//            int DataRead = Reader.Read(_Data, 0, _Data.Length);

//            _CRC = Reader.ReadUInt32();
//        }
//        catch (IOException IOe)
//        {
//            throw new DFException(IOe.Message);
//        }
//    }

//    public override void WriteUserData(BinaryWriter Writer)
//    {
//        try
//        {
//            Writer.Write(_Signature);

//            Writer.Write(_Previous);
//            Writer.Write(_Next);

//            Writer.Write(_Data, 0, _Data.Length);
//            Writer.Write(_CRC = ReCRC());

//            Dirty = false;
//        }
//        catch (IOException IOe)
//        {
//            throw new DFException(IOe.Message);
//        }
//    }
//    #endregion

//    public int BlockRead(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
//    {
//        int Read = (_Data.Length - SourceOffset);

//        if (Read > Length)
//            Read = Length;

//        try
//        {
//            Array.Copy(_Data, SourceOffset, Buffer, DestOffset, Read);
//        }
//        catch
//        {
//            throw new DFException("array read-access troubles");
//        }

//        return Read;
//    }

//    public int BlockWrite(byte[] Buffer, int SourceOffset, int DestOffset, int Length)
//    {
//        int Read = (_Data.Length - SourceOffset);

//        if (Read > _Data.Length)
//            Read = Length;

//        try
//        {
//            Array.Copy(Buffer, SourceOffset, _Data, DestOffset, Length);
//            Dirty = true;
//        }
//        catch
//        {
//            throw new Exception("array write-access troubles");
//        }


//        return Read;
//    }

//    #region Helpers
//    /// <summary>
//    /// Berechnet die Checksumme für den Cluster.
//    /// </summary>
//    /// <returns>der CRC32 des Knoten</returns>
//    public uint ReCRC()
//    {
//        var CRC = new tinyCRC();

//        CRC.Push(_Signature);
//        CRC.Push(_Data, 0, _Data.Length);

//        return CRC.CRC32;
//    }
//    #endregion

//    public DFDataCluster(IDF DF, int Cluster)
//        : base(DF, Cluster)
//    {
//        _Signature = DEFAULT_SIGNATURE;

//        _Data = new byte[DF.Header.UseableClusterSize];

//        _CRC = ReCRC();
//    }

//    ~DFDataCluster()
//    {
//        try
//        {
//            if (Dirty)
//                Write();
//        }
//        catch (DFException)
//        {
//            // pass through
//            throw;
//        }
//    }
//}