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
using Nox.IO.Buffer;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;

namespace Nox.IO.DF;

//public class DFMap 
//    : DFElement
//{
//    private byte[] _Map;

//    private int _SlotCount;
//    private int _SlotsFree;

//    #region Properties
//    /// <summary>
//    /// Liefert die Belegung eines Clusters zurück oder legt ihn fest.
//    /// </summary>
//    /// <param name="Index">Der 0-basierte Index des Clusters</param>
//    /// <returns>Wahr wenn der Cluster in Verwendung ist, anderenfalls Falsch.</returns>
//    public bool this[int Index]
//    {
//        get
//        {
//            byte Mask = (byte)(1 << (Index & 7));
//            return (_Map[Index >> 3] & Mask) == Mask;
//        }
//        set
//        {
//            byte Mask = (byte)(1 << (Index & 7));
//            bool Used = (_Map[Index >> 3] & Mask) == Mask;

//            if (value)
//            {
//                if (!Used)
//                    _SlotsFree++;

//                _Map[Index >> 3] |= Mask;
//            }
//            else
//            {
//                if (Used)
//                    _SlotsFree--;

//                _Map[Index >> 3] &= (byte)~Mask;
//            }
//        }
//    }

//    /// <summary>
//    /// Liefert die Anzahl an Slot zurück.
//    /// </summary>
//    public int SlotCount
//    {
//        get
//        {
//            return _SlotCount;
//        }
//    }

//    /// <summary>
//    /// Liefert die Anzahl an freien Slots zurück.
//    /// </summary>
//    public int SlotsFree
//    {
//        get
//        {
//            return _SlotsFree;
//        }
//    }

//    /// <summary>
//    /// Liefert die Anzahl an belegten Slots zurück
//    /// </summary>
//    public int SlotsUsed
//    {
//        get
//        {
//            return _SlotCount - _SlotsFree;
//        }
//    }

//    /// <summary>
//    /// Liefert die Größe der Karte in Bytes zurück.
//    /// </summary>
//    public int MapSize
//    {
//        get
//        {
//            return _Map.Length;
//        }
//    }
//    #endregion

//    #region I/O
//    public override void Read(BinaryReader Reader)
//    {
//        try
//        {
//            for (int i = 0; i < _Map.Length; i++)
//            {
//                byte t = _Map[i] = Reader.ReadByte();

//                if (t == 0xFF)
//                    _SlotsFree -= 8;
//                else
//                    for (int j = 0; j < 8; j++, t >>= 1)
//                        _SlotsFree -= (byte)(t & 1);
//            }
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
//            for (int i = 0; i < _Map.Length; i++)
//                Writer.Write(_Map[i]);
//        }
//        catch (IOException IOe)
//        {
//            throw new DFException(IOe.Message);
//        }
//    }
//    #endregion

//    /// <summary>
//    /// Ermittelt einen freien Slot und liefert ihn zurück
//    /// </summary>
//    /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
//    public int GetFreeSlot()
//    {
//        if (_SlotsFree == 0)
//            return -1;
//        else
//            for (int i = 0; i < _Map.Length; i++)
//                if (_Map[i] != 0xFF)
//                {
//                    int Start = i << 3;
//                    for (int j = 0; j < 8; j++)
//                        if (!this[Start + j])
//                            return Start + j;
//                }

//        return -1;
//    }

//    public override void UserDataCRC(tinyCRC CRC)
//    {
//        throw new NotImplementedException();
//    }

//    public DFMap(IDF DF, int SlotCount)
//        : base(DF)
//    {
//        _SlotsFree = _SlotCount = SlotCount;
//        _Map = new byte[(int)M.Ceiling(SlotCount / (double)8)];
//    }
//}