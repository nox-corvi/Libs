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

public class DFClusterMaps
    : DFContainer
{
    private DFClusterMap[] _Map;

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
            for (int i = 0; i < DF.Header.ClusterMapCount; i++)
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
            for (int i = 0; i < DF.Header.ClusterMapCount; i++)
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
            for (int i = 0; i < DF.Header.ClusterMapCount; i++)
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

            for (int i = 0; i < DF.Header.ClusterMapCount; i++)
                Result |= _Map[i].Dirty;

            return Result;
        }
        protected set
        {
            base.Dirty = value;
        }
    }
    #endregion

    public override void ReadUserData(BinaryReader Reader)
    {
        _Map = new DFClusterMap[DF.Header.ClusterMapCount];
        for (int i = 0; i < DF.Header.ClusterMapCount; i++)
        {
            _Map[i] = new DFClusterMap(DF);
            _Map[i].Read(Reader);
        }
    }

    public override void WriteUserData(BinaryWriter Writer)
    {
        for (int i = 0; i < DF.Header.ClusterMapCount; i++)
            if (_Map[i].Dirty)
                _Map[i].Write(Writer);
            else
                // move one cluster
                Writer.Seek(DF.Header.ClusterSize, SeekOrigin.Current);
    }

    /// <summary>
    /// Ermittelt einen freien Slot und liefert ihn zurück
    /// </summary>
    /// <returns>Eine Id wenn erfolgreich, sonst -1</returns>
    public int GetFreeSlot()
    {
        int Base = 0;
        for (int i = 0; i < DF.Header.ClusterMapCount; i++)
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
            for (int i = 0; i < DF.Header.ClusterMapCount; i++)
                _Map[i] = new DFClusterMap(DF);
        }
        catch (DFException)
        {
            // pass through
            throw;
        }
    }

    public override int UserDataSize()
        => _Map.Length * DF.Header.ClusterSize;

    public override void UserDataCRC(tinyCRC CRC)
    {
        for (int i = 0; i < _Map.Length; i++)
            _Map[i].UserDataCRC(CRC);
    }

    public DFClusterMaps(IDF DF, int ClusterMapCount)
        : base(DF)
        => _Map = new DFClusterMap[ClusterMapCount];

    public DFClusterMaps(IDF DF)
        : this(DF, DF.Header.ClusterMapCount)
    {

    }
}