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

//public class DFStream
//    : Stream
//{
//    private DFNode _Node;

//    private DFDataCluster _CurrentCluster;
//    private int _CurrentIndex;

//    private long _ClusterStart;
//    private long _ClusterEnd;

//    private long _CurrentPosition = 0;

//    #region Properties
//    public override bool CanRead
//    {
//        get { return true; }
//    }
//    public override bool CanSeek
//    {
//        get { return true; }
//    }
//    public override bool CanTimeout
//    {
//        get { return false; }
//    }
//    public override bool CanWrite
//    {
//        get { return true; }
//    }
//    public override long Length
//    {
//        get { return _Node.FileSize; }
//    }
//    public override long Position
//    {
//        get
//        {
//            return _CurrentPosition;
//        }
//        set
//        {
//            _CurrentPosition = value;
//        }
//    }

//    public Stream BaseStream { get { return this; } }

//    private string _LastError = "";
//    /// <summary>
//    /// Liefert den letzten Fehler zurück.
//    /// </summary>
//    public string LastError
//    {
//        get
//        {
//            var Result = _LastError;
//            _LastError = "";

//            return Result;
//        }
//        private set
//        {
//            _LastError = value;
//        }
//    }
//    #endregion

//    #region Stream Methods
//    public override void Flush()
//    {
//        try
//        {
//            _DF.Flush();
//        }
//        catch (DFException)
//        {
//            // pass through
//            throw;
//        }

//    }
//    public override void Close()
//    {
//        try
//        {
//            Flush();
//            base.Close();
//        }
//        catch (FSException)
//        {
//            // pass through
//            throw;
//        }
//        catch (IOException IOe)
//        {
//            throw new FSException(IOe.Message);
//        }

//    }
//    public override int Read(byte[] buffer, int offset, int count)
//    {
//        int Read = 0, BufferOffset = offset;
//        while (Read < count)
//        {
//            if (!SetClusterMatchPosition(Position))
//                return Read;

//            int LoopCount = (int)(_ClusterEnd - _CurrentPosition) + 1;
//            if (_ClusterEnd > _Node.FileSize)
//                LoopCount -= (int)(_ClusterEnd - _Node.FileSize) + 1;

//            if (LoopCount == 0)
//                return Read;

//            int LoopPos = (int)(_CurrentPosition - _ClusterStart);
//            int DataCount = ((count - Read) < LoopCount ? count - Read : LoopCount);

//            _CurrentCluster.BlockRead(buffer, LoopPos, BufferOffset, DataCount);

//            BufferOffset += DataCount;
//            Read += DataCount;
//            Position += DataCount;
//        }
//        return Read;
//    }

//    public override int ReadByte()
//    {
//        byte[] Result = new byte[1];
//        int Read = this.Read(Result, 0, 1);

//        if (Read == 0)
//            return -1;
//        else
//            return (int)Result[0];
//    }

//    public override long Seek(long offset, SeekOrigin origin)
//    {
//        long NewOffset;
//        switch (origin)
//        {
//            case SeekOrigin.Begin:
//                NewOffset = offset;
//                break;
//            case SeekOrigin.Current:
//                NewOffset = _CurrentPosition + offset;
//                break;
//            case SeekOrigin.End:
//                NewOffset = _Node.FileSize + offset;
//                break;
//            default:
//                NewOffset = 0;
//                break;
//        }

//        if (NewOffset > Length)
//        {
//            int NewClusterCount = ClusterCountRequirement(NewOffset);
//            if (!EnhanceClustersTo(NewClusterCount))
//                throw new IOException("Oops");
//        }

//        if (SetClusterMatchPosition(NewOffset))
//            return _CurrentPosition = NewOffset;
//        else
//            throw new IOException();
//    }

//    public override void SetLength(long value)
//    {
//        try
//        {

//            if (value > Length)
//            {
//                int NewClusterCount = ClusterCountRequirement(value);
//                if (!EnhanceClustersTo(NewClusterCount))
//                    throw new IOException("Oops");
//            }
//            else
//            {
//                int NewClusterCount = ClusterCountRequirement(value);
//                ReduceClustersTo(NewClusterCount);
//            }
//            _Node.FileSize = (int)value;
//        }
//        catch (FSException)
//        {
//            // pass through
//            throw;
//        }
//    }

//    public override void Write(byte[] buffer, int offset, int count)
//    {
//        if ((_CurrentPosition + count) > Length)
//            SetLength(_CurrentPosition + count);

//        if (!SetClusterMatchPosition(_CurrentPosition))
//            throw new Exception(LastError);

//        int Written = 0, BufferOffset = offset;
//        while (Written < count)
//        {
//            if (!SetClusterMatchPosition(_CurrentPosition))
//                throw new IndexOutOfRangeException();

//            int LoopCount = (int)(_ClusterEnd - _CurrentPosition) + 1;

//            int LoopPos = (int)(_CurrentPosition - _ClusterStart);
//            int DataCount = ((count - Written) < LoopCount ? count - Written : LoopCount);

//            _CurrentCluster.BlockWrite(buffer, BufferOffset, LoopPos, DataCount);

//            BufferOffset += DataCount;
//            Written += DataCount;
//            Position += DataCount;
//        }
//    }

//    public override void WriteByte(byte value)
//    {
//        Write(new byte[] { value }, 0, 1);
//    }
//    #endregion

//    private bool ReadCluster(int FileIndex, int Cluster)
//    {
//        // read next cluster
//        if (Cluster != -1)
//        {
//            _CurrentCluster = _FSBase.ReadCluster(Cluster);

//            _ClusterStart = (FileIndex * _FSBase.Header.UseableClusterSize);
//            _ClusterEnd = (FileIndex * _FSBase.Header.UseableClusterSize) + _FSBase.Header.UseableClusterSize - 1;

//            _CurrentIndex = FileIndex;
//        }
//        else
//        {
//            // position points to an unknown cluster.. wait and see
//            _CurrentCluster = null;

//            _ClusterStart = (FileIndex * _FSBase.Header.UseableClusterSize);
//            _ClusterEnd = (FileIndex * _FSBase.Header.UseableClusterSize) + _FSBase.Header.UseableClusterSize - 1;

//            _CurrentIndex = FileIndex;
//        }

//        return true;
//    }

//    /// <summary>
//    /// Versucht den Cluster aktuellen Cluster zu laden. 
//    /// </summary>
//    /// <param name="Position">Die Position die den Cluster angibt.</param>
//    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//    private bool SetClusterMatchPosition(long Position)
//    {
//        while (Position < _ClusterStart)
//            if (!ReadCluster(_CurrentIndex - 1, _CurrentCluster.Previous))
//                return false;


//        while (Position > _ClusterEnd)
//            if (!ReadCluster(_CurrentIndex + 1, _CurrentCluster.Next))
//                return false;

//        return true;
//    }

//    private bool EnhanceClustersTo(int ClusterCount)
//    {
//        while (_Node.ClusterCount < ClusterCount)
//        {
//            int Slot = _FSBase.Maps.GetFreeSlot();

//            FSDataCluster CurrentCluster; int CurrentClusterSlot = -1;
//            // Sonderfall, kein Cluster existiert - Setze FirstCluster
//            if (_Node.ClusterCount == 0)
//                _FSBase.Maps[_Node.FirstCluster = _Node.LastCluster = Slot] = true;
//            else
//            {
//                CurrentCluster = _FSBase.ReadCluster(_Node.LastCluster);
//                CurrentClusterSlot = CurrentCluster.Cluster;

//                _FSBase.Maps[CurrentCluster.Next = _Node.LastCluster = Slot] = true;
//            }

//            var NewCluster = _FSBase.CreateDataCluster(Slot);
//            NewCluster.Previous = CurrentClusterSlot;

//            _Node.ClusterCount++;
//        }
//        if ((_CurrentCluster == null) && (ClusterCount > 0))
//            if (!ReadCluster(0, _Node.FirstCluster))
//                return WithError(LastError);

//        return true;
//    }
//    private void ReduceClustersTo(int ClusterCount)
//    {
//        try
//        {
//            FSDataCluster CurrentCluster;
//            if (_Node.LastCluster == -1)
//                return;
//            else
//            {
//                int Cluster = _Node.LastCluster, Previous = -1;
//                while (_Node.ClusterCount > ClusterCount)
//                {
//                    CurrentCluster = _FSBase.ReadCluster(Cluster);
//                    Previous = _Node.LastCluster = CurrentCluster.Previous;

//                    _FSBase.ClearCluster(Cluster);

//                    _FSBase.Maps[Cluster] = false;
//                    _Node.ClusterCount--;

//                    Cluster = Previous;
//                }

//                if (_Node.ClusterCount > 0)
//                {
//                    CurrentCluster = _FSBase.ReadCluster(_Node.LastCluster);
//                    CurrentCluster.Next = -1;
//                }
//                else
//                    _Node.FirstCluster = _Node.LastCluster;
//            }
//        }
//        catch (FSException)
//        {
//            throw;
//        }
//    }

//    public bool WithError(string Error)
//    {
//        LastError = Error;
//        return false;
//    }

//    private int ClusterCountRequirement(long FileLength)
//    {
//        return (int)Math.Ceiling(FileLength / (double)_FSBase.Header.UseableClusterSize);
//    }

//    public DFStream(IDF DF, DFNode Node)
//        : base()
//    {
//        _Node = Node;
//        if (_Node.ClusterCount > 0)
//            ReadCluster(0, _Node.FirstCluster);
//    }
//}