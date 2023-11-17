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

//public class DFNodeCluster 
//    : DFCluster
//{
//    // Konstanten
//    private const uint DEFAULT_SIGNATURE = 0x6CD353FA;

//    private uint _Signature = DEFAULT_SIGNATURE;

//    private int _NextBlock;
//    private byte[] _Reserved;

//    private DFMap _NodeMap;
//    private DFNode[] _Nodes;

//    #region Properties
//    /// <summary>
//    /// Liefert den ClusterIndex des nächsten Knotens zurück
//    /// </summary>
//    public int NextBlock
//    {
//        get
//        {
//            return _NextBlock;
//        }
//        set
//        {
//            if (_NextBlock != value)
//            {
//                _NextBlock = value;
//                Dirty = true;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert den Offset der lokalen Karte zurück
//    /// </summary>
//    private int LocalMapOffset
//    {
//        get
//        {
//            return sizeof(uint) + sizeof(int) + _Reserved.Length;
//        }
//    }

//    /// <summary>
//    /// Liefert den Offset des ersten Knoten zurück
//    /// </summary>
//    private int LocalNodeOffset
//    {
//        get
//        {
//            return LocalMapOffset + _NodeMap.MapSize;
//        }
//    }

//    /// <summary>
//    /// LIefert die Anzahl an freien Knoten zurück
//    /// </summary>
//    public int NodesFree
//    {
//        get
//        {
//            return _NodeMap.SlotsFree;
//        }
//    }

//    /// <summary>
//    /// Liefert die Anzahl an belegten Knoten zurück.
//    /// </summary>
//    public int NodesUsed
//    {
//        get
//        {
//            return _NodeMap.SlotsUsed;
//        }
//    }

//    public override bool Dirty
//    {
//        get
//        {
//            if (!base.Dirty)
//                for (int i = 0; i < _Nodes.Length; i++)
//                    if (_Nodes[i] != null)
//                        if (_Nodes[i].Dirty)
//                            return true;

//            return base.Dirty;
//        }
//        protected set
//        {
//            base.Dirty = value;
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

//            _NextBlock = Reader.ReadInt32();
//            _Reserved = Reader.ReadBytes(_Reserved.Length);

//            _NodeMap.Read(Reader);

//            // Nodes einlesen...
//            byte[] Blank = new byte[DFBase.Header.NodeSize];

//            for (int i = 0; i < _Nodes.Length; i++)
//            {
//                if (_NodeMap[i])
//                {
//                    _Nodes[i] = new DFNode(this.DF);
//                    _Nodes[i].Read(Reader);
//                }
//                else
//                    Reader.Read(Blank, 0, Blank.Length);
//            }
//        }
//        catch (FSException)
//        {
//            throw;
//        }
//    }

//    public override void WriteUserData(BinaryWriter Writer)
//    {
//        try
//        {
//            Writer.Write(_Signature);

//            Writer.Write(_NextBlock);
//            Writer.Write(_Reserved);

//            _NodeMap.Write(Writer);


//            byte[] Blank = new byte[FSBase.Header.NodeSize];
//            for (int i = 0; i < _Nodes.Length; i++)
//            {
//                if (_NodeMap[i])
//                    _Nodes[i].Write(Writer);
//                else
//                    Writer.Write(Blank);
//            }
//        }
//        catch (IOException IOe)
//        {
//            throw new FSException(IOe.Message);
//        }
//        catch (FSException)
//        {
//            throw;
//        }
//    }
//    #endregion

//    #region Helpers
//    /// <summary>
//    /// Ermittelt den nächsten freien Slot.
//    /// </summary>
//    /// <returns>den nächsten freien Slot wenn erfolgreich, sonst -1</returns>
//    public int GetFreeSlot()
//    {
//        return _NodeMap.GetFreeSlot();
//    }

//    /// <summary>
//    /// Erstellt einen neuen Knoten.
//    /// </summary>
//    /// <returns>ein IDXFSNode wenn erfolgreich, sonst null</returns>
//    public FSNode CreateNode(uint NodeId, uint Parent = 0xFFFFFFFF)
//    {
//        int Slot;
//        if ((Slot = GetFreeSlot()) != -1)
//        {
//            _NodeMap[Slot] = true;
//            Dirty = true;

//            _Nodes[Slot] = new FSNode(_IDXFS);
//            _Nodes[Slot].Id = NodeId;
//            _Nodes[Slot].Parent = Parent;

//            return _Nodes[Slot];
//        }
//        else
//            return null;
//    }

//    /// <summary>
//    /// Entfernt einen Knoten aus der Auflistung
//    /// </summary>
//    /// <param name="iNodeId">Die Id des Knoten</param>
//    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
//    public void RemoveNode(uint NodeId)
//    {
//        for (int i = 0; i < _NodeMap.SlotCount; i++)
//            if (_NodeMap[i])
//                if (_Nodes[i].Id == NodeId)
//                {
//                    _NodeMap[i] = false;
//                    _Nodes[i] = null;

//                    Dirty = true;
//                    return;
//                }

//        throw new FSException("NODE NOT FOUND");
//    }

//    /// <summary>
//    /// Liefert den Knoten mit dem angegebenen Index zurück
//    /// </summary>
//    /// <param name="Index"></param>
//    /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
//    public FSNode GetNodeAt(int Index)
//    {
//        return _NodeMap[Index] ? _Nodes[Index] : null;
//    }

//    /// <summary>
//    /// Liefert den Knoten mit dem angegebenen Namen zurück
//    /// </summary>
//    /// <param name="Name">Der Name nach dem gesucht werden soll</param>
//    /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
//    public FSNode FindNode(string Name)
//    {
//        for (int i = 0; i < _Nodes.Length; i++)
//            if (_NodeMap[i])
//                if (_Nodes[i].Name.ToLower() == Name.ToLower())
//                    return _Nodes[i];

//        return null;
//    }

//    /// <summary>
//    /// Liefert den Knoten mit dem angegebenen Namen zurück
//    /// </summary>
//    /// <param name="NodeId">Die Id nach der gesucht werden soll</param>
//    /// <returns>ein IDXFSNode wenn belegt, sonst null</returns>
//    public FSNode FindNode(uint NodeId)
//    {
//        for (int i = 0; i < _Nodes.Length; i++)
//            if (_NodeMap[i])
//                if (_Nodes[i].Id == NodeId)
//                    return _Nodes[i];

//        return null;
//    }
//    #endregion

//    public DFNodeCluster(IDF DF, int Cluster)
//        : base(DF, Cluster)
//    {
//        _NodeMap = new DFMap(DF, DF.Header.NodesPerBlock);
//        _NextBlock = -1;

//        _Nodes = new DFNode[DF.Header.NodesPerBlock];
//        _Reserved = new byte[DF.Header.ClusterSize - ((DF.Header.NodesPerBlock *
//            DF.Header.NodeSize) + _NodeMap.MapSize + 4)];

//    }
//}